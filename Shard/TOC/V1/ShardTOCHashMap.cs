// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.TOC.V1;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCHashMap {
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }
}
