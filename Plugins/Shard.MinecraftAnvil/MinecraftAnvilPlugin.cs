// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using DragonLib;
using Shard.SDK;
using Shard.SDK.Models;

namespace Shard.MinecraftAnvil;

[ShardPlugin(Encoder, "Shard.MinecraftAnvil", "chronovore", "1.0.0", "Processes mca entries")]
public class MinecraftAnvilPlugin : ShardPlugin {
	public const string Encoder = "aq.chronovore.shard.minecraft_anvil";

	public bool CanRecode => true;

	public bool CanProcess(Stream stream, string path, ShardRecordMetadata metadata) => (path.EndsWith(".mca", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".mcr", StringComparison.OrdinalIgnoreCase)) && stream.Length >= 0x2000;

	public unsafe void Decode(Stream stream, string path, IShardArchive archive, ShardRecordMetadata metadata) {
		using var newRegionBufferRaw = MemoryPool<byte>.Shared.Rent(0x3000);
		var newRegionBufferMem = newRegionBufferRaw.Memory[..0x3000];
		var newRegionBuffer = MemoryMarshal.Cast<byte, uint>(newRegionBufferMem.Span);

		Span<uint> regionBuffer = stackalloc uint[0x800];
		stream.ReadExactly(MemoryMarshal.AsBytes(regionBuffer));

		var header = MemoryMarshal.Cast<uint, HeaderEntry>(newRegionBuffer[..0x800]);

		newRegionBuffer.Clear();
		regionBuffer[0x400..].CopyTo(newRegionBuffer[0x800..]); // move timestamps back.

		using var outputTmp = new MemoryStream();
		for (var i = 0; i < 0x400; ++i) {
			var l = BinaryPrimitives.ReverseEndianness(regionBuffer[i]);
			var offset = ((l >> 8) & 0xFFFFFF) << 12;
			var size = (int) ((l & 0xFF) << 12);
			if (offset == 0 || size == 0) {
				continue;
			}

			stream.Position = offset;
			using var buffer = MemoryPool<byte>.Shared.Rent(size);
			var mem = buffer.Memory[..size];
			stream.ReadExactly(mem.Span);

			var compressionSize = BinaryPrimitives.ReadInt32BigEndian(mem.Span) - 1;
			var compressionType = mem.Span[4];
			if (mem.Length < 4 + compressionSize) {
				throw new InvalidDataException();
			}

			// ugh why doesn't minecraft store decompressed size, it'd only increase the file size by 4 kb...

			using var fixedHandle = mem[0x5..].Pin();
			using var compressedStream = new UnmanagedMemoryStream((byte*) fixedHandle.Pointer, compressionSize);
			var outputOffset = outputTmp.Position;
			switch (compressionType) {
				case 1: // gzip
					using (var gzip = new GZipStream(compressedStream, CompressionMode.Decompress)) {
						gzip.CopyTo(outputTmp);
						break;
					}
				case 2:
					using (var zlib = new ZLibStream(compressedStream, CompressionMode.Decompress)) {
						zlib.CopyTo(outputTmp);
						break;
					}
				default:
					throw new InvalidDataException();
			}

			outputTmp.SetLength(outputTmp.Length.Align(0x1000));
			var outputSize = outputTmp.Position - offset;

			header[i] = new HeaderEntry {
				Offset = (int) outputOffset,
				Size = (int) outputSize,
			};
		}

		// save timestamps.
		archive.AddRecord(path, newRegionBufferMem, new ShardRecordMetadata { Encoder = Encoder });
		archive.AddRecord($"{path}.chunks", outputTmp.ToArray(), new ShardRecordMetadata { Flags = ShardRecordFlags.Hidden });
	}

	public unsafe Memory<byte> Encode(Memory<byte> data, IShardRecord record, IShardArchive archive) {
		var output = new MemoryStream();

		Span<uint> regionBuffer = stackalloc uint[0x400];
		regionBuffer.Clear();
		Span<byte> headerBuffer = stackalloc byte[5];

		output.Write(MemoryMarshal.AsBytes(regionBuffer));
		output.Write(data.Span[0x800..]); // write timestamps
		var chunks = archive.GetRecord($"{record.Name}.chunks", record.Version);
		if (chunks.Length == 0) {
			return output.ToArray();
		}

		var header = MemoryMarshal.Cast<byte, HeaderEntry>(data.Span[..0x800]);

		for (var i = 0x0; i < 0x400; ++i) {
			var buffer = chunks.Slice(header[i].Offset, header[i].Size);
			//recompress with zlib
			using var compressBuffer = MemoryPool<byte>.Shared.Rent(buffer.Length); // realistically it will never be larger than the whole file size.
			using var fixedHandle = compressBuffer.Memory.Pin();
			using var compressedStream = new UnmanagedMemoryStream((byte*) fixedHandle.Pointer, buffer.Length);

			using var bufPin = buffer.Pin();
			using var bufferStream = new UnmanagedMemoryStream((byte*) bufPin.Pointer, buffer.Length);
			using var zlib = new ZLibStream(bufferStream, CompressionLevel.Fastest);
			zlib.CopyTo(compressedStream);

			var encodedOffset = ((uint) output.Position >> 12) << 8;
			var len = (uint) compressedStream.Position + 5; // stream len + size + encoding
			var encodedSize = (byte) ((len.Align(0x1000) >> 12) & 0xFF);
			regionBuffer[i] = BinaryPrimitives.ReverseEndianness(encodedOffset | encodedSize);

			BinaryPrimitives.WriteUInt32BigEndian(headerBuffer, (uint) compressedStream.Position + 1);
			headerBuffer[4] = 2; // zlib
			output.Write(headerBuffer);
			output.Write(compressBuffer.Memory.Span[..(int) compressedStream.Position]);
			output.SetLength(output.Length.Align(0x1000));
		}

		output.Position = 0x0;
		output.Write(MemoryMarshal.AsBytes(regionBuffer));

		return output.ToArray();
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HeaderEntry : IEquatable<HeaderEntry> {
		public int Offset;
		public int Size;

		public override bool Equals(object? obj) => obj is HeaderEntry headerEntry && Equals(headerEntry);
		public override int GetHashCode() => HashCode.Combine(Offset, Size);
		public static bool operator ==(HeaderEntry left, HeaderEntry right) => left.Equals(right);
		public static bool operator !=(HeaderEntry left, HeaderEntry right) => !(left == right);
		public bool Equals(HeaderEntry other) => other.Offset == Offset && other.Size == Size;
	}
}
