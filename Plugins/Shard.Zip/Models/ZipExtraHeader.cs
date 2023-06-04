// SPDX-License-Identifier: MPL-2.0

namespace Shard.Zip.Models;

public readonly record struct ZipExtraHeader {
	public ZipExtraHeaderId Id { get; init; }
	public ushort Length { get; init; }
}
