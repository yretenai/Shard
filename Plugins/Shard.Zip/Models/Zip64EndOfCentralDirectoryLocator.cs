// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;

namespace Shard.Zip.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Zip64EndOfCentralDirectoryLocator {
	public uint Magic { get; init; }
	public uint DiskNumber { get; init; }
	public long EOCDOffset { get; init; }
	public uint TotalNumberOfDisks { get; init; }
}
