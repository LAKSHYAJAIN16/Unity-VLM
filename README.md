# Unity VLM NPCs

A Unity package for building NPCs driven by Vision-Language Models.

This repository currently contains the initial package scaffolding for a VLM-controlled NPC system, including:

- `VLMNpc` component
- semantic world object support
- structured action abstraction
- memory and perception models
- sample/demo setup pipeline

## Package

- `Packages/com.unityvlm.npcs`

## Getting started

1. Open this project in Unity.
2. Add the package via the local package manager if needed.
3. Add a `VLMNpc` component to a GameObject.
4. Configure the camera, identity, goals, actions, and decision interval.
5. Use the sample apartment builder script to create the demo scene.

## Current scope

This is intentionally a narrow MVP focused on:

- visual observation via camera
- structured world context
- action validation
- modular action execution
- memory and decision looping

The implementation is designed to be extended toward richer agent behavior, planning, and event-driven cognition.
