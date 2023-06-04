// SPDX-License-Identifier: MPL-2.0

using System.Reflection;
using System.Runtime.Loader;
using Semver;
using Serilog;
using Shard.SDK;
using Shard.SDK.Models;

namespace Shard;

public static class ShardPluginEngine {
	static ShardPluginEngine() {
		LoadPluginFromAssembly(Assembly.GetExecutingAssembly());
		LoadFromDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "plugins"));
	}

	public static Dictionary<string, (ShardPlugin Plugin, ShardPluginAttribute Info)> Plugins { get; set; } = new();
	private static AssemblyLoadContext Context { get; } = new("ShardPlugin");

	private static void LoadFromDirectory(string path) {
		foreach (var dll in Directory.GetFiles(path, "*.dll", new EnumerationOptions {
			         RecurseSubdirectories = true,
			         ReturnSpecialDirectories = false,
			         IgnoreInaccessible = true,
			         MatchCasing = MatchCasing.PlatformDefault,
			         MatchType = MatchType.Simple,
		         })) {
			LoadPluginFromAssembly(Context.LoadFromAssemblyPath(dll));
		}
	}

	private static void LoadPluginFromAssembly(Assembly asm) {
		foreach (var plugin in asm.GetExportedTypes().Where(x => x.IsAssignableTo(typeof(ShardPlugin)) && x.GetCustomAttribute<ShardPluginAttribute>() != null)) {
			var info = plugin.GetCustomAttribute<ShardPluginAttribute>()!;
			if (Plugins.TryGetValue(info.Id, out var existing)) {
				var version = SemVersion.Parse(info.Version);
				var existingVersion = SemVersion.Parse(existing.Info.Version);
				if (version.ToVersion() > existingVersion.ToVersion()) {
					if (existing.Plugin is IDisposable disposable) {
						disposable.Dispose();
					}
				} else {
					continue;
				}
			}

			var dll = (ShardPlugin) Activator.CreateInstance(plugin)!;
			Plugins[info.Id] = (dll, info);
		}
	}

	public static void Decode(string name, Stream data, ShardArchive archive) {
		foreach (var plugin in Plugins.Values.OrderByDescending(x => x.Info.Priority).Select(x => x.Plugin)) {
			if (plugin.CanProcess(data, name)) {
				plugin.Decode(data, name, archive);
				return;
			}
		}
	}

	public static Span<byte> Encode(string pluginName, IShardRecord record, Span<byte> data, ShardArchive archive) {
		if (Plugins.TryGetValue(pluginName, out var plugin) && plugin.Plugin.CanRecode) {
			return plugin.Plugin.Encode(data, record, archive);
		}

		return data;
	}

	public static void Dump() {
		Log.Information("--------------------------");
		Log.Information("Plugins:");
		Log.Information("--------------------------");
		foreach (var (_, plugin) in Plugins.Values.OrderByDescending(x => x.Info.Priority)) {
			Log.Information("{Id}@{Version}: {Name} - {Description} by {Author}", plugin.Id, plugin.Version, plugin.Name, plugin.Description, plugin.Author);
		}

		Log.Information("--------------------------");
	}
}
