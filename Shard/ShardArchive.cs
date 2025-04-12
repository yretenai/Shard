// SPDX-License-Identifier: MPL-2.0

using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Blake3;
using DragonLib;
using IronCompress;
using Serilog;
using Shard.SDK;
using Shard.SDK.Models;
using Shard.TOC;

namespace Shard;

public record ShardOptions {
	public static ShardOptions Default { get; } = new() {
		BlockSize = ShardArchive.DEFAULT_BLOCK_SIZE,
		ShardSize = ShardArchive.DEFAULT_SHARD_SIZE,
		CompressType = ShardArchive.DEFAULT_COMPRESSION,
		Alignment = ShardArchive.DEFAULT_ALIGNMENT,
		HeaderAlignment = ShardArchive.DEFAULT_HEADER_ALIGNMENT,
	};

	public int BlockSize { get; init; }
	public long ShardSize { get; init; }
	public ShardCompressType CompressType { get; init; }
	public bool IsReadOnly { get; init; }
	public ushort Alignment { get; init; }
	public ushort HeaderAlignment { get; init; }
	public ShardCompressor? CustomCompressor { get; init; }
}

public sealed partial class ShardArchive : IShardArchive, IDisposable {
	public const ushort DEFAULT_ALIGNMENT = 16;
	public const ushort DEFAULT_HEADER_ALIGNMENT = 16;
	public const long DEFAULT_SHARD_SIZE = Extensions.OneGiB * 16;
	public const int DEFAULT_BLOCK_SIZE = (int) Extensions.OneMiB;
	public const ShardCompressType DEFAULT_COMPRESSION = ShardCompressType.ZStd;
	private const long MAGIC = 0x434F544452414853;
	private readonly Iron Iron = new();

	public ShardArchive(string name, string path, ShardOptions options) {
		Name = name;
		ShardPath = path;
		Directory.CreateDirectory(ShardPath);

		Log.Information("Loading shard archive {Name} at {Path}", Name, ShardPath);

		CompressType = options.CompressType;
		CustomCompressor = options.CustomCompressor;
		IsReadOnly = options.IsReadOnly;

		var tocPath = Path.Combine(ShardPath, $"{Name}.shardtoc");
		if (!File.Exists(tocPath)) {
			Header = new ShardTOCHeader {
				Magic = MAGIC, // SHARDTOC
				Version = ShardTOCVersion.Latest,
				ShardSize = options.ShardSize,
				BlockSize = options.BlockSize,
				Alignment = options.Alignment,
				HeaderAlignment = options.HeaderAlignment,
			};
			Records = [];
			Versions = [];
			Names = [];
			Blocks = [];
			BlockIndices = [];
			BlockHashMap = [];
			RecordHashMap = [];
			BlockStreams = [];
			Flush();
			DumpTOCMetrics();
			return;
		}

		using var toc = new FileStream(tocPath, FileMode.Open, IsReadOnly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read);
		Span<ShardTOCHeader> header = stackalloc ShardTOCHeader[1];
		toc.ReadExactly(MemoryMarshal.AsBytes(header));
		Header = header[0];

		if (Header.Magic != MAGIC) {
			// Header.Magic != SHARDTOC
			throw new InvalidDataException("Invalid magic number.");
		}

		if (Header.Version >= ShardTOCVersion.FixAlignment) {
			toc.Align(Header.HeaderAlignment);
		}

		var p = toc.Position;
		var tocSize = (int) (toc.Length - toc.Position);
		using (var fullBuffer = MemoryPool<byte>.Shared.Rent(tocSize)) {
			toc.ReadExactly(fullBuffer.Memory.Span[..tocSize]);
			var hash = Hasher.Hash(fullBuffer.Memory.Span[..tocSize]);
			if (Header.Checksum != hash) {
				throw new InvalidDataException("Invalid header checksum.");
			}
		}

		toc.Position = p;

		Records = new List<ShardTOCRecord>(Header.RecordCount);
		Versions = new List<string>(Header.VersionCount);
		Names = new List<string>(Header.NameCount);
		Blocks = new List<ShardTOCBlock>(Header.BlockCount);
		BlockIndices = new List<int>(Header.BlockIndiceCount);
		RecordHashMap = new Dictionary<Hash, ShardTOCHashMap>(Header.HashMapCount);
		BlockStreams = new List<Stream>(Header.BlockCount);
		BlockHashMap = new Dictionary<Hash, int>();

		Log.Information("Loading TOC");

		switch (Header.Version) {
			case ShardTOCVersion.Initial:
				LoadTOCV1(toc);
				break;
			case ShardTOCVersion.AddEncoder:
			case ShardTOCVersion.FixAlignment:
				LoadTOCV2(toc);
				break;
			case ShardTOCVersion.CompressWholeTOC:
				LoadTOCV3(toc);
				break;
			case ShardTOCVersion.StoreAttributes:
			case ShardTOCVersion.CustomCompressor:
				LoadTOCV4(toc);
				break;
			default:
				throw new InvalidDataException("Invalid version number.");
		}

		if (Header.ShardCount > 0) {
			for (var i = 0; i < Header.ShardCount; ++i) {
				var blockPath = Path.Combine(ShardPath, $"{Name}_{i}.shard");
				if (!File.Exists(blockPath)) {
					Log.Warning($"BlockStream {i} not found.");
					BlockStreams.Add(Stream.Null);
					continue;
				}

				if (!IsReadOnly && i == Header.ShardCount - 1) {
					BlockStreams.Add(new FileStream(blockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read));
				} else {
					BlockStreams.Add(new FileStream(blockPath, FileMode.Open, FileAccess.Read, FileShare.Read));
				}
			}

			if (!IsReadOnly && BlockStreams.Count > 0 && BlockStreams[^1].Length < Header.ShardSize && BlockStreams[^1].Length > 0) {
				CurrentBlockStream = BlockStreams[^1];
			}
		}

		Header = Header with {
			Version = ShardTOCVersion.Latest,
		};

		DumpTOCMetrics();
	}

	public ShardTOCHeader Header { get; private set; }
	public string ShardPath { get; }
	public string Name { get; }
	public ShardCompressType CompressType { get; }
	public ShardCompressor? CustomCompressor { get; }
	public bool IsReadOnly { get; }
	public List<ShardTOCRecord> Records { get; }
	public List<string> Versions { get; }
	public List<string> Names { get; }
	public List<ShardTOCBlock> Blocks { get; }
	public List<int> BlockIndices { get; }
	public Dictionary<Hash, ShardTOCHashMap> RecordHashMap { get; }
	public Dictionary<Hash, int> BlockHashMap { get; set; }
	public List<Stream> BlockStreams { get; }
	public Stream? CurrentBlockStream { get; private set; }
	public string? CurrentVersion { get; private set; }
	private int CurrentVersionIndex { get; set; } = -1;

	public void Dispose() {
		CurrentBlockStream?.Dispose();
		foreach (var stream in BlockStreams) {
			stream.Dispose();
		}
	}

	IEnumerable<IShardRecord> IShardArchive.Records => Records.Select(RecordToVirtual);

	public Span<byte> GetRecord(IShardRecord record) {
		if (record is not ShardRecord shardRecord) {
			throw new ArgumentException("Record is not a ShardRecord.", nameof(record));
		}

		return GetRecord(shardRecord.Record);
	}

	public Span<byte> GetRecord(string name, string version) {
		var nameIndex = Names.IndexOf(name);
		var versionIndex = Versions.IndexOf(version);
		if (nameIndex == -1 || versionIndex == -1) {
			return Span<byte>.Empty;
		}

		var record = Records.SingleOrDefault(x => x.NameIndex == nameIndex && x.VersionIndex == versionIndex);
		return record.Size == 0 ? Span<byte>.Empty : GetRecord(record);
	}

	public void AddRecord(string name, Span<byte> data, ShardRecordMetadata metadata) {
		if (IsReadOnly) {
			return;
		}

		if (CurrentVersionIndex == -1) {
			throw new InvalidOperationException("No version is selected.");
		}

		Log.Debug("Adding record {Name} to {ShardName}...", name, Name);

		var hash = Hasher.Hash(data);
		if (RecordHashMap.TryGetValue(hash, out var map)) {
			Records.Add(new ShardTOCRecord {
				NameIndex = AddName(name),
				VersionIndex = CurrentVersionIndex,
				Hash = hash,
				BlockIndex = map.BlockIndex,
				BlockCount = map.BlockCount,
				EncoderIndex = metadata.Encoder == null ? -1 : AddName(metadata.Encoder),
				Flags = metadata.Flags,
				Size = data.Length,
				Timestamp = metadata.Timestamp,
				Permissions = metadata.Permissions == 0 ? 0x1FF : metadata.Permissions,
				Attributes = metadata.Attributes,
			});
			return;
		}

		var blockCount = data.Length.Align(Header.BlockSize) / Header.BlockSize;
		var blockIndex = BlockIndices.Count;
		RecordHashMap[hash] = new ShardTOCHashMap {
			BlockCount = blockCount,
			BlockIndex = blockIndex,
		};

		for (var i = 0; i < blockCount; ++i) {
			var slice = data[(i * Header.BlockSize)..];
			if (slice.Length > Header.BlockSize) {
				slice = slice[..Header.BlockSize];
			}

			var blockHash = Hasher.Hash(slice);
			if (BlockHashMap.TryGetValue(blockHash, out var block)) {
				BlockIndices.Add(block);
				continue;
			}

			if (CurrentBlockStream == null) {
				var blockPath = Path.Combine(ShardPath, $"{Name}_{BlockStreams.Count}.shard");
				CurrentBlockStream = new FileStream(blockPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
				BlockStreams.Add(CurrentBlockStream);
			}

			var type = ShardCompressType.None;
			IDisposable? disposable = null;
			try {
				var writeData = CompressData(slice, CompressType, out type, out disposable);

				var compressedHash = Hasher.Hash(writeData);

				CurrentBlockStream.Position = CurrentBlockStream.Length;

				var blockEntry = new ShardTOCBlock {
					ShardIndex = BlockStreams.Count - 1,
					Offset = CurrentBlockStream.Position,
					Footer = new ShardBlockHeader {
						HeaderSize = (ushort) (Unsafe.SizeOf<ShardBlockHeader>() - 2),
						Version = ShardBlockVersion.Latest,
						Size = slice.Length,
						CompressedSize = writeData.Length,
						CompressionType = type,
						BlockHash = blockHash,
						CompressedBlockHash = compressedHash,
					},
				};

				var footer = blockEntry.Footer;
				var footerSpan = new Span<ShardBlockHeader>(ref footer);
				CurrentBlockStream.Write(MemoryMarshal.AsBytes(footerSpan));
				CurrentBlockStream.Write(writeData);
				BlockIndices.Add(Blocks.Count);
				BlockHashMap.Add(blockHash, Blocks.Count);
				Blocks.Add(blockEntry);

				CurrentBlockStream.Align(Header.Alignment);

				if (CurrentBlockStream.Length > Header.ShardSize) {
					ResetBlockStream();
				}
			} finally {
				disposable?.Dispose();
			}
		}

		Records.Add(new ShardTOCRecord {
			NameIndex = AddName(name),
			VersionIndex = CurrentVersionIndex,
			Hash = hash,
			BlockIndex = blockIndex,
			BlockCount = blockCount,
			EncoderIndex = metadata.Encoder == null ? -1 : AddName(metadata.Encoder),
			Flags = metadata.Flags,
			Size = data.Length,
			Timestamp = metadata.Timestamp,
			Permissions = metadata.Permissions == 0 ? 0x1FF : metadata.Permissions,
			Attributes = metadata.Attributes,
		});
	}

	public unsafe void ProcessFile(string name, Span<byte> data, ShardRecordMetadata? metadata = null) {
		fixed (byte* pin = &data.GetPinnableReference()) {
			using var stream = new UnmanagedMemoryStream(pin, data.Length);
			ProcessFile(name, stream, metadata);
		}
	}

	public void ProcessFile(string name, Stream data, ShardRecordMetadata? metadata = null) => ShardPluginEngine.Decode(name, data, this, metadata.GetValueOrDefault());

	private void DumpTOCMetrics() {
		Log.Information("--------------------------");
		Log.Information("TOC Metrics:");
		Log.Information("--------------------------");
		Log.Information("  TOC Version: {Version}", Header.Version);
		Log.Information("  Checksum: {Checksum}", Header.Checksum);
		Log.Information("  Shard Size: {ShardSize}", Header.ShardSize.GetHumanReadableBytes());
		Log.Information("  Block Size: {BlockSize}", Header.BlockSize.GetHumanReadableBytes());
		Log.Information("  Records: {Records}", Header.RecordCount);
		Log.Information("  Versions: [{Versions}]", string.Join(", ", Versions));
		Log.Information("  Names: {Names}", Header.NameCount);
		Log.Information("  Blocks: {Blocks}", Header.BlockCount);
		Log.Information("  Block Indices: {BlockIndices}", Header.BlockIndiceCount);
		Log.Information("  Hashes: {HashMaps}", Header.HashMapCount);
		Log.Information("  Shards: {Shards}", Header.ShardCount);
		Log.Information("--------------------------");
		Log.Information("  Records Size: {RecordsSize}", (Header.RecordCount * Unsafe.SizeOf<ShardTOCRecord>()).GetHumanReadableBytes());
		Log.Information("  Versions Size: {VersionsSize}", Versions.Sum(x => Encoding.UTF8.GetByteCount(x)).GetHumanReadableBytes());
		Log.Information("  Names Size: {NamesSize}", Names.Sum(x => Encoding.UTF8.GetByteCount(x)).GetHumanReadableBytes());
		Log.Information("  Blocks Size: {BlocksSize}", (Header.BlockCount * Unsafe.SizeOf<ShardTOCBlock>()).GetHumanReadableBytes());
		Log.Information("  Block Indices Size: {BlockIndicesSize}", (Header.BlockIndiceCount * sizeof(int)).GetHumanReadableBytes());
		Log.Information("  Hashes Size: {HashMapsSize}", (Header.HashMapCount * Unsafe.SizeOf<KeyValuePair<Hash, ShardTOCHashMap>>()).GetHumanReadableBytes());
		Log.Information("  Shard Sizes: [{ShardSizes}]", string.Join(", ", BlockStreams.Select(x => x.Length.GetHumanReadableBytes())));
		Log.Information("  Total File Size: {TotalSize}", Records.Sum(x => (long) x.Size).GetHumanReadableBytes());
		Log.Information("  Total Uncompressed Size: {TotalSize}", Blocks.Sum(x => (long) x.Footer.Size).GetHumanReadableBytes());
		Log.Information("  Total Compressed Size: {TotalSize}", Blocks.Sum(x => (long) x.Footer.CompressedSize).GetHumanReadableBytes());
		ShardPluginEngine.Dump();
	}

	public void Flush() {
		if (IsReadOnly) {
			return;
		}

		var header = Header;
		header.Version = ShardTOCVersion.Latest;
		header.RecordCount = Records.Count;
		header.VersionCount = Versions.Count;
		header.NameCount = Names.Count;
		header.BlockCount = Blocks.Count;
		header.BlockIndiceCount = BlockIndices.Count;
		header.HashMapCount = RecordHashMap.Count;
		header.ShardCount = BlockStreams.Count;
		Header = header;

		using var buffer = new MemoryStream();

		if (Records.Count > 0) {
			var records = Records.ToArray();
			buffer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref records[0], Records.Count)));
			buffer.Align(Header.HeaderAlignment);
		}

		if (Versions.Count > 0) {
			var versions = Versions.Select(x => Encoding.UTF8.GetByteCount(x)).ToArray();
			buffer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref versions[0], Versions.Count)));
			buffer.Align(Header.HeaderAlignment);
			foreach (var version in Versions) {
				buffer.Write(Encoding.UTF8.GetBytes(version));
			}

			buffer.Align(Header.HeaderAlignment);
		}

		if (Names.Count > 0) {
			var names = Names.Select(x => Encoding.UTF8.GetByteCount(x)).ToArray();
			buffer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref names[0], Names.Count)));
			buffer.Align(Header.HeaderAlignment);
			foreach (var name in Names) {
				buffer.Write(Encoding.UTF8.GetBytes(name));
			}

			buffer.Align(Header.HeaderAlignment);
		}

		if (Blocks.Count > 0) {
			var blocks = Blocks.ToArray();
			buffer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref blocks[0], Blocks.Count)));
			buffer.Align(Header.HeaderAlignment);
		}

		if (BlockIndices.Count > 0) {
			var blockIndices = BlockIndices.ToArray();
			buffer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref blockIndices[0], BlockIndices.Count)));
			buffer.Align(Header.HeaderAlignment);
		}

		if (RecordHashMap.Count > 0) {
			var hashMaps = RecordHashMap.ToArray();
			buffer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref hashMaps[0], RecordHashMap.Count)));
			buffer.Align(Header.HeaderAlignment);
		}

		var unsafeBuffer = buffer.GetBuffer().AsSpan(0, (int) buffer.Length);
		var writeData = CompressData(unsafeBuffer, CompressType, out var type, out var disposable);

		Span<ShardTOCCompressInfo> compressInfo = stackalloc ShardTOCCompressInfo[1];
		compressInfo[0] = new ShardTOCCompressInfo {
			CompressType = type,
			CompressSize = writeData.Length,
			Size = unsafeBuffer.Length,
		};

		using var hasher = Hasher.New();
		hasher.Update(MemoryMarshal.AsBytes(compressInfo));
		hasher.Update(writeData);
		header.Checksum = hasher.Finalize();

		CurrentBlockStream?.Flush();

		using var stream = new FileStream(Path.Combine(ShardPath, $"{Name}.shardtoc"), FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
		stream.SetLength(0);
		stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
		stream.Align(Header.HeaderAlignment);
		stream.Write(MemoryMarshal.AsBytes(compressInfo));
		stream.Write(writeData);
		stream.Flush();

		disposable?.Dispose();

		Header = header;
	}

	public IEnumerable<ShardRecord> GetRecordsForVersion(string version) {
		var versionIndex = Versions.IndexOf(version);
		if (versionIndex == -1) {
			yield break;
		}

		foreach (var record in Records.Where(x => x.VersionIndex == versionIndex && !x.Flags.HasFlag(ShardRecordFlags.Hidden))) {
			yield return (ShardRecord) RecordToVirtual(record);
		}
	}

	private unsafe Span<byte> GetRecord(ShardTOCRecord record) {
		var data = new byte[record.Size].AsSpan();
		var offset = 0;
		if (data.Length > 0 && !record.Flags.HasFlag(ShardRecordFlags.Meta)) {
			var block = BlockIndices.Skip(record.BlockIndex).Take(record.BlockCount).Select(x => Blocks[x]).ToArray();
			using var inChunk = MemoryPool<byte>.Shared.Rent(Header.BlockSize);

			Span<ushort> header = stackalloc ushort[1];

			foreach (var blockEntry in block) {
				var stream = BlockStreams[blockEntry.ShardIndex];
				if (stream.Length < blockEntry.Offset) {
					Log.Error("Corrupt Shard (ID: {ID})", blockEntry.ShardIndex);
					return Span<byte>.Empty;
				}

				stream.Seek(blockEntry.Offset, SeekOrigin.Begin);
				stream.ReadExactly(MemoryMarshal.AsBytes(header));
				stream.Position += header[0]; // skip header.
				stream.ReadExactly(inChunk.Memory.Span[..blockEntry.Footer.CompressedSize]);

				var slice = inChunk.Memory.Span[..blockEntry.Footer.CompressedSize];
				DecompressData(blockEntry.Footer.CompressionType, slice, blockEntry.Footer.Size, out var disposable).CopyTo(data[offset..]);
				disposable?.Dispose();
				offset += blockEntry.Footer.Size;
			}
		}

		return record.EncoderIndex < 0 ? data : ShardPluginEngine.Encode(Names[record.EncoderIndex], RecordToVirtual(record), data, this);
	}

	private IShardRecord RecordToVirtual(ShardTOCRecord record) =>
		new ShardRecord {
			Name = Names[record.NameIndex],
			Version = Versions[record.VersionIndex],
			Encoder = record.EncoderIndex == -1 ? null : Names[record.EncoderIndex],
			Hash = record.Hash,
			BlockHashes = BlockIndices.Skip(record.BlockIndex).Take(record.BlockCount).Select(x => Blocks[x].Footer.BlockHash),
			Flags = record.Flags,
			Timestamp = record.Timestamp,
			Permissions = record.Permissions == 0 ? 0x1FF : record.Permissions,
			Attributes = record.Attributes,
			Record = record,
		};

	private unsafe Span<byte> CompressData(Span<byte> slice, ShardCompressType providedType, out ShardCompressType type, out IDisposable? disposable) {
		type = ShardCompressType.None;
		disposable = null;

		switch (providedType) {
			case ShardCompressType.Custom: {
				if (CustomCompressor == null) {
					throw new InvalidOperationException("Tried using a custom compressor when none exists");
				}

				var compressed = CustomCompressor.Compress(slice, out disposable);
				if (compressed.Length < slice.Length) {
					type = CompressType;
					return compressed;
				}

				disposable?.Dispose();
				disposable = null;
				break;
			}
			case ShardCompressType.ZStd:
			case ShardCompressType.LZO:
			case ShardCompressType.LZ4:
			case ShardCompressType.Snappy: {
				// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
				var compressed = Iron.Compress(CompressType switch {
					ShardCompressType.ZStd => Codec.Zstd,
					ShardCompressType.LZO => Codec.LZO,
					ShardCompressType.LZ4 => Codec.LZ4,
					ShardCompressType.Snappy => Codec.Snappy,
					_ => throw new UnreachableException(),
				}, slice);
				if (compressed.Length < slice.Length) {
					disposable = compressed;
					type = CompressType;
					return compressed.AsSpan();
				}

				compressed.Dispose();
				break;
			}
			case ShardCompressType.ZLib: {
				fixed (byte* ptr = &slice.GetPinnableReference()) {
					using var unmanaged = new UnmanagedMemoryStream(ptr, slice.Length);
					using var stream = new ZLibStream(unmanaged, CompressionLevel.SmallestSize, false);
					var output = new MemoryStream();
					stream.CopyTo(output);
					stream.Flush();
					output.Flush();
					if (output.Length < slice.Length) {
						disposable = output;
						type = CompressType;
						return output.GetBuffer().AsSpan(0, (int) output.Length);
					}

					output.Dispose();
					break;
				}
			}
			case ShardCompressType.None:
				return slice;
			default: throw new ArgumentOutOfRangeException(nameof(providedType));
		}

		return slice;
	}

	private unsafe Span<byte> DecompressData(ShardCompressType type, Span<byte> slice, int size, out IDisposable? disposable) {
		disposable = null;

		switch (type) {
			case ShardCompressType.Custom: {
				if (CustomCompressor == null) {
					throw new InvalidOperationException("Tried using a custom compressor when none exists");
				}

				var pool = MemoryPool<byte>.Shared.Rent(size);
				disposable = pool;
				var sz = CustomCompressor.Decompress(slice, pool.Memory[..size].Span);
				return pool.Memory[..sz].Span;
			}
			case ShardCompressType.ZStd:
			case ShardCompressType.LZO:
			case ShardCompressType.LZ4:
			case ShardCompressType.Snappy: {
				// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
				var compressed = Iron.Decompress(type switch {
					ShardCompressType.ZStd => Codec.Zstd,
					ShardCompressType.LZO => Codec.LZO,
					ShardCompressType.LZ4 => Codec.LZ4,
					ShardCompressType.Snappy => Codec.Snappy,
					_ => throw new UnreachableException(),
				}, slice);
				disposable = compressed;
				return compressed.AsSpan();
			}
			case ShardCompressType.ZLib: {
				fixed (byte* ptr = &slice.GetPinnableReference()) {
					using var unmanaged = new UnmanagedMemoryStream(ptr, slice.Length);
					using var zstream = new ZLibStream(unmanaged, CompressionMode.Decompress, false);
					var output = new MemoryStream();
					disposable = output;
					zstream.CopyTo(output);
					zstream.Flush();
					output.Flush();
					return output.GetBuffer().AsSpan(0, (int) output.Length);
				}
			}
			case ShardCompressType.None:
				return slice;
			default: throw new ArgumentOutOfRangeException(nameof(type));
		}
	}

	private int AddName(string name) {
		if (Names.Contains(name)) {
			return Names.IndexOf(name);
		}

		Names.Add(name);
		return Names.Count - 1;
	}

	public void SetVersion(string version) {
		if (CurrentVersion == version) {
			return;
		}

		CurrentVersion = version;
		Log.Information("Setting version to {Version}", CurrentVersion);

		CurrentVersionIndex = Versions.IndexOf(version);
		if (CurrentVersionIndex == -1) {
			if (IsReadOnly) {
				throw new VersionNotFoundException();
			}

			CurrentVersionIndex = Versions.Count;
			Versions.Add(version);

			Flush();
		}
	}

	private void ResetBlockStream() {
		if (CurrentBlockStream is null) {
			return;
		}

		CurrentBlockStream.Flush();
		CurrentBlockStream = null;
	}
}
