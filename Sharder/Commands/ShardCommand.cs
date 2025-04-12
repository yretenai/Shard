// SPDX-License-Identifier: MPL-2.0

using Shard;
using Sharder.Flags;

namespace Sharder.Commands;

internal abstract record ShardCommand : IDisposable {
	protected ShardCommand(ShardFlags flags, bool @readonly = true) {
		var rebuild = flags as ShardBuildFlags;
		// ReSharper disable once WithExpressionModifiesAllMembers
		var options = ShardOptions.Default with {
			BlockSize = rebuild?.BlockSize ?? ShardOptions.Default.BlockSize,
			ShardSize = rebuild?.ShardSize ?? ShardOptions.Default.ShardSize,
			CompressType = rebuild?.CompressionType ?? ShardOptions.Default.CompressType,
			IsReadOnly = @readonly,
		};

		Archive = new ShardArchive(flags.Name, flags.ShardPath, options);
	}

	protected ShardArchive Archive { get; }

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~ShardCommand() => Dispose(false);

	protected virtual void Dispose(bool disposing) {
		if (disposing) {
			Archive.Dispose();
		}
	}
}
