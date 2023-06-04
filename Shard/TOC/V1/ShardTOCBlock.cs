// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.TOC.V1;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCBlock {
	public long Offset { get; init; }
	public int ShardIndex { get; init; }
	public ShardBlockHeader Footer { get; set; }
}
