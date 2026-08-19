# FOUND Unity Migration Baseline

## Locked legacy reference

- Legacy repository: `misterscrappy/FOUND`
- Reference branch: `reference/3.1.49-unity-migration`
- Reference commit: `5994a48b3a3dfdccb0402865a86b673e12c11a6e`
- Reference product: FOUND 3.1.49 APK

The legacy repository is not the production architecture for the Unity rebuild. It exists to preserve approved visuals, content, gameplay intent, data, and behavior for comparison.

## Migration rule

When the Unity rebuild conflicts with the legacy implementation, preserve the approved player-facing result but prefer a clean native Unity implementation. Do not reproduce legacy hotfix chains, DOM/WebView rendering, or patch-on-patch behavior.

## Core product requirements

The new architecture must be designed for:

1. Fifty-state content expansion without duplicating core gameplay code.
2. Data-driven states, locations, stamp designs, rarity variants, and individual collectible instances.
3. Unique collectible identity and provenance suitable for player-to-player trading.
4. Location-aware local postmarks without blocking non-local collection of ordinary stamps.
5. Native 3D stamp viewing with predictable mesh/material behavior.
6. Rarity-specific visual treatments implemented with reusable materials, shaders, textures, decals/overlays, and controlled geometry.
7. Persistent collection/save data with a clear migration strategy.
8. Portrait-first mobile UI and touch interactions.
9. Stable production branches: experimental work must not patch the stable build in place.

## First production vertical slice

Before scaling to additional states, the Unity version should prove New York at production quality:

- app shell and navigation
- Explore
- Collection/state album
- stamp viewer
- one complete collectible acquisition/reveal flow
- New York stamp data/content
- rarity presentation
- local postmark behavior
- Field Route/Hunt core loop
- persistence
- trading-ready collectible instance model

Trading UI/networking may be implemented after the collectible identity model is proven, but the data model must support trading from the beginning.

## 3D quality rule

Do not regenerate a whole stamp mesh to make simple art/layout corrections. Use a stable base stamp mesh and separate responsibilities:

- geometry: physical stamp shape/depth
- textures: artwork and printed information
- materials/shaders: paper, ink, foil, holographic/security behavior, emboss/deboss response
- overlays/decals: postmarks, proof marks, signatures, security marks, controlled variant effects

This is intended to make visual corrections deterministic and prevent unrelated 3D regressions.
