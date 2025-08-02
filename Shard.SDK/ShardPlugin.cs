// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using Shard.SDK.Models;

namespace Shard.SDK;

public interface ShardPlugin {
	bool CanRecode { get; }
	bool CanProcess(Stream stream, string path, ShardRecordMetadata metadata);
	void Decode(Stream stream, string path, IShardArchive archive, ShardRecordMetadata metadata);
	Memory<byte> Encode(Memory<byte> data, IShardRecord record, IShardArchive archive);
}
