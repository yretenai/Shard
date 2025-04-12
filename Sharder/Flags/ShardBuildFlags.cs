// SPDX-License-Identifier: MPL-2.0

using System.IO.Compression;
using DragonLib.CommandLine;
using Shard;
using Waterfall.Compression;

namespace Sharder.Flags;

internal record ShardBuildFlags : ShardIOFlags {
	[Flag("c", Aliases = ["com", "type"], Help = "Compression Type to use")]
	public CompressionType CompressionType { get; set; } = ShardArchive.DEFAULT_COMPRESSION;

	[Flag("l", Aliases = ["level"], Help = "Compression Type to use")]
	public CompressionLevel CompressionLevel { get; set; } = ShardArchive.DEFAULT_COMPRESSION_LEVEL;

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
