# Shard.Plugins

These are two sample plugins for Shard to show two use cases.

## Shard.MinecraftAnvil

This is a non-destructive (as in it can preserve file integrity) plugin that handles Minecraft Anvil world files.
It uses the Shard "Encoder" API to tell the shard system to use this plugin to rebuild files.

## Shard.Zip

This is a destructive (as in it will not preserve file integrity) plugin that handles zip files.
It is a very simple zip decoder that simply unpacks the files directly into the shard system, because of this it
is not able to rebuild zip files.
