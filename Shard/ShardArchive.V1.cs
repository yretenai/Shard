// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Blake3;
using DragonLib;
using Shard.TOC;

namespace Shard;

public partial class ShardArchive {
	private void LoadTOCV1(Stream toc) {
		var recordSize = Unsafe.SizeOf<ShardTOCRecordV1>() * Header.RecordCount;
		var versionSize = sizeof(int) * Header.VersionCount;
		var nameSize = sizeof(int) * Header.NameCount;
		var blockSize = Unsafe.SizeOf<ShardTOCBlock>() * Header.BlockCount;
		var blockIndiceSize = sizeof(int) * Header.BlockIndiceCount;
		var hashMapSize = Unsafe.SizeOf<KeyValuePair<Hash, ShardTOCHashMap>>() * Header.HashMapCount;

		if (Header.RecordCount > 0) {
			using var rented = MemoryPool<byte>.Shared.Rent(recordSize);
			toc.ReadExactly(rented.Memory.Span[..recordSize]);
			var records = MemoryMarshal.Cast<byte, ShardTOCRecordV1>(rented.Memory.Span[..recordSize]);
			foreach (var record in records) {
				Records.Add(record.ToLatest());
			}

			toc.Align(Header.HeaderAlignment);
		}

		if (Header.VersionCount > 0) {
			using var rented = MemoryPool<byte>.Shared.Rent(versionSize);
			toc.ReadExactly(rented.Memory.Span[..versionSize]);
			toc.Align(Header.HeaderAlignment);
			var versions = MemoryMarshal.Cast<byte, int>(rented.Memory.Span[..versionSize]);
			foreach (var length in versions) {
				using var rentedStr = MemoryPool<byte>.Shared.Rent(length);
				toc.ReadExactly(rentedStr.Memory.Span[..length]);
				Versions.Add(Encoding.UTF8.GetString(rentedStr.Memory.Span[..length]));
			}

			toc.Align(Header.HeaderAlignment);
		}

		if (Header.NameCount > 0) {
			using var rented = MemoryPool<byte>.Shared.Rent(nameSize);
			toc.ReadExactly(rented.Memory.Span[..nameSize]);
			toc.Align(Header.HeaderAlignment);
			var names = MemoryMarshal.Cast<byte, int>(rented.Memory.Span[..nameSize]);
			foreach (var length in names) {
				using var rentedStr = MemoryPool<byte>.Shared.Rent(length);
				toc.ReadExactly(rentedStr.Memory.Span[..length]);
				Names.Add(Encoding.UTF8.GetString(rentedStr.Memory.Span[..length]));
			}

			toc.Align(Header.HeaderAlignment);
		}

		if (Header.BlockCount > 0) {
			using var rented = MemoryPool<byte>.Shared.Rent(blockSize);
			toc.ReadExactly(rented.Memory.Span[..blockSize]);
			toc.Align(Header.HeaderAlignment);
			var blocks = MemoryMarshal.Cast<byte, ShardTOCBlock>(rented.Memory.Span[..blockSize]);
			Blocks.AddRange(blocks);
			BlockHashMap = Blocks.Select((x, Index) => (Hash: x.Footer.BlockHash, Index)).ToDictionary(x => x.Hash, x => x.Index);
		}

		if (Header.BlockIndiceCount > 0) {
			using var rented = MemoryPool<byte>.Shared.Rent(blockIndiceSize);
			toc.ReadExactly(rented.Memory.Span[..blockIndiceSize]);
			toc.Align(Header.HeaderAlignment);
			var indices = MemoryMarshal.Cast<byte, int>(rented.Memory.Span[..blockIndiceSize]);
			BlockIndices.AddRange(indices);
		}

		if (Header.HashMapCount > 0) {
			using var rented = MemoryPool<byte>.Shared.Rent(hashMapSize);
			toc.ReadExactly(rented.Memory.Span[..hashMapSize]);
			toc.Align(Header.HeaderAlignment);
			var hashMaps = MemoryMarshal.Cast<byte, KeyValuePair<Hash, ShardTOCHashMap>>(rented.Memory.Span[..hashMapSize]);
			foreach (var (hash, map) in hashMaps) {
				RecordHashMap.Add(hash, map);
			}
		}
	}
}
