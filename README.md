# FOUND — Unity Rebuild

This repository is the clean Unity production rebuild of **FOUND**.

## Source of truth

The legacy application remains in `misterscrappy/FOUND`. The migration/reference baseline is **FOUND 3.1.49**, frozen from legacy commit `5994a48b3a3dfdccb0402865a86b673e12c11a6e` on branch `reference/3.1.49-unity-migration`.

The 3.1.49 build is a visual, content, and behavior reference. Its patch/hotfix architecture is not copied here.

## Current architecture decision

FOUND is now a **2D collectible-first game**. The 3D stamp concept has been removed completely.

- Every stamp uses one reusable flat stamp-card layout.
- Rarities change artwork treatment, overlays, typography, and color accents only.
- Gold Foil uses the same layout and information fields as every other stamp; it is an unnumbered state-completion reward.
- Proof uses a visible `PRINTER'S PROOF` overlay.
- Local Postmarks are visible city-and-date marks.
- Alternate replaces Archive in the clean rebuild.
- Limited Issue is the only numbered rarity and is constrained to `1–500`.

## Core systems already represented in code

- data-driven 50-state registry
- New York 3.1.49 destination/catalog data
- rarity and Bonus Trait rules
- unique collectible instance IDs
- per-copy provenance
- Field Route and check-in acquisition
- location-locked Local Postmarks
- discovery, coin, XP, level, and album milestone progression
- automatic Gold Foil state-completion awards
- persistent JSON saves with backup recovery
- direct player-to-player transfer codes with replay prevention on the receiving profile
- trade history and provenance preservation
- reusable 2D `StampCardView`
- Editor validation command: `FOUND > Validate Core Content`

## Production boundary

The local Limited Issue allocator prevents duplicate edition numbers inside one save. Truly global `1–500` uniqueness and authoritative online trading require a server/backend authority. The code isolates that responsibility behind interfaces so a backend can replace the local implementation without rewriting collecting or UI systems.

See `Docs/ARCHITECTURE.md` and `Docs/MIGRATION_BASELINE.md`.
