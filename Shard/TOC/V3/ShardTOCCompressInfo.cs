// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.TOC.V3;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCCompressInfo {
	public ShardCompressType CompressType { get; init; }
	public int CompressSize { get; init; }
	public long Size { get; init; }
}
