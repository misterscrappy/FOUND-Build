# FOUND Unity Migration Baseline

## Locked legacy reference

- Legacy repository: `misterscrappy/FOUND`
- Reference branch: `reference/3.1.49-unity-migration`
- Reference commit: `5994a48b3a3dfdccb0402865a86b673e12c11a6e`
- Reference product: FOUND 3.1.49 APK

The legacy repository preserves approved visuals, content, gameplay intent, data, and behavior for comparison. It is not the production architecture for this rebuild.

## Migration rule

Preserve the approved player-facing identity of FOUND while replacing legacy implementation details with clean native Unity systems. Do not reproduce DOM/WebView rendering, hotfix chains, runtime monkey-patches, duplicate renderers, or version-specific compatibility layers.

## 2D-only rule

The 3D stamp concept is removed.

There is one reusable 2D stamp-card layout. Standard, Special, Limited, Proof, Alternate, Local Postmark presentation, Bonus Traits, and Gold Foil all use that same structural layout. Rarity changes styling and overlays rather than changing geometry or creating a separate renderer.

Gold Foil requirements:

- same stamp layout as ordinary destination stamps
- all catalog/title/place/lore/history/legal information remains visible
- unnumbered
- no Bonus Trait
- awarded to every collector who completes a state's required destination set
- not randomly rolled
- not tradeable

## Rarity model

Rollable rarities:

1. Standard Issue
2. Special Issue
3. Limited Issue
4. Proof Issue
5. Alternate

Gold Foil is completion-only and outside the random rarity pool.

Limited Issue is the only numbered rarity. Its production contract is globally unique numbers `1–500` per stamp design. Local development allocation is not represented as globally authoritative.

## Fifty-state architecture

All fifty states are registered in the content model now. New York is populated from the 3.1.49 baseline. Additional states add data/art, not new gameplay code.

Each state definition supplies:

- state code and name
- album identity
- destination stamp IDs
- optional completion stamp ID

Each stamp design supplies:

- stable internal ID
- catalog number
- state/album placement
- place/title
- artwork resource key
- observations
- local-postmark coordinates/radius
- lore/history
- base value
- completion/tradeability flags

## Collectible-instance contract

Every owned stamp is an individual instance with:

- immutable instance ID
- stamp design ID
- rarity
- Bonus Trait
- optional Limited edition number
- acquisition time/source
- optional Local Postmark
- calculated value
- provenance history

Trading moves the exact instance rather than creating a new copy.

## Location behavior

Ordinary stamps are not location-locked. A player can collect stamps from other states through normal collecting systems.

Local Postmarks are location-locked. A qualifying check-in inside a stamp's configured local radius adds the visible city-and-date postmark to that exact collectible instance.

## Validation rule

New features are integrated into the core service/model architecture. Do not solve defects by appending a new hotfix file or replacing behavior at runtime.
