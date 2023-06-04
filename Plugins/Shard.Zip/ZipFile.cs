// SPDX-License-Identifier: MPL-2.0

using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Shard.SDK.IO;
using Shard.Zip.Models;

namespace Shard.Zip;

public sealed class ZipFile : IDisposable {
	public ZipFile(Stream data) {
		Data = data;
		Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<ZipEndOfCentralDirectory>()];
		data.Seek(-buffer.Length, SeekOrigin.End);
		data.ReadExactly(buffer);
		EOCD = MemoryMarshal.Read<ZipEndOfCentralDirectory>(buffer);
		if (EOCD.Magic != 0x06054B50) {
			throw new InvalidDataException("Can't read end of central directory magic");
		}

		if (EOCD.IsZip64) {
			buffer = stackalloc byte[Unsafe.SizeOf<Zip64EndOfCentralDirectoryLocator>()];
			data.Seek(-(buffer.Length + Unsafe.SizeOf<ZipEndOfCentralDirectory>()), SeekOrigin.End);
			data.ReadExactly(buffer);
			EOCD64Locator = MemoryMarshal.Read<Zip64EndOfCentralDirectoryLocator>(buffer);
			if (EOCD64Locator.Magic != 0x07064B50) {
				throw new InvalidDataException("Can't read end of central directory locator magic");
			}

			buffer = stackalloc byte[Unsafe.SizeOf<Zip64EndOfCentralDirectory>()];
			data.Seek(EOCD64Locator.EOCDOffset, SeekOrigin.Begin);
			data.ReadExactly(buffer);
			EOCD64 = MemoryMarshal.Read<Zip64EndOfCentralDirectory>(buffer);
			if (EOCD64.Magic != 0x06064B50) {
				throw new InvalidDataException("Can't read end of central directory zip64 magic");
			}
		}

		var numberOfRecords = EOCD.IsZip64 ? EOCD64.NumberOfDirectoryRecords : EOCD.NumberOfDirectoryRecords;
		data.Seek(EOCD.IsZip64 ? EOCD64.DirectoryOffset : EOCD.DirectoryOffset, SeekOrigin.Begin);
		buffer = new byte[EOCD.IsZip64 ? EOCD64.DirectorySize : EOCD.DirectorySize];
		data.ReadExactly(buffer);
		var offset = 0;
		for (var i = 0L; i < numberOfRecords; ++i) {
			var record = MemoryMarshal.Read<ZipCentralDirectoryHeader>(buffer[offset..]);
			if (record.Magic != 0x02014B50) {
				throw new InvalidDataException("Can't read central directory header magic");
			}

			offset += Unsafe.SizeOf<ZipCentralDirectoryHeader>();
			var name = Encoding.UTF8.GetString(buffer[offset..(offset + record.FileNameLength)]);
			offset += record.FileNameLength;
			var extraData = buffer[offset..(offset + record.ExtraFieldLength)];
			offset += record.ExtraFieldLength;
			var extra = new List<KeyValuePair<ZipExtraHeader, object>>();
			for (var extraOffset = 0; extraOffset < extraData.Length;) {
				var extraField = MemoryMarshal.Read<ZipExtraHeader>(extraData[extraOffset..]);
				extraOffset += Unsafe.SizeOf<ZipExtraHeader>();
				var extraFieldData = extraData[extraOffset..(extraOffset + extraField.Length)];
				extraOffset += extraField.Length;
				switch (extraField.Id) {
					case ZipExtraHeaderId.Zip64ExtraHeader:
						var tmp = new byte[Unsafe.SizeOf<Zip64ExtendedInformation>()].AsSpan();
						extraFieldData.CopyTo(tmp);
						extra.Add(new KeyValuePair<ZipExtraHeader, object>(extraField, MemoryMarshal.Read<Zip64ExtendedInformation>(tmp)));
						break;
					default:
						extra.Add(new KeyValuePair<ZipExtraHeader, object>(extraField, extraFieldData.ToArray()));
						break;
				}
			}

			var comment = Encoding.UTF8.GetString(buffer[offset..(offset + record.CommentLength)]);
			offset += record.CommentLength;
			Entries.Add(new ZipEntry(name, record.UncompressedSize, extra, comment, record));
		}
	}

	public ZipEndOfCentralDirectory EOCD { get; }
	public Zip64EndOfCentralDirectoryLocator EOCD64Locator { get; set; }
	public Zip64EndOfCentralDirectory EOCD64 { get; set; }
	public List<ZipEntry> Entries { get; set; } = [];
	public Stream Data { get; set; }

	public void Dispose() {
		Data.Dispose();
	}

	public Stream Open(ZipEntry entry) {
		if ((entry.Header.Flags & 0b1) == 1) {
			throw new NotSupportedException("Encrypted Zip files are not supported");
		}

		var csize = (long) entry.Header.CompressedSize;
		var msize = (long) entry.Header.UncompressedSize;
		var offset = (long) entry.Header.Offset;
		var zipInfoComposite = entry.Extra.FirstOrDefault(x => x.Value is Zip64ExtendedInformation);
		if (zipInfoComposite.Value is Zip64ExtendedInformation zipInfo) {
			Span<long> value = stackalloc long[3] { zipInfo.UncompressedSize, zipInfo.CompressedSize, zipInfo.Offset };
			Span<int> remapId = stackalloc int[3] { -1, -1, -1 };
			var id = 0;
			if (entry.Header.UncompressedSize == uint.MaxValue) {
				remapId[0] = id++;
			}

			if (entry.Header.CompressedSize == uint.MaxValue) {
				remapId[1] = id++;
			}

			if (entry.Header.Offset == uint.MaxValue) {
				remapId[2] = id;
			}

			if (remapId[0] > -1) {
				msize = value[remapId[0]];
			}

			if (remapId[1] > -1) {
				csize = value[remapId[1]];
			}

			if (remapId[2] > -1) {
				offset = value[remapId[2]];
			}
		}

		Data.Seek(offset, SeekOrigin.Begin);
		Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<ZipFileHeader>()];
		Data.ReadExactly(buffer);
		var header = MemoryMarshal.Read<ZipFileHeader>(buffer);
		if (header.Magic != 0x04034B50) {
			throw new InvalidDataException("Can't read file header magic");
		}

		var shift = buffer.Length + header.ExtraFieldLength + header.FileNameLength;
		offset += shift;

		if (entry.Header.Compression == ZipCompression.Store) {
			return new OffsetStream(Data, offset, msize, true);
		}

		using var stream = new OffsetStream(Data, offset, csize, true);

		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch (entry.Header.Compression) {
			case ZipCompression.Deflate:
			case ZipCompression.Deflate64: {
				var ms = new MemoryStream();
				using var ds = new DeflateStream(stream, CompressionMode.Decompress);
				ds.CopyTo(ms);
				ms.Position = 0;
				return ms;
			}
			default:
				throw new NotSupportedException($"Compression {entry.Header.Compression} is not supported");
		}
	}
}
