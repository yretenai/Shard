// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Waterfall.Compression;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCCompressInfo {
	public CompressionType CompressType { get; init; }
	public int CompressSize { get; init; }
	public long Size { get; init; }
}
