// SPDX-License-Identifier: MPL-2.0

using DragonLib;
using DragonLib.CommandLine;
using Serilog;
using Shard.SDK.Models;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardFlags), "stats", "Lists stats and versions of shard")]
public record StatsCommand : ShardCommand {
	public StatsCommand(ShardFlags flags) : base(flags) {
		var occurences = ((IShardArchive) Archive).Records.SelectMany(x => x.BlockHashes).GroupBy(x => x).ToDictionary(x => x.Key, y => y.Count());
		foreach (var version in Archive.Versions) {
			var msize = Archive.GetRecordsForVersion(version).Sum(x => (long) x.Record.Size);
			var csize = Archive.GetRecordsForVersion(version).Select(x => x.BlockHashes.Select(y => Archive.Blocks[Archive.BlockHashMap[y]])).Sum(x => x.Sum(y => (long) y.Footer.CompressedSize));
			var dsize = Archive.GetRecordsForVersion(version).Select(x => x.BlockHashes.Where(y => occurences[y] == 1).Select(y => Archive.Blocks[Archive.BlockHashMap[y]])).Sum(x => x.Sum(y => (long) y.Footer.CompressedSize));
			Log.Information("[{Version}] {Size} uncompressed, {CSize} compressed, {DSize} original", version, msize.GetHumanReadableBytes(), csize.GetHumanReadableBytes(), dsize.GetHumanReadableBytes());
		}

		Log.Information("--------------------------");
	}
}
