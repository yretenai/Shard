// SPDX-License-Identifier: MPL-2.0

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
