// SPDX-License-Identifier: MPL-2.0

using Shard.SDK.Models;

namespace Shard.SDK;

public interface ShardPlugin {
	public bool CanRecode { get; }
	public bool CanProcess(Stream stream, string path, ShardRecordMetadata metadata);
	public void Decode(Stream stream, string path, IShardArchive archive, ShardRecordMetadata metadata);
	public Memory<byte> Encode(Memory<byte> data, IShardRecord record, IShardArchive archive);
}
