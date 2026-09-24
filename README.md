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

## Using Claude as the VLM

1. Get an Anthropic API key from [console.anthropic.com](https://console.anthropic.com)
2. On your `VLMNpc` component, paste your API key into the `apiKey` field
3. Choose your reasoning mode:
   - **Planner mode (Dreamer)** — `usePlanner = true` (default)
     - Claude generates 2-5 step action plans
     - Fewer API calls
     - More coherent multi-step behaviors
   - **Reactive mode** — `usePlanner = false`
     - Claude decides one action per decision cycle
     - More responsive to world changes
     - More API calls

4. Play the scene — the NPC will use Claude 3.5 Sonnet to reason about the world

The NPC will:
- Capture its camera view
- Send the image + structured prompt to Claude
- Receive a plan (or single action) and execute it
- Re-plan as needed

Falls back to heuristic behavior if no API key is set.

## MVP behavior

The NPC can:

- observe the scene with a camera and semantic object list
- reason about visible objects using a prompt + decision provider
- choose from a limited structured action set
- validate actions before execution
- maintain short-term memory and semantic facts

This is intentionally narrow and designed as a first step toward a more advanced agent-native game architecture.
