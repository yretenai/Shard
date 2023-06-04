// SPDX-License-Identifier: MPL-2.0

using Blake3;
using Shard.SDK.Models;
using Shard.TOC.V2;

namespace Shard;

public readonly record struct ShardRecord : IShardRecord {
	public ShardTOCRecord Record { get; init; }
	public string Name { get; init; }
	public string Version { get; init; }
	public string? Encoder { get; init; }
	public Hash Hash { get; init; }
	public IEnumerable<Hash> BlockHashes { get; init; }
	public ShardRecordFlags Flags { get; init; }
}
