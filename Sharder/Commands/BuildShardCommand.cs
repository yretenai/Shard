// SPDX-License-Identifier: MPL-2.0

using DragonLib.CommandLine;
using Sharder.Flags;

namespace Sharder.Commands;

[Command(typeof(ShardBuildFlags), "build", "Adds files to a shard")]
internal record BuildShardCommand : ShardCommand {
	public BuildShardCommand(ShardBuildFlags flags) : base(flags, false) {
		if (string.IsNullOrEmpty(flags.Path)) {
			throw new InvalidOperationException("Path must be defined.");
		}

		if (new FileInfo(flags.Path).Attributes.HasFlag(FileAttributes.Directory)) {
			var baseUri = new Uri(flags.Path.TrimEnd('/', '\\') + '/');
			foreach (var file in Directory.GetFiles(flags.Path, flags.Filter, new EnumerationOptions {
				IgnoreInaccessible = true,
				MatchCasing = MatchCasing.PlatformDefault,
				RecurseSubdirectories = flags.Recursive,
				ReturnSpecialDirectories = false,
				MatchType = MatchType.Simple,
			})) {
				ResolveVersion(flags, file);
				var uri = new Uri(file);
				var relative = baseUri.MakeRelativeUri(uri);
				var localPath = Uri.UnescapeDataString(relative.ToString());
				using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				Archive.ProcessFile(localPath, stream);
			}
		} else {
			ResolveVersion(flags, flags.Path);
			using var stream = new FileStream(flags.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			Archive.ProcessFile(Path.GetFileName(flags.Path), stream);
		}

		Archive.Flush();
	}

	private void ResolveVersion(ShardBuildFlags flags, string file) {
		var constructed = new List<string?> {
			flags.Version,
		};

		if (flags.FilenameAsVersion) {
			constructed.Add(Path.GetFileNameWithoutExtension(file));
		}

		if (flags.TimeAsVersion) {
			constructed.Add(flags.InitTime.ToUnixTimeMilliseconds().ToString());
		}

		var version = string.Join("::", constructed.Where(x => x != null));

		if (!string.IsNullOrEmpty(version)) {
			Archive.SetVersion(version);
		}
	}
}
