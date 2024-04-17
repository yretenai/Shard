// SPDX-License-Identifier: MPL-2.0

namespace Shard.Zip.Models;

public enum ZipCompression : ushort {
	Store = 0,
	Shrink = 1,
	Reduce1 = 2,
	Reduce2 = 3,
	Reduce3 = 4,
	Reduce4 = 5,
	Implode = 6,
	Tokenize = 7,
	Deflate = 8,
	Deflate64 = 9,
	PKImplode = 10,
	BZip2 = 12,
	LZMA = 14,
	Oodle = 15,
	CMPSC = 16,
	TERSE = 18,
	LZ77 = 19,
	DeprecatedZstd = 20,
	Zstd = 93,
	MP3 = 94,
	XZ = 95,
	JPEG = 96,
	WavPack = 97,
	PPMd = 98,
	EncryptionMarker = 99,
}
