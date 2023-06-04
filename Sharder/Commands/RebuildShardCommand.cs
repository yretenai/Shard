// SPDX-License-Identifier: MPL-2.0

using DragonLib.CommandLine;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardFlags), "rebuild", "Rebuilds TOC to latest version")]
public record RebuildShardCommand : ShardCommand {
	public RebuildShardCommand(ShardFlags flags) : base(flags, false) {
		// rebuild happens anyway on load, we just have to save it.
		Archive.Flush();
	}
}
