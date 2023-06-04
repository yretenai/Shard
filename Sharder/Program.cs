// SPDX-License-Identifier: MPL-2.0

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
