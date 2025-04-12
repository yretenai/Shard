// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Blake3;
using Waterfall.Compression;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardBlockHeader {
	public ushort HeaderSize { get; init; }
	public ShardBlockVersion Version { get; init; }
	public int Size { get; init; }
	public int CompressedSize { get; init; }
	public CompressionType CompressionType { get; set; }
	public Hash BlockHash { get; init; }
	public Hash CompressedBlockHash { get; init; }

	public CompressionType LatestCompressionType {
		get {
			if (Version >= ShardBlockVersion.Waterfall) {
				return CompressionType;
			}

		#pragma warning disable CS0618
			return ((ShardLegacyCompressType) CompressionType).ToWaterfall();
		#pragma warning restore CS0618
		}
	}
}
