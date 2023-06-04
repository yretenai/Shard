// SPDX-License-Identifier: MPL-2.0

namespace Shard.Zip.Models;

public record struct ZipEntry(string Path, long Length, List<KeyValuePair<ZipExtraHeader, object>> Extra, string Comment, ZipCentralDirectoryHeader Header);
