// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.TOC;

public enum ShardTOCVersion : uint {
	Initial = 1,
	AddEncoder,
	FixAlignment,
	CompressWholeTOC,
	StoreAttributes,
	CustomCompressor,
	Waterfall,
	Latest = Waterfall,
}
