// SPDX-License-Identifier: MPL-2.0

using Blake3;
using Shard.SDK.Models;
using Shard.TOC.V1;

namespace Shard.TOC.V2;

public record struct ShardTOCRecord {
	public ShardTOCRecord() { }

	public ShardTOCRecord(ShardTOCRecordV1 v1) {
		NameIndex = v1.NameIndex;
		VersionIndex = v1.VersionIndex;
		Hash = v1.Hash;
		Size = v1.Size;
		BlockIndex = v1.BlockIndex;
		BlockCount = v1.BlockCount;
		EncoderIndex = -1;
		Flags = ShardRecordFlags.None;
	}

	public int NameIndex { get; init; }
	public int VersionIndex { get; init; }
	public Hash Hash { get; init; }
	public int Size { get; init; }
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }
	public int EncoderIndex { get; init; }
	public ShardRecordFlags Flags { get; set; }
}
