# Security policy

## Reporting a vulnerability

Please do not open a public issue for a security problem. Use GitHub's
**Report a vulnerability** button on the **Security** tab of this repository, which opens a
private draft advisory visible only to the maintainer.

Include:

- what the problem lets someone do (crash the game, read a file, run code, crash a lobby),
- the mod version and the game build (`BepInEx/LogOutput.log` shows both),
- the smallest set of steps you can find that reproduces it.

## What this mod can and cannot do

It reads the level editor's already-loaded parts palette and shows a filtered grid. It opens
no sockets, downloads nothing, writes no files, loads no assemblies and has no telemetry. The
full threat model, including what *is* attacker-controlled (nothing remote), is in
[docs/security.md](../docs/security.md).

## Supported versions

The latest release is supported. Fixes are released as a new version on mod.io; there are no
backports to older versions.
