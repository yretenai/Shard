// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.Zip.Models;

public readonly record struct ZipExtraHeader {
	public ZipExtraHeaderId Id { get; init; }
	public ushort Length { get; init; }
}
