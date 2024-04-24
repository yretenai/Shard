// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK;

public interface ShardCompressor {
	public int Decompress(ReadOnlySpan<byte> input, Span<byte> output);
	public Span<byte> Compress(ReadOnlySpan<byte> input, out IDisposable? disposable);
}
