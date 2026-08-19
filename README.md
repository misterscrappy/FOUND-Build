# FOUND — Unity Rebuild

This repository is the clean Unity production rebuild of **FOUND**.

## Source of truth

The legacy application remains in `misterscrappy/FOUND`. The migration/reference baseline is **FOUND 3.1.49**, frozen from legacy commit `5994a48b3a3dfdccb0402865a86b673e12c11a6e` on branch `reference/3.1.49-unity-migration`.

The 3.1.49 build is a **visual, content, and behavior reference**. Its patch/hotfix architecture is not to be copied into this repository.

## Rebuild principles

- Native Unity implementation; no embedded legacy web runtime.
- Preserve approved FOUND artwork, layouts, content, and identity while improving polish.
- Build systems cleanly before scaling content to all 50 states.
- 3D stamp presentation uses stable meshes, materials, shaders, textures, and controlled overlays rather than per-fix mesh regeneration.
- Trading, unique collectible instances, provenance, local postmarks, rarity variants, and persistence are first-class systems.
- New work is developed in focused branches and merged only after validation.

## Planned Unity structure

`Assets/FOUND/` will contain game-owned art, audio, materials, models, prefabs, scenes, scripts, shaders, stamp data, and UI.

See `Docs/MIGRATION_BASELINE.md` for the migration contract.
