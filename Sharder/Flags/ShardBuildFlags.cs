// SPDX-License-Identifier: MPL-2.0

using DragonLib.CommandLine;
using Shard;
using Shard.TOC;

namespace Sharder.Flags;

internal record ShardBuildFlags : ShardIOFlags {
	[Flag("com", Help = "Compression Type to use")]
	public ShardCompressType CompressionType { get; set; } = ShardCompressType.ZStd;

	[Flag("block-size", Help = "Size in bytes of individual blocks")]
	public int BlockSize { get; set; } = ShardArchive.DEFAULT_BLOCK_SIZE;

	[Flag("shard-size", Help = "Size in bytes of shard streams")]
	public long ShardSize { get; set; } = ShardArchive.DEFAULT_SHARD_SIZE;

	[Flag("filter", Help = "Filter directory using this mask")]
	public string Filter { get; set; } = "*";

	[Flag("autodetect", Help = "Use filename as the version")]
	public bool FilenameAsVersion { get; set; }

	[Flag("time", Help = "Use time as the version")]
	public bool TimeAsVersion { get; set; }

	[Flag("recursive", Aliases = ["r"], Help = "Process directories recursively")]
	public bool Recursive { get; set; }

	public DateTimeOffset InitTime { get; } = DateTimeOffset.UtcNow;
}
