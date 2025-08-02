// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.TOC;

public enum ShardBlockVersion : ushort {
	Initial = 1,
	Waterfall,
	Latest = Waterfall,
}
