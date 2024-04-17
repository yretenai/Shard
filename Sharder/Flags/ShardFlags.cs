// SPDX-License-Identifier: MPL-2.0

using DragonLib.CommandLine;

namespace Sharder.Flags;

internal record ShardFlags : CommandLineFlags {
	[Flag("path", Positional = 0, Help = "Path the shard files are stored", IsRequired = true)]
	public string ShardPath { get; set; } = null!;

	[Flag("name", Positional = 1, Help = "Name of the archive", IsRequired = true)]
	public string Name { get; set; } = null!;
}

internal record ShardIOFlags : ShardFlags {
	[Flag("paths", Positional = 2, Help = "The paths to process")]
	public string? Path { get; set; } = null!;

	[Flag("version", Positional = 3, Help = "The version of to save as")]
	public string? Version { get; set; }
}

internal record ShardIFlags : ShardFlags {
	[Flag("version", Positional = 2, Help = "The version of to save as")]
	public string? Version { get; set; }
}
