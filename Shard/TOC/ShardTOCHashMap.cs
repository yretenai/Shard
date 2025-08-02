// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCHashMap {
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }
}
