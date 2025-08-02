// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using DragonLib;
using DragonLib.CommandLine;
using Serilog;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardIOFlags), "extract", "Extracts files to a shard")]
internal record ExtractShardCommand : ShardCommand {
	public ExtractShardCommand(ShardIOFlags flags) : base(flags) {
		if (string.IsNullOrEmpty(flags.Path)) {
			throw new InvalidOperationException("Path must be defined.");
		}

		if (string.IsNullOrEmpty(flags.InVersion)) {
			throw new InvalidOperationException("Version must be defined.");
		}

		Archive.SetVersion(flags.InVersion);

		foreach (var record in Archive.GetRecordsForVersion(flags.InVersion)) {
			var destPath = Path.Combine(flags.Path, record.Name);
			destPath.EnsureDirectoryExists();

			var data = Archive.GetRecord(record);
			Log.Information("Writing {Name}", record.Name);
			using var stream = new FileStream(destPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.SetLength(0);
			stream.Write(data.Span);
		}
	}
}
