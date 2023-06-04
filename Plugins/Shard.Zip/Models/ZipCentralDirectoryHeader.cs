// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.Zip.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZipCentralDirectoryHeader {
	public uint Magic { get; init; }
	public ushort VersionMadeBy { get; init; }
	public ushort VersionNeeded { get; init; }
	public ushort Flags { get; init; }
	public ZipCompression Compression { get; init; }
	public ushort ModTime { get; init; }
	public ushort ModDate { get; init; }
	public uint CRC32 { get; init; }
	public uint CompressedSize { get; init; }
	public uint UncompressedSize { get; init; }
	public ushort FileNameLength { get; init; }
	public ushort ExtraFieldLength { get; init; }
	public ushort CommentLength { get; init; }
	public ushort DiskNumber { get; init; }
	public ushort InternalFileAttributes { get; init; }
	public uint ExternalFileAttributes { get; init; }
	public uint Offset { get; init; }
}
