// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.Zip.Models;

public record struct ZipEntry(string Path, long Length, List<KeyValuePair<ZipExtraHeader, object>> Extra, string Comment, ZipCentralDirectoryHeader Header);
