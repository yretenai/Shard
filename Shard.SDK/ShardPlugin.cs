// SPDX-License-Identifier: MPL-2.0

using Shard.SDK.Models;

namespace Shard.SDK;

public interface ShardPlugin {
	public bool CanRecode { get; }
	public bool CanProcess(Stream stream, string path);
	public void Decode(Stream stream, string path, IShardArchive archive);
	public Span<byte> Encode(Span<byte> data, IShardRecord record, IShardArchive archive);
}
