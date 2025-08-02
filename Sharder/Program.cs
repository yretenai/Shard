// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using DragonLib.CommandLine;
using Serilog;
using Sharder.Commands;

namespace Sharder;

internal class Program {
	private static void Main() {
		Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
		using var command = Command.Run<ShardCommand>(out _, out _);
	}
}
