// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK.Models;

// ReSharper disable UnusedMemberInSuper.Global
public interface IShardArchive {
	public IEnumerable<IShardRecord> Records { get; }
	public Memory<byte> GetRecord(IShardRecord record);
	public Memory<byte> GetRecord(string record, string version);
	public void AddRecord(string name, Memory<byte> data, ShardRecordMetadata metadata);
	public void ProcessFile(string name, Memory<byte> data, ShardRecordMetadata? metadata);
	public void ProcessFile(string name, Stream data, ShardRecordMetadata? metadata);
}
