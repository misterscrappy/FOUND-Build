# Unity AI Handoff — FOUND 2D Rebuild

## Do not rebuild the core again

The clean domain code already owns collecting, rarity rolls, unique collectible instances, save persistence, Field Routes, Local Postmarks, state progress, Gold Foil completion awards, direct trading, trade provenance, economy, and 50-state registration.

Unity AI should treat `Assets/FOUND/Core/` as the gameplay source of truth. Fix compile errors cleanly if Unity 6.5 APIs require adjustment, but do not replace these services with a second implementation and do not add hotfix scripts.

## First action in Unity

1. Import the New York 2D artwork under `Assets/FOUND/Resources/FOUND/Art/NY/`.
2. Ensure the Unity UI (uGUI) package is installed because the current presentation controllers use `UnityEngine.UI`.
3. Let Unity compile.
4. Run `FOUND > Validate Core Content`.
5. Do not begin visual polish until validation passes.

## Visual build target

Recreate the approved FOUND 3.1.49 visual identity as native Unity UI, but keep the new implementation 2D-only.

### Required screens

- Explore
- Collection
- Field Route / Hunt
- Trade Center
- Profile / collector status
- stamp detail / reveal using one shared 2D stamp prefab

### Existing presentation code to wire

- `StampCardView` — shared stamp card for all rarities and Gold Foil
- `FoundExplorePanelController` — location and Local Postmark check-in
- `FoundRoutePanelController` — Field Route survey/collect loop
- `FoundCollectionPanelController` — collection card population
- `FoundTradePanelController` — send/redeem direct trade codes
- `FoundStateSelectorController` — all 50 states with un-authored states shown safely as coming soon
- `FoundNavigationController` — centralized screen switching

## Stamp visual rules

There is no 3D model, mesh, rotation, camera-based foil, or separate Gold Foil renderer.

Every rarity uses the same stamp geometry/layout. Variants only change 2D treatment:

- Standard: restrained neutral print treatment
- Special: green accent/treatment
- Limited: blue accent plus visible `NO. N / 500`
- Proof: purple treatment plus clearly visible `PRINTER'S PROOF`
- Alternate: alternate artwork/treatment; this replaces Archive
- Local Postmark: visible city + date cancellation mark on the exact collected copy
- Bonus Traits: visible but do not replace the base rarity
- Gold Foil: same layout, gold treatment, all normal information visible, no number, no Bonus Trait, no 3D

Gold Foil should feel premium through flat color, gradients, subtle texture, borders, typography, and 2D lighting-like illustration—not through a 3D mesh.

## State scaling rule

New York is the only authored state content in the current baseline. All 50 state shells are already registered. Do not duplicate scenes or gameplay scripts per state. Future states are content additions to the catalog and artwork folders.

## Trading rule

Do not make a second inventory system for trading. The existing service moves exact `CollectibleInstance` identities and preserves provenance. The current direct-code implementation is an offline transport for testing; a future server should replace the authority/transport boundary rather than the collection model.

## Quality rule

When a layout or gameplay issue is found, modify the owning component/service directly. Do not add files named `hotfix`, `patch`, `fix-v2`, or runtime replacement scripts.
