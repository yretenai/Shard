// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using System.Runtime.InteropServices;
using Shard.TOC;
using Shard.TOC.V3;

namespace Shard;

public partial class ShardArchive {
	private void LoadTOCV3(Stream toc) {
		Span<ShardTOCCompressInfo> header = stackalloc ShardTOCCompressInfo[1];
		toc.ReadExactly(MemoryMarshal.AsBytes(header));

		if (header[0].CompressType != ShardCompressType.None) {
			using var fullBuffer = MemoryPool<byte>.Shared.Rent(header[0].CompressSize);
			var slice = fullBuffer.Memory.Span[..header[0].CompressSize];
			toc.ReadExactly(slice);
			using var newToc = new MemoryStream();
			newToc.Write(DecompressData(header[0].CompressType, slice, out var disposable));
			newToc.Position = 0;
			disposable?.Dispose();
			LoadTOCV2(newToc);
		} else {
			LoadTOCV2(toc);
		}
	}
}
