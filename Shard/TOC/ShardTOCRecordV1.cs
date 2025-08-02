// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Blake3;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct ShardTOCRecordV1 {
	public int NameIndex { get; init; }
	public int VersionIndex { get; init; }
	public Hash Hash { get; init; }
	public int Size { get; init; }
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }

	public ShardTOCRecord ToLatest() =>
		new() {
			NameIndex = NameIndex,
			VersionIndex = VersionIndex,
			Hash = Hash,
			Size = Size,
			BlockIndex = BlockIndex,
			BlockCount = BlockCount,
		};
}
