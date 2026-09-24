# Unity VLM NPCs

This project is a Unity package for creating intelligent NPCs powered by a vision-language model workflow.

## What is included

- `VLMNpc` component with perception, memory, action validation, and a decision loop
- `SemanticObject` for world entities the AI can reason about
- `VLMDecision` + `IVLMProvider` interfaces for model-driven decisioning
- `VLMPromptBuilder` for structured agent prompts
- `ApartmentDemoBuilder` that procedurally creates a small apartment with NPC, player, table, chair, door, cup, apple, box, and trash can
- action system for `move_to`, `look_at`, `pick_up`, `place`, `speak`, `wait`, plus validation guards

## Package location

- `Packages/com.unityvlm.npcs`

## Quick usage

1. Open the project in Unity.
2. Add the package to the project via the local package manager if needed.
3. Create a GameObject and add `VLMNpc`.
4. Add a `NavMeshAgent` and a child `Camera` for vision.
5. Add `SemanticObject` components to interactable objects.
6. Call `ApartmentDemoBuilder.BuildApartment(Vector3.zero)` from an editor script or runtime bootstrap to create the apartment demo.

## MVP behavior

The NPC can:

- observe the scene with a camera and semantic object list
- reason about visible objects using a prompt + decision provider
- choose from a limited structured action set
- validate actions before execution
- maintain short-term memory and semantic facts

This is intentionally narrow and designed as a first step toward a more advanced agent-native game architecture.
