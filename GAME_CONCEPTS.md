# Knights of Honor II: Game Concepts & Architecture

This document gathers knowledge about the game's internal architecture and terminology, derived from code analysis.

## Core Entities

### Kingdom (`Logic.Kingdom`)
The top-level entity representing a faction (Player or AI).
- **Ownership**: Owns a list of **Realms** (`kingdom.realms`).
- **Resources**: Manages global resources like Gold, Books, Piety, and Levies.
- **Court**: Contains the Royal Court characters (King, Marshals, Merchants, Clerics, Spies, Diplomats).
- **Diplomacy**: Manages wars, alliances, and pacts (`kingdom.diplomacy`).

### Realm (`Logic.Realm`)
A **Realm** is the active game instance of a province. It binds ownership to geography.
- **Is-A**: Represents a single province on the map owned by a specific Kingdom.
- **Components**:
    - **Castle**: The capital/fortress of the realm (`realm.castle`).
    - **Settlements**: List of villages, farms, monasteries (`realm.settlements`).
    - **Features**: Active tags/resources in this province (`realm.features`, e.g., "IronOre", "DeepForests").
    - **Workforce**: Population available for construction or recruitment.
    - **Religion**: The dominant religion of the local population (can differ from Kingdom religion).

### Castle (`Logic.Castle`)
The central hub of a Realm.
- **Role**: The entity you interact with to build buildings or recruit units.
- **Buildings**: Contains the list of constructed buildings (`castle.buildings`).
- **Governor**: A court member assigned to govern this castle (`castle.governor`).
- **Garrison**: Units stationed inside for defense.
- **Districts**: Slots for building construction (e.g., Castle District, Town District).

### Province (`Logic.Province` / Definitions)
The **static** definition of a geographical region.
- **Role**: Defines the map layout, terrain type, neighbors, and potential resources.
- **Relationship**: A `Realm` is effectively "The current state of Province X owned by Kingdom Y". When a province is conquered, the `Realm` object might change ownership or properties, but the underlying `Province` definition remains static.

### Settlement (`Logic.Settlement`)
Secondary locations within a Realm.
- **Types**: Villages, Crop Farms, Livestock Farms, Coastal Villages, Monasteries.
- **Function**: Provide passive income, goods, or resources. They can be raided (plundered) during war but not "conquered" independently of the Castle.

### Army (`Logic.Army`)
A mobile military force on the map.
- **Composition**: Consists of multiple `Squads` (Units).
- **Leader**: Usually led by a **Marshal** (from the Kingdom's court).
- **State**: Can be in various states (Idle, Moving, Fighting, Sieging, Retreating).
- **Behavior**: Controlled by `KingdomAI` logic (e.g., `ThinkArmy` methods).

## Key Relationships

- **Kingdom** has many **Realms**.
- **Realm** has one **Castle** and many **Settlements**.
- **Realm** has **Features** (e.g., `FeatureNames.IronOre`).
- **Features** unlock **Buildings** (e.g., `IronOre` -> `Metalworking`).
- **Castle** contains **Buildings**.
- **Buildings** produce **Goods** (e.g., `Metalworking` -> `GoodsNames.Iron`).

## Terminology Mapping

| Concept | Code Class | Description |
| :--- | :--- | :--- |
| **Faction** | `Logic.Kingdom` | The political entity (France, England, etc.) |
| **Province (Instance)** | `Logic.Realm` | The owned land with dynamic state |
| **Province (Geo)** | `Logic.Province` | The immutable map region |
| **City/Fort** | `Logic.Castle` | The command center of a realm |
| **Resource/Tag** | `string` (in `realm.features`) | "IronOre", "Cattle" (use `FeatureNames`) |
| **Trade Good** | `string` (in `GoodsNames`) | "Iron", "Meat", "Dyes" |
