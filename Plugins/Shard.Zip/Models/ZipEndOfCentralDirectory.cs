// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.Zip.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZipEndOfCentralDirectory {
	public uint Magic { get; init; }
	public ushort DiskNumber { get; init; }
	public ushort DirectoryDiskNumber { get; init; }
	public ushort NumberOfDirectoryRecords { get; init; }
	public ushort TotalNumberOfDirectoryRecords { get; init; }
	public uint DirectorySize { get; init; }
	public uint DirectoryOffset { get; init; }
	public ushort CommentLength { get; init; }

	public bool IsZip64 => DirectoryOffset == uint.MaxValue;
}
