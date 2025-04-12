// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK;

public interface ShardCompressor {
	public int Decompress(ReadOnlyMemory<byte> input, Memory<byte> output);
	public Memory<byte> Compress(ReadOnlyMemory<byte> input, out IDisposable? disposable);
}
