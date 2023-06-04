// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK.Models;

public enum ShardRecordFlags : uint {
	None = 0,
	Hidden = 1,
	Meta = 2, // has no data
}
