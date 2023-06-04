// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK.Models;

// ReSharper disable UnusedMemberInSuper.Global
public interface IShardArchive {
	public IEnumerable<IShardRecord> Records { get; }

	public Span<byte> GetRecord(IShardRecord record);
	public Span<byte> GetRecord(string record, string version);
	public void AddRecord(string name, Span<byte> data, string? encoder = null, ShardRecordFlags flags = ShardRecordFlags.None);
	public void ProcessFile(string name, Span<byte> data);
	public void ProcessFile(string name, Stream data);
}
