# MR-bridge
Addon for the OpenRA Dedicated Server to provide:

1. Commands that can be entered into the dedicated server console window (i.e. stdin) for server administration
2. Remote administration connectivity, encrypted and password protected, which allows for remotely issuing the administration commands, and reading viewing console outputs

Plugs into OpenRA ServerTraits

To install:

Add the appropriate .cs files to OpenRA.Mods.Common/ServerTraits, compile OpenRA and add the traits to mod.yaml
