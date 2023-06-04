// SPDX-License-Identifier: MPL-2.0

using Blake3;

namespace Shard.SDK.Models;

// ReSharper disable UnusedMemberInSuper.Global
public interface IShardRecord {
	public string Name { get; }
	public string Version { get; }
	public string? Encoder { get; }
	public Hash Hash { get; }
	public IEnumerable<Hash> BlockHashes { get; }
	public ShardRecordFlags Flags { get; }
}
