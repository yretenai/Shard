// SPDX-FileCopyrightText: 2025 Np-93/237 (Yretenai/Legiayayana/Chronovore)
//
// SPDX-License-Identifier: EUPL-1.2

namespace Shard.SDK.Models;

// ReSharper disable UnusedMemberInSuper.Global
public interface IShardArchive {
	IEnumerable<IShardRecord> Records { get; }
	Memory<byte> GetRecord(IShardRecord record);
	Memory<byte> GetRecord(string record, string version);
	void AddRecord(string name, Memory<byte> data, ShardRecordMetadata metadata);
	void ProcessFile(string name, Memory<byte> data, ShardRecordMetadata? metadata);
	void ProcessFile(string name, Stream data, ShardRecordMetadata? metadata);
}
