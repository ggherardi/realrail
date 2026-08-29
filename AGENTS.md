# Agent Instructions

## Project

This is a Unity 6.5 3D game project written in C#.

## Working Principles

- Analyze the relevant existing code and project structure before making changes.
- Reuse existing patterns and abstractions where appropriate.
- Keep changes focused on the requested task.
- Do not perform unrelated refactoring unless it is necessary for the task.
- If a requirement is ambiguous and the choice could have a significant architectural or gameplay impact, ask for clarification before implementing it.
- Prefer simple solutions over speculative abstractions.
- Do not add third-party dependencies unless they are clearly necessary.

## Milestone Integration Workflow

Milestone Leads normally work in an isolated Codex worktree, may delegate
bounded tasks to native subagents, integrate their work, run required automated
validation, review the combined diff, and create one local milestone commit
when validation succeeds.

After creating a validated milestone commit, integrate it into the user's
normal RealRail working copy at `/Users/ggherardi/realrail` when the milestone
explicitly authorizes an Epic target branch. Before cherry-picking, verify that
the normal working copy is on exactly that branch, its working tree is clean,
and the commit is not already present. If any precondition fails, stop and
report the exact state; do not repair it by stashing, resetting, switching
branches, restoring/discarding files, cleaning, or using destructive Git
operations.

When all preconditions pass, cherry-pick the validated commit, then verify the
target branch is unchanged, the target working tree is clean, and report its
resulting HEAD. If the cherry-pick conflicts, stop and report the conflicting
files and relevant context; do not resolve it unless explicitly authorized.

After a successful cherry-pick, stop for manual acceptance whenever the
milestone requires human judgment, such as Play Mode gameplay, balance,
visual readability, animation quality, camera/framing, or UI/UX appearance.
Automated validation does not replace human acceptance.

Unless explicitly authorized for a particular task, milestone Leads may modify
their isolated worktree, use native subagents, run project tooling and
automated tests, create local commits, and cherry-pick validated commits into
an explicitly authorized local Epic branch after the checks above. They must
not push, merge into or modify `main`, create a PR, delete branches/worktrees,
force-push, or reset, clean, discard, or otherwise destructively alter user
work.

## Unity

- Do not modify generated Unity folders such as `Library`, `Temp`, `Logs`, or `obj`.
- Preserve Unity `.meta` files.
- Do not manually edit generated project/solution files unless required.
- Keep gameplay logic separate from presentation where practical.

### Serialized object references

- Do not manually fabricate Unity YAML object references or fileIDs, especially
  for prefabs and prefab variants.
- Use Unity serialization, import, or editor tooling to assign object references
  whenever possible, and validate that each reference resolves to the field type
  Unity expects.
- After scene or prefab serialization changes, inspect Unity import/runtime logs
  for type-mismatch or deserialization errors. A textually plausible YAML
  reference is not sufficient validation.

### Unity `.meta` files

- Do not fabricate minimal `.meta` files; let Unity generate and fully serialize
  metadata for new assets and folders whenever possible.
- Preserve valid GUIDs. Never regenerate one merely to normalize metadata.
- Treat importer sections (`DefaultImporter`, `ModelImporter`,
  `TextureImporter`, animation settings, and similar) as semantic changes.
  Inspect the actual Git diff before calling metadata formatting-only or
  restoring it; do not automatically discard Unity-generated importer data.
- When generating external assets (`.blend`, `.fbx`, textures, audio, etc.),
  verify their final Unity-imported `.meta` state and version-control it with
  the asset.

## Verification

Before considering a task complete:

- Check the resulting Git diff.
- Ensure the project compiles.
- Run relevant tests when available.
- Add or update tests when the changed behavior can reasonably be tested.
- Report any verification that could not be performed.

For authored scene or layout changes, tests should validate the complete
relevant contract where practical. Coordinate and serialization assertions do
not replace Unity Play Mode visual/runtime acceptance for concerns such as
geometry continuity, overlap/occlusion, camera readability, gameplay feel,
enemy silhouette/readability, or animation appearance.

### Unity batch-mode licensing

An initial LicensingClient protocol or handshake error in a Unity batch log can
be recoverable. Unity may subsequently launch its bundled version-specific
LicensingClient; evaluate the final licensing state, where `Licensing is
initialized` confirms successful initialization. Determine test success from
process completion, creation of `TestResults.xml`, and its NUnit results. If no
results file is created, inspect the end of the Unity log and report the final
shutdown cause rather than an earlier licensing warning.

## Documentation

Project documentation is stored under `docs/`.

When a task introduces or changes an important architectural decision or game rule, update the relevant documentation.

## Art Direction

Before making major visual decisions for assets, environment/art, UI/HUD, VFX,
or camera/presentation, Leads must read `docs/art/ART_DIRECTION.md` and inspect
its associated visual reference.
