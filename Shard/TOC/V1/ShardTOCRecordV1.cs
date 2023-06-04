// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Blake3;

namespace Shard.TOC.V1;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCRecordV1 {
	public int NameIndex { get; init; }
	public int VersionIndex { get; init; }
	public Hash Hash { get; init; }
	public int Size { get; init; }
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }
}
