# FOUND Core Architecture

## Layers

### Content

`Assets/FOUND/Resources/FOUND/Content/catalog.json`

One catalog contains authored state content, stamp designs, rarity rules, and Bonus Trait rules. A compact state registry supplies all fifty state shells so additional states add content rather than gameplay code. The runtime validates IDs, state references, completion rules, Gold Foil behavior, and Limited numbering configuration on startup.

### Domain/services

`FoundCatalogService` — read-only validated content access.

`FoundCollectionService` — owned copies, unique instance checks, album completion queries, trade eligibility, and discovery tracking.

`FoundRarityRoller` — weighted rarity and Bonus Trait selection. Rarity odds do not depend on collection size or unlock thresholds.

`FoundLocationService` — local-zone distance checks and Local Postmark creation.

`FoundAcquisitionService` — creates new collectible instances for Field Routes and Check Ins.

`FoundRouteService` — reusable Field Route sessions; missing destinations are favored but never become rarity/unlock gates.

`FoundEconomyService` — coins, XP, levels, and ledger entries.

`FoundProgressionService` — first-discovery rewards, album milestones, and state Gold Foil awards.

`FoundTradeService` — exact-instance direct transfer packages, validation, replay blocking on a receiving profile, provenance, and trade history.

`JsonFileSaveRepository` — atomic local save with backup recovery.

`FoundQueryService` — collection/state summaries for UI without duplicating rules in screen code.

`FoundGame` — small composition root/facade. UI calls this layer rather than duplicating game rules.

### Presentation

`StampCardView` is the shared 2D stamp presenter. Gold Foil does not have a separate renderer. Rarity presentation is color/overlay-based and all information remains bound through the same fields.

## Save shape

A save contains a player profile, collection buckets, discoveries, per-state progression, economy ledger, trade history, and redeemed trade IDs.

Dictionaries are deliberately avoided in serialized save objects so Unity `JsonUtility` can persist the model consistently on Android without third-party JSON dependencies.

## Trading security boundary

Direct transfer codes are integrity-checked and preserve exact instance/provenance data. Creating a direct code removes the outgoing instance from the sender's local save immediately.

This is still an offline trust model. It cannot guarantee global anti-duplication across modified clients or devices. Production online trading should replace only the authority/transport boundary with a server-backed implementation; collection identity and UI do not need to be rewritten.

## Limited Issue authority boundary

`IEditionNumberAuthority` isolates the numbered-edition responsibility. `LocalEditionNumberAuthority` is suitable for development and guarantees no duplicate `1–500` number in one local save. Production must substitute a server-backed allocator to enforce global uniqueness.

## Adding another state

1. Add that state's authored definition and destination designs to the catalog.
2. Fill the state's destination stamp IDs.
3. Add an optional state completion design and set `completionDesignId`.
4. Add/import flat artwork sprites matching `artworkKey`.
5. Run `FOUND > Validate Core Content`.

No collecting, rarity, progression, trading, save, or Gold Foil code should be duplicated for the new state.
