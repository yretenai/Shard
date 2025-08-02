// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using Blake3;

namespace Shard.SDK.Models;

// ReSharper disable UnusedMemberInSuper.Global
public interface IShardRecord {
	string Name { get; }
	string Version { get; }
	string? Encoder { get; }
	Hash Hash { get; }
	IEnumerable<Hash> BlockHashes { get; }
	ShardRecordFlags Flags { get; }
}
