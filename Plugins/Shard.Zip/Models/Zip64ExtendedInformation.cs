// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.Zip.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Zip64ExtendedInformation {
	public long UncompressedSize { get; init; }
	public long CompressedSize { get; init; }
	public long Offset { get; init; }
	public uint DiskNumber { get; init; }
}
