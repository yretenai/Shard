// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Shard.Zip.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Zip64EndOfCentralDirectoryLocator {
	public uint Magic { get; init; }
	public uint DiskNumber { get; init; }
	public long EOCDOffset { get; init; }
	public uint TotalNumberOfDisks { get; init; }
}
