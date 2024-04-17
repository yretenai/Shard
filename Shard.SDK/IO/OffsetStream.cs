// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK.IO;

public class OffsetStream : Stream {
	public OffsetStream(Stream stream, long? offset = null, long? length = null, bool leaveOpen = false) {
		BaseStream = stream;
		Start = offset ?? stream.Position;
		End = Start + (length ?? stream.Length - Start);
		BaseStream.Position = Start;
		LeaveOpen = leaveOpen;
	}

	private Stream BaseStream { get; }
	public long Start { get; }
	public long End { get; }
	public bool LeaveOpen { get; }
	public bool Disposed { get; private set; }

	public override bool CanRead => BaseStream.CanRead;

	public override bool CanSeek => BaseStream.CanSeek;

	public override bool CanWrite => false;

	public override long Length => End - Start;

	public override long Position {
		get => BaseStream.Position - Start;
		set => Seek(value, SeekOrigin.Begin);
	}

	public override void Close() {
		Disposed = true;

		if (LeaveOpen) {
			return;
		}

		BaseStream.Close();
	}

	public override async ValueTask DisposeAsync() {
		Disposed = true;

		if (!LeaveOpen) {
			await BaseStream.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	public override void Flush() {
		ObjectDisposedException.ThrowIf(Disposed, this);

		BaseStream.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count) {
		ObjectDisposedException.ThrowIf(Disposed, this);

		if (Position < 0) { // stream is reused oh no.
			Seek(0, SeekOrigin.Begin);
		}

		if (BaseStream.Position + count > End) {
			count = (int) (End - BaseStream.Position);
		}

		return count <= 0 ? 0 : BaseStream.Read(buffer, offset, count);
	}

	public override long Seek(long offset, SeekOrigin origin) {
		ObjectDisposedException.ThrowIf(Disposed, this);

		var absolutePosition = origin switch {
			                       SeekOrigin.Begin => Start + offset,
			                       SeekOrigin.Current => BaseStream.Position + offset,
			                       SeekOrigin.End => End + offset,
			                       _ => throw new IOException("Unknown seek origin"),
		                       };

		if (absolutePosition > End) {
			throw new IOException("Attempting to seek past the end of the stream");
		}

		if (absolutePosition < Start) {
			throw new IOException("Attempting to seek to a negative value");
		}

		BaseStream.Seek(absolutePosition, SeekOrigin.Begin);
		return Position;
	}

	public override void SetLength(long value) {
		throw new IOException("Read only");
	}

	public override void Write(byte[] buffer, int offset, int count) {
		throw new IOException("Read only");
	}
}
