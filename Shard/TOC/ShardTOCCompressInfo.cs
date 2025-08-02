// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Waterfall.Compression;

namespace Shard.TOC;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct ShardTOCCompressInfo {
	public CompressionType CompressType { get; init; }
	public int CompressSize { get; init; }
	public long Size { get; init; }
}
