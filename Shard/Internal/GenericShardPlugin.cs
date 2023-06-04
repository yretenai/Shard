// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using Shard.SDK;
using Shard.SDK.Models;

namespace Shard.Internal;

[ShardPlugin("aq.chronovore.shard.generic", "Shard.Generic", "chronovore", "1.0.0", "Processes generic files", Priority = int.MinValue)]
public class GenericShardPlugin : ShardPlugin {
	public bool CanRecode => true;

	public bool CanProcess(Stream stream, string path) => stream.Length <= int.MaxValue;

	public void Decode(Stream stream, string path, IShardArchive archive) {
		if (stream.Length > int.MaxValue) {
			throw new Exception("[Generic] File is too large");
		}

		using var buffer = MemoryPool<byte>.Shared.Rent((int) stream.Length);
		stream.ReadExactly(buffer.Memory.Span[..(int) stream.Length]);
		archive.AddRecord(path, buffer.Memory.Span[..(int) stream.Length]);
	}

	public Span<byte> Encode(Span<byte> data, IShardRecord record, IShardArchive archive) => data;
}
