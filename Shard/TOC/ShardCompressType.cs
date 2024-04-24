// SPDX-License-Identifier: MPL-2.0

namespace Shard.TOC;

public enum ShardCompressType {
	None,
	ZStd,
	LZO,
	LZ4,
	Snappy,
	ZLib,
	Custom = -1,
}
