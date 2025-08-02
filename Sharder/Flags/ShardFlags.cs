// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

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

	[Flag("in-version", Positional = 3, Help = "The version to process")]
	public string? InVersion { get; set; }
}

internal record ShardIFlags : ShardFlags {
	[Flag("in-version", Positional = 2, Help = "The version to process")]
	public string? InVersion { get; set; }
}
