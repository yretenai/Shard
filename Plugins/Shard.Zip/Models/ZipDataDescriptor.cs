// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.Zip.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZipDataDescriptor {
	public uint CRC32 { get; init; }
	public uint CompressedSize { get; init; }
	public uint UncompressedSize { get; init; }
}
