// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.SDK.Models;

public record struct ShardRecordMetadata() {
	public string? Encoder { get; set; } = null;
	public ShardRecordFlags Flags { get; set; } = ShardRecordFlags.None;
	public long Timestamp { get; set; } = 0;
	public ulong Permissions { get; set; } = 0x1FF;
	public ulong Attributes { get; set; } = 0;
}
