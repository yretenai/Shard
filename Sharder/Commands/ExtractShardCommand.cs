// SPDX-License-Identifier: MPL-2.0

using DragonLib;
using DragonLib.CommandLine;
using Serilog;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardIOFlags), "extract", "Extracts files to a shard")]
public record ExtractShardCommand : ShardCommand {
	public ExtractShardCommand(ShardIOFlags flags) : base(flags) {
		if (string.IsNullOrEmpty(flags.Path)) {
			throw new InvalidOperationException("Path must be defined.");
		}

		if (string.IsNullOrEmpty(flags.Version)) {
			throw new InvalidOperationException("Version must be defined.");
		}

		Archive.SetVersion(flags.Version);

		foreach (var record in Archive.GetRecordsForVersion(flags.Version)) {
			var destPath = Path.Combine(flags.Path, record.Name);
			destPath.EnsureDirectoryExists();

			var data = Archive.GetRecord(record);
			Log.Information("Writing {Name}", record.Name);
			using var stream = new FileStream(destPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			stream.SetLength(0);
			stream.Write(data);
		}
	}
}
