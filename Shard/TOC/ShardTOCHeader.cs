// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Blake3;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCHeader {
	public ulong Magic { get; init; }
	public ShardTOCVersion Version { get; set; }
	public int RecordCount { get; set; }
	public int BlockSize { get; init; }
	public int ShardCount { get; set; }
	public int VersionCount { get; set; }
	public int NameCount { get; set; }
	public int BlockCount { get; set; }
	public int BlockIndiceCount { get; set; }
	public int HashMapCount { get; set; }
	public long ShardSize { get; set; }
	public ushort HeaderAlignment { get; set; }
	public ushort Alignment { get; set; }
	public Hash Checksum { get; set; }
}
