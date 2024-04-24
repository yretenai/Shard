// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using Shard.SDK;
using Shard.SDK.Models;

namespace Shard.Internal;

[ShardPlugin("aq.chronovore.shard.generic", "Shard.Generic", "chronovore", "1.0.0", "Processes generic files", Priority = int.MinValue)]
public class GenericShardPlugin : ShardPlugin {
	public bool CanRecode => true;

	public bool CanProcess(Stream stream, string path, ShardRecordMetadata metadata) => stream.Length <= int.MaxValue;

	public void Decode(Stream stream, string path, IShardArchive archive, ShardRecordMetadata metadata) {
		if (stream.Length > int.MaxValue) {
			throw new Exception("[Generic] File is too large");
		}

		using var buffer = MemoryPool<byte>.Shared.Rent((int) stream.Length);
		stream.ReadExactly(buffer.Memory.Span[..(int) stream.Length]);

		var meta = metadata;
		if (stream is FileStream fs) {
			var info = new FileInfo(fs.Name);
			meta = metadata with {
				Timestamp = new DateTimeOffset(info.CreationTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
				Permissions = (ulong) info.UnixFileMode,
				Attributes = (ulong) info.Attributes,
			};
		}

		archive.AddRecord(path, buffer.Memory.Span[..(int) stream.Length], meta);
	}

	public Span<byte> Encode(Span<byte> data, IShardRecord record, IShardArchive archive) => data;
}
