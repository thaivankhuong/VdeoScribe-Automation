# Video Spec

## Purpose of Project/Spec JSON
Define a single source of truth for timeline, scene composition, camera plan, asset references, and output intent so the engine can generate consistent outputs across many scenarios.

## Top-Level Structure
- `meta`: project id, schema version, and global settings.
- `assets`: references to SVG, audio, fonts, hand overlays, and other reusable media.
- `scene`: drawable items and grouping for the current composition model.
- `timeline`: event schedule for draw/transform behavior, with camera handled as an explicit timeline track.
- `output`: frame rate, resolution, and export intent.

## Major Entities and Fields
- Drawable object: `id`, `type`, `assetRef`, `transform`, `style`, `layer`.
- Timeline event: `id`, `start`, `duration`, `targetId`, `action`, `params`.
- Camera keyframe: `time`, `x`, `y`, `zoom`, `easing`.
- Audio cue: `trackRef`, `offset`, `gain`, `fadeIn`, `fadeOut`.

## Example JSON Shape
```json
{
  "meta": { "schemaVersion": "0.1", "projectId": "demo-001" },
  "assets": {
    "svg": [{ "id": "s1", "path": "assets/svg/scene.svg" }],
    "audio": [{ "id": "a1", "path": "assets/audio/voice.mp3" }]
  },
  "scene": {
    "objects": [
      { "id": "obj-1", "type": "path", "assetRef": "s1", "layer": 1 }
    ]
  },
  "timeline": {
    "events": [
      { "id": "e1", "start": 0.0, "duration": 2.5, "targetId": "obj-1", "action": "draw" }
    ],
    "camera": [
      { "time": 0.0, "x": 0.5, "y": 0.5, "zoom": 1.0 }
    ]
  },
  "output": { "fps": 30, "width": 1920, "height": 1080 }
}