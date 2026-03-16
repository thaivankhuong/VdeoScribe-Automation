---
name: ffmpeg-export
description: Design the frame-to-video export pipeline, encoding strategy, audio merge flow, and output packaging for the whiteboard engine. Use when planning final video export architecture. Do not use for frame rendering logic, timeline calculation, camera behavior, or SVG animation design.
---

## Purpose
Define a clean and repeatable export pipeline that converts deterministic rendered frames and audio inputs into final video deliverables such as MP4.

## When to Use
- Planning frame-sequence to video assembly.
- Choosing encoding strategy, output profiles, and quality boundaries.
- Defining narration/music merge flow and sync checkpoints.
- Designing export-stage validation, retry points, and failure handling.

## Rules
- Keep export isolated from render planning, animation logic, and domain modeling.
- Treat input as deterministic frame outputs plus explicit render/audio metadata.
- Require clear handoff contracts: ordered frames, FPS, duration, audio assets, and output profile.
- Prefer minimal, justified FFmpeg option sets and dependency usage.
- Define repeatable export stages with explicit failure points and validation checks.
- Do not implement FFmpeg commands or code in this phase unless explicitly requested.

## Expected Output
- Export pipeline stages and handoff contracts.
- Encoding, audio merge, and packaging decision guidelines.
- Risks and tradeoffs such as quality vs speed, file size vs bitrate, and sync stability.
- Validation checklist for output consistency.