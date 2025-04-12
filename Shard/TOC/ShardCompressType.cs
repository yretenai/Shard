// SPDX-License-Identifier: MPL-2.0

using Waterfall.Compression;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Shard.TOC;

[Obsolete("Use Waterfall.Compression.CompressionType")]
public enum ShardLegacyCompressType {
	None,
	ZStd,
	LZO,
	LZ4,
	Snappy,
	ZLib,
	Custom = -1,
}

public static class ShardCompressTypeExtensions {
	public static CompressionType ToWaterfall(this ShardLegacyCompressType type) =>
		type switch {
			ShardLegacyCompressType.None => CompressionType.None,
			ShardLegacyCompressType.ZStd => CompressionType.Zstd,
			ShardLegacyCompressType.LZO => CompressionType.LZO2,
			ShardLegacyCompressType.LZ4 => CompressionType.LZ4HC,
			ShardLegacyCompressType.Snappy => throw new NotSupportedException(),
			ShardLegacyCompressType.ZLib => CompressionType.Zlib,
			ShardLegacyCompressType.Custom => (CompressionType) (-1),
			_ => throw new NotSupportedException(),
		};

	public static ShardLegacyCompressType ToShard(this CompressionType type) {
		if (type == (CompressionType) (-1)) {
			return (ShardLegacyCompressType) type;
		}

		return type switch {
			CompressionType.None => ShardLegacyCompressType.None,
			CompressionType.Zstd => ShardLegacyCompressType.ZStd,
			CompressionType.LZO2 => ShardLegacyCompressType.LZO,
			CompressionType.LZ4 => ShardLegacyCompressType.LZ4,
			CompressionType.LZ4HC => ShardLegacyCompressType.LZ4,
			CompressionType.Zlib => ShardLegacyCompressType.ZLib,
			_ => throw new NotSupportedException(),
		};
	}
}
