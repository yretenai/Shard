// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using DragonLib.CommandLine;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardFlags), "rebuild", "Rebuilds TOC to latest version")]
internal record RebuildShardCommand : ShardCommand {
	public RebuildShardCommand(ShardFlags flags) : base(flags, false) =>
		// rebuild happens anyway on load, we just have to save it.
		Archive.Flush();
}
