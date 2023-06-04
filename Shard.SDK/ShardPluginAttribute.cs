// SPDX-License-Identifier: MPL-2.0

namespace Shard.SDK;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ShardPluginAttribute : Attribute {
	public ShardPluginAttribute(string id, string name, string author, string version, string description) {
		Id = id;
		Name = name;
		Author = author;
		Version = version;
		Description = description;
	}

	public string Id { get; set; }
	public string Name { get; set; }
	public string Author { get; set; }
	public string Version { get; set; }
	public string Description { get; set; }
	public int Priority { get; set; }
}
