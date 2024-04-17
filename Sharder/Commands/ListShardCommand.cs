// SPDX-License-Identifier: MPL-2.0

using DragonLib;
using DragonLib.CommandLine;
using Serilog;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardIFlags), "list", "Lists files in a shard")]
internal record ListShardCommand : ShardCommand {
	public ListShardCommand(ShardIFlags flags) : base(flags) {
		if (!string.IsNullOrEmpty(flags.Version)) {
			ListVersion(flags.Version);
			return;
		}

		foreach (var version in Archive.Versions) {
			ListVersion(version);
		}
	}

	private void ListVersion(string version) {
		foreach (var record in Archive.GetRecordsForVersion(version)) {
			Log.Information("[{Version}] {Path} - {Size} ({Blocks} blocks) - {Hash}", version, record.Name, record.Record.Size.GetHumanReadableBytes(), record.BlockHashes.Count(), record.Hash.ToString());
		}

		Log.Information("--------------------------");
	}
}
