// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using System.Runtime.InteropServices;
using Shard.TOC;
using Waterfall.Compression;

namespace Shard;

public partial class ShardArchive {
	private void LoadTOCV3(Stream toc) {
		Span<ShardTOCCompressInfo> header = stackalloc ShardTOCCompressInfo[1];
		toc.ReadExactly(MemoryMarshal.AsBytes(header));

	#pragma warning disable CS0618 // Type or member is obsolete
		var compressType = ((ShardLegacyCompressType) header[0].CompressType).ToWaterfall();
	#pragma warning restore CS0618 // Type or member is obsolete

		if (compressType != CompressionType.None) {
			using var fullBuffer = MemoryPool<byte>.Shared.Rent(header[0].CompressSize);
			var slice = fullBuffer.Memory[..header[0].CompressSize];
			toc.ReadExactly(slice.Span);
			using var newToc = new MemoryStream();
			newToc.Write(DecompressData(compressType, slice, (int) header[0].Size, out var disposable).Span);
			newToc.Position = 0;
			disposable?.Dispose();
			LoadTOCV2(newToc);
		} else {
			LoadTOCV2(toc);
		}
	}
}
