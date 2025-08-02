// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.SDK;

public interface ShardCompressor {
	int Decompress(ReadOnlyMemory<byte> input, Memory<byte> output);
	Memory<byte> Compress(ReadOnlyMemory<byte> input, out IDisposable? disposable);
}
