# Project Scope

## Project Goal
Build a .NET whiteboard video engine that reproduces core VideoScribe-style rendering behavior such as draw progression, timing, camera motion, and export-ready frame flow, without building an editor UI or cloning the VideoScribe authoring experience.

## Current Phase Non-Goals
- No UI/editor design or implementation.
- No drag-and-drop authoring workflow.
- No production rendering/export code yet.
- No plugin ecosystem or advanced authoring tools.

## Target Output
- Deterministic frame sequences and video outputs from project/spec JSON.
- Repeatable results for identical inputs.
- A stable architecture that supports many videos in the same format with different scripts/scenarios.
- A foundation for future template-based and batch video generation.

## Core Features to Clone First
- Timeline-driven reveal of drawable objects.
- Path-based draw behavior for handwriting-like progression.
- Camera timing (pan/zoom focus changes) tied to timeline events.
- Stable frame output flow for later export integration.

## Constraints and Assumptions
- Engine-first delivery: renderer/export behavior before any authoring experience.
- Strict module boundaries: Core, Engine, Renderer, Export, CLI.
- Spec-driven scenes only; no hardcoded storyboard logic.
- Deterministic, frame-based pipeline is a hard requirement.