// SPDX-License-Identifier: MPL-2.0

namespace Shard.TOC;

public enum ShardBlockVersion : ushort {
	Initial = 1,
	LatestPlusOne,
	Latest = LatestPlusOne - 1,
}
