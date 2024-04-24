// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK.Models;

public record struct ShardRecordMetadata() {
	public string? Encoder { get; set; } = null;
	public ShardRecordFlags Flags { get; set; } = ShardRecordFlags.None;
	public long Timestamp { get; set; } = 0;
	public ulong Permissions { get; set; } = 0x1FF;
	public ulong Attributes { get; set; } = 0;
}
