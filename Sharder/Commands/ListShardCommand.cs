// SPDX-License-Identifier: MPL-2.0

using System.Globalization;
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
			var blockSize = record.BlockHashes.Select(x => (long) Archive.Blocks[Archive.BlockHashMap[x]].Footer.CompressedSize).Sum();
			var ratio = (blockSize / (float) record.Record.Size * 100).ToString("F2", CultureInfo.InvariantCulture);
			Log.Information("[{Version}] {Path} - {Size} ({Blocks} blocks. {BlockSize} - {Ratio}%) - {Hash}", version, record.Name, record.Record.Size.GetHumanReadableBytes(), record.BlockHashes.Count(), blockSize.GetHumanReadableBytes(), ratio, record.Hash.ToString());
		}

		Log.Information("--------------------------");
	}
}
