// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Blake3;
using Shard.SDK.Models;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCRecordV4() {
	public int NameIndex { get; init; }
	public int VersionIndex { get; init; }
	public Hash Hash { get; init; }
	public int Size { get; init; }
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }
	public int EncoderIndex { get; init; } = -1;
	public ShardRecordFlags Flags { get; set; }
	public long Timestamp { get; set; }
	public ulong Permissions { get; set; } = 0x1FF;
	public ulong Attributes { get; set; }
}
