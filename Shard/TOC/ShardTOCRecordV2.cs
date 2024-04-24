// SPDX-License-Identifier: MPL-2.0

using Blake3;
using Shard.SDK.Models;

namespace Shard.TOC;

public record struct ShardTOCRecordV2 {
	public int NameIndex { get; init; }
	public int VersionIndex { get; init; }
	public Hash Hash { get; init; }
	public int Size { get; init; }
	public int BlockIndex { get; init; }
	public int BlockCount { get; init; }
	public int EncoderIndex { get; init; }
	public ShardRecordFlags Flags { get; set; }

	public ShardTOCRecord ToLatest() {
		return new ShardTOCRecord {
			NameIndex = NameIndex,
			VersionIndex = VersionIndex,
			Hash = Hash,
			Size = Size,
			BlockIndex = BlockIndex,
			BlockCount = BlockCount,
			EncoderIndex = EncoderIndex,
			Flags = Flags,
		};
	}
}
