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

## Unity

- Do not modify generated Unity folders such as `Library`, `Temp`, `Logs`, or `obj`.
- Preserve Unity `.meta` files.
- Do not manually edit generated project/solution files unless required.
- Keep gameplay logic separate from presentation where practical.

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
