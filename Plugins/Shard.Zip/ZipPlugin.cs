// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;
using Shard.SDK;
using Shard.SDK.Models;
using Shard.Zip.Models;

namespace Shard.Zip;

[ShardPlugin("aq.chronovore.shard.zip", "Shard.Zip", "chronovore", "1.0.0", "Processes zip entries")]
public class ZipPlugin : ShardPlugin {
	public bool CanRecode => false;

	public bool CanProcess(Stream stream, string path, ShardRecordMetadata metadata) {
		var pos = stream.Position;
		try {
			if (stream.Length < Unsafe.SizeOf<ZipEndOfCentralDirectory>()) {
				return false;
			}

			Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<ZipEndOfCentralDirectory>()];
			stream.Seek(-buffer.Length, SeekOrigin.End);
			stream.ReadExactly(buffer);
			var eocd = MemoryMarshal.Read<ZipEndOfCentralDirectory>(buffer);
			return eocd.Magic == 0x06054B50;
		} catch {
			return false;
		} finally {
			stream.Position = pos;
		}
	}

	public void Decode(Stream stream, string path, IShardArchive archive, ShardRecordMetadata metadata) {
		using var zip = new ZipFile(stream);

		foreach (var entry in zip.Entries) {
			if (entry.Length > int.MaxValue) {
				Log.Warning("[Zip] File {Path} is too large", entry.Path);
				continue;
			}

			using var data = zip.Open(entry);
			using var buffer = MemoryPool<byte>.Shared.Rent((int) entry.Length);
			data.ReadExactly(buffer.Memory.Span[..(int) entry.Length]);
			var meta = new ShardRecordMetadata {
				Timestamp = entry.Header.ModDateTime.ToUnixTimeMilliseconds(),
				Attributes = entry.Header.ExternalFileAttributes,
				Permissions = (entry.Header.ExternalFileAttributes >> 16) & 0x1FF,
			};
			archive.ProcessFile(entry.Path, buffer.Memory[..(int) entry.Length], meta);
		}
	}

	public Memory<byte> Encode(Memory<byte> data, IShardRecord record, IShardArchive archive) => throw new NotSupportedException();
}
