# Fair play policy

FairPlay is a mod menu with a deliberately short feature list. This file explains what is in it,
what is not, and where each rule is actually enforced in code — because a policy that lives only in
a README is a preference, not a constraint.

## The three rules

### 1. Nothing runs outside a modded lobby

Two independent sources must agree — the Photon room's `gameMode` property, and Utilla's
`[ModdedGamemode]` edge. Either one missing, unreadable, or throwing means the gate is shut.

Enforced in [`src/Core/LobbyGuard.cs`](../src/Core/LobbyGuard.cs). `Module.SetEnabled` refuses to
turn anything on while the gate is closed, and `ModuleRegistry.Tick` force-disables everything the
moment it closes.

There is one configurable relaxation, `AllowOutsideRoom`, which permits features while you are in
**no lobby at all** — you, alone, connected to nobody. It cannot open the gate in a public lobby;
that code path does not exist.

### 2. Nothing touches another player

Every module changes only the local player's transform, rigidbody, colliders, materials, or
locally-spawned objects. No module sends an RPC, writes a room or player property, or instantiates
a networked object.

Enforced in [`src/Core/ModuleRegistry.cs`](../src/Core/ModuleRegistry.cs): a module declaring
`AffectsOtherPlayers` is rejected at registration with an error, not merely warned about. The flag
is declared rather than detected — the point is that every module file has to give an explicit,
reviewable answer instead of leaving the question unasked.

### 3. Everything is reversible

Every module implements `Revert()`, which must fully restore whatever it touched and must be safe
to call at any time, including when the module was never enabled. It is called on toggle-off, on
leaving a modded lobby, on scene load, on the panic button, and on plugin teardown.

If a module throws in `Tick`, the registry disables it and reverts it rather than letting it run
half-applied.

## What is deliberately not here

**Player highlighting / ESP / through-wall vision.** Technically self-only — it changes nothing on
anyone else's machine — and that is exactly why the "self-only" test alone is not a sufficient
policy. Seeing tagged players through terrain rewrites the game everyone in the lobby agreed to
play, in a way they cannot see happening. The Beacon module marks *a spot you chose*, not a person,
and that is the line.

**Anything that moves, tags, freezes, or renders another player.** Rule 2.

**Room manipulation** — forcing gamemodes, kicking, locking, renaming, editing room properties.
FairPlay reads room state and never writes it.

**Name or cosmetic spoofing.** Chroma tints your own skin material locally and puts the original
back; the networked colour you actually chose is never overwritten, so the lobby always sees the
real you.

**Anything that pretends to be vanilla.** No hiding the mod, no faking the gate, no "stealth" mode.
The panel is visible, says what state it is in, and says why.

## If you fork this

The gate is the interesting part, and it is one small file. Please keep it — or replace it with
something stricter. The rest of the codebase assumes a single authority answering "is anything
allowed to run right now", and modules stay simple precisely because they never have to ask that
question themselves.
