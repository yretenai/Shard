// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Blake3;
using DragonLib;
using Shard.TOC;
using Waterfall.Compression;

namespace Shard;

public partial class ShardArchive {
	private void LoadTOCV4(Stream toc, bool waterfall) {
		Span<ShardTOCCompressInfo> header = stackalloc ShardTOCCompressInfo[1];
		toc.ReadExactly(MemoryMarshal.AsBytes(header));

		var theToc = toc;
	#pragma warning disable CS0618 // Type or member is obsolete
		var compressType = waterfall ? header[0].CompressType : ((ShardLegacyCompressType) header[0].CompressType).ToWaterfall();
	#pragma warning restore CS0618 // Type or member is obsolete
		try {
			if (compressType != CompressionType.None) {
				using var fullBuffer = MemoryPool<byte>.Shared.Rent(header[0].CompressSize);
				var slice = fullBuffer.Memory[..header[0].CompressSize];
				toc.ReadExactly(slice.Span);
				theToc = new MemoryStream();
				theToc.Write(DecompressData(compressType, slice, (int) header[0].Size, out var disposable).Span);
				theToc.Position = 0;
				disposable?.Dispose();
			}

			var recordSize = Unsafe.SizeOf<ShardTOCRecord>() * Header.RecordCount;
			var versionSize = sizeof(int) * Header.VersionCount;
			var nameSize = sizeof(int) * Header.NameCount;
			var blockSize = Unsafe.SizeOf<ShardTOCBlock>() * Header.BlockCount;
			var blockIndiceSize = sizeof(int) * Header.BlockIndiceCount;
			var hashMapSize = Unsafe.SizeOf<KeyValuePair<Hash, ShardTOCHashMap>>() * Header.HashMapCount;

			if (Header.RecordCount > 0) {
				using var rented = MemoryPool<byte>.Shared.Rent(recordSize);
				theToc.ReadExactly(rented.Memory.Span[..recordSize]);
				var records = MemoryMarshal.Cast<byte, ShardTOCRecord>(rented.Memory.Span[..recordSize]);
				Records.AddRange(records);
				theToc.Align(Header.HeaderAlignment);
			}

			if (Header.VersionCount > 0) {
				using var rented = MemoryPool<byte>.Shared.Rent(versionSize);
				theToc.ReadExactly(rented.Memory.Span[..versionSize]);
				theToc.Align(Header.HeaderAlignment);
				var versions = MemoryMarshal.Cast<byte, int>(rented.Memory.Span[..versionSize]);
				foreach (var length in versions) {
					using var rentedStr = MemoryPool<byte>.Shared.Rent(length);
					theToc.ReadExactly(rentedStr.Memory.Span[..length]);
					Versions.Add(Encoding.UTF8.GetString(rentedStr.Memory.Span[..length]));
				}

				theToc.Align(Header.HeaderAlignment);
			}

			if (Header.NameCount > 0) {
				using var rented = MemoryPool<byte>.Shared.Rent(nameSize);
				theToc.ReadExactly(rented.Memory.Span[..nameSize]);
				theToc.Align(Header.HeaderAlignment);
				var names = MemoryMarshal.Cast<byte, int>(rented.Memory.Span[..nameSize]);
				foreach (var length in names) {
					using var rentedStr = MemoryPool<byte>.Shared.Rent(length);
					theToc.ReadExactly(rentedStr.Memory.Span[..length]);
					Names.Add(Encoding.UTF8.GetString(rentedStr.Memory.Span[..length]));
				}

				theToc.Align(Header.HeaderAlignment);
			}

			if (Header.BlockCount > 0) {
				using var rented = MemoryPool<byte>.Shared.Rent(blockSize);
				theToc.ReadExactly(rented.Memory.Span[..blockSize]);
				theToc.Align(Header.HeaderAlignment);
				var blocks = MemoryMarshal.Cast<byte, ShardTOCBlock>(rented.Memory.Span[..blockSize]);
				Blocks.AddRange(blocks);
				BlockHashMap = Blocks.Select((x, Index) => (Hash: x.Footer.BlockHash, Index)).ToDictionary(x => x.Hash, x => x.Index);
			}

			if (Header.BlockIndiceCount > 0) {
				using var rented = MemoryPool<byte>.Shared.Rent(blockIndiceSize);
				theToc.ReadExactly(rented.Memory.Span[..blockIndiceSize]);
				theToc.Align(Header.HeaderAlignment);
				var indices = MemoryMarshal.Cast<byte, int>(rented.Memory.Span[..blockIndiceSize]);
				BlockIndices.AddRange(indices);
			}

			if (Header.HashMapCount > 0) {
				using var rented = MemoryPool<byte>.Shared.Rent(hashMapSize);
				theToc.ReadExactly(rented.Memory.Span[..hashMapSize]);
				theToc.Align(Header.HeaderAlignment);
				var hashMaps = MemoryMarshal.Cast<byte, KeyValuePair<Hash, ShardTOCHashMap>>(rented.Memory.Span[..hashMapSize]);
				foreach (var (hash, map) in hashMaps) {
					RecordHashMap.Add(hash, map);
				}
			}
		} finally {
			if (compressType != CompressionType.None) {
				theToc.Dispose();
			}
		}
	}
}
