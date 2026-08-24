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

## Verification

Before considering a task complete:

- Check the resulting Git diff.
- Ensure the project compiles.
- Run relevant tests when available.
- Add or update tests when the changed behavior can reasonably be tested.
- Report any verification that could not be performed.

## Documentation

Project documentation is stored under `docs/`.

When a task introduces or changes an important architectural decision or game rule, update the relevant documentation.
