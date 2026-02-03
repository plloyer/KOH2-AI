---
name: koh2-feature
description: Implement features for Knights of Honor II AI Overhaul with rigorous API validation
---

You are implementing a feature for the Knights of Honor II AI Overhaul mod with STRICT requirements.

# REQUIRED SKILL

**Always also load the `unity-development` skill** when implementing features. This ensures C# code quality, Unity best practices, and proper code review standards are followed alongside the KOH2-specific rules below.

# CRITICAL RULES - NEVER VIOLATE THESE

1. **NEVER HALLUCINATE APIs** - Always verify against Sources/Logic/*.cs before using any API
2. **ALWAYS COMPILE** - Build must succeed with 0 errors before committing
3. **VERIFY THEN CODE** - Read relevant source files first, then implement
4. **REVISE BEFORE COMMIT** - Review for issues and improvements after implementation
5. **COMMIT WHEN DONE** - Git commit with descriptive message when complete
6. **NEVER BLINDLY REMOVE CODE** - If a method/API doesn't exist, understand what it does first, then implement a replacement. DO NOT just delete calls without preserving functionality
7. **NO HARDCODED STRINGS** - Always use constants from Constants/ folder (BuildingNames.*, CharacterClassNames.*, ActionNames.*, etc.) for all KOH string. For our own, if only used within a single class, have it as a const at the top of the class.
8. **NO HARDCODED VALUES** - Always use (or create) constants in GameBalance.cs for all numeric values. If the numeric value is only used in one class, keep it as a const at the top of the class.
9. **NO HARDCODED VALUES IN COMMENTS** - Reference constant names in comments (e.g., "MinArmiesForWar armies" not "2 armies") so comments stay accurate when values change
10. **NO REDUNDANT MODIFIERS** - Omit `private` on class members (it's the default in C#)
11. **USE TARGET-TYPED NEW** - Use `new()` instead of `new Type()` when type is evident (e.g., `List<int> list = new();`)
12. **KEEP AI_ENHANCEMENTS.md IN SYNC** - When implementing/modifying AI features, update AI_ENHANCEMENTS.md to match

# PROJECT STRUCTURE

```
NewSource/
├── Plugin.cs                    # Main BepInEx plugin entry point
├── TraverseAPI.cs               # Centralized Harmony Traverse API for reflection
├── AutoStarter.cs               # Automated game start for testing
│
├── Constants/                   # All string and numeric constants
│   ├── ActionNames.cs           # Character action names
│   ├── AIStatusNames.cs         # AI status identifiers
│   ├── BattleTacticNames.cs     # Battle tactics
│   ├── BuildingNames.cs         # Building types
│   ├── BuildingUpgradeNames.cs  # Building upgrade names
│   ├── CampaignVarNames.cs      # Campaign variable names
│   ├── CharacterClassNames.cs   # Character classes (Marshal, Merchant, etc.)
│   ├── Constants.cs             # General constants
│   ├── DiplomacyConstants.cs    # Diplomacy-related constants
│   ├── DistrictNames.cs         # District types
│   ├── FeatureNames.cs          # Realm features (IronOre, Cattle, etc.)
│   ├── GameBalance.cs           # ALL TUNABLE NUMERIC VALUES
│   ├── GlobalConstants.cs       # Global constants
│   ├── GoodsNames.cs            # Trade goods
│   ├── KingdomNames.cs          # Kingdom identifiers
│   ├── LogCategory.cs           # Log categories (War, Military, Economy, etc.)
│   ├── MapNames.cs              # Map identifiers
│   ├── SettlementNames.cs       # Settlement names
│   ├── SkillNames.cs            # Character skills
│   └── TraditionNames.cs        # Kingdom traditions
│
├── Helpers/                     # Utility/extension method classes
│   │                            # Add extension methods here, organized by game type
│   │                            # e.g., KingdomHelper.cs for Kingdom extensions
│   └── ...
│
├── Patches/                     # Harmony patches organized by category
│   ├── Military/                # Army, battle, unit hiring patches
│   ├── Diplomacy/               # War declaration, offers, peace patches
│   ├── Spending/                # Building, expense evaluation patches
│   ├── Court/                   # Character, court member patches
│   ├── RoyalFamily/             # Succession, family patches
│   ├── Logging/                 # Logging-only patches (no behavior change)
│   └── ...                      # Create new subfolders for new categories
│
├── Log/                         # Performance logging and metrics
│
└── Debug/                       # Debug tools (F9 overlay, debug patches)
```

## Constant Placement Rules

- **Strings matching KOH game code/values** → Always in `Constants/` folder (e.g., building names, skill names)
- **Numeric tuning values** → Always in `Constants/GameBalance.cs`
- **Constants used only in one class** → Can be defined at top of that class
- **Constants used across multiple files** → Must go in `Constants/` folder

# VANILLA GAME SYSTEM REFERENCES

Before modifying AI systems, **read the relevant reference documentation** to understand vanilla behavior.

Reference files are located in `.claude/skills/koh2-feature/references/`:

| System | Reference File | Read Before Modifying |
|--------|----------------|----------------------|
| **Spending System** | `references/AI_SPENDING_SYSTEM.md` | Expense flow, eval system, queue selection, SpendExpense (`Patches/Spending/`) |
| **Military AI** | `references/ARMY_MANAGEMENT_GUIDE.md` | Army assignment, ThinkArmy, ThinkFight, threat levels (`Patches/Military/`) |
| **Diplomacy** | `references/LAND_DIPLOMACY_SYSTEM.md` | Land offers/demands, realm cost, diplomatic AI (`Patches/Diplomacy/`) |
| **Game Concepts** | `references/GAME_CONCEPTS.md` | Core entities (Kingdom, Realm, Castle), AI architecture overview |

**Critical:** The eval system has a naming collision - `BuildOption.eval` (higher = better) vs `Expense.eval` (lower = better). Read the "Dual Eval System" section in `references/AI_SPENDING_SYSTEM.md` before touching any eval-related code.

# WORKFLOW - FOLLOW EXACTLY

## Phase 1: API Discovery (MANDATORY)

Before writing ANY code:

1. **Identify what game objects you need to interact with**
   - Kingdom? Army? Castle? Realm? Character?

2. **Read the decompiled source for those types**
   ```
   Use Read tool on: Sources/Logic/Kingdom.cs, Sources/Logic/Army.cs, etc.
   ```

3. **Read GameBalance.cs for current constant values**
   ```
   Use Read tool on: NewSource/Constants/GameBalance.cs
   ```
   - Never assume constant values - always fetch current values
   - If you need a new constant, add it to GameBalance.cs

4. **Document the EXACT properties and methods available**
   - Property names (e.g., `k.vassalStates` NOT `k.vassals`)
   - Property types (e.g., `List<Army>` vs `HashSet<Kingdom>`)
   - Method signatures (e.g., `army.EvalStrength()` returns what type?)

5. **Check for null safety patterns**
   - Use `?.` operator: `k.realms?.Count ?? 0`
   - Guard against empty collections

## Phase 2: Implementation

1. **Determine if this is Enhanced AI only or all kingdoms**
   - Enhanced only: Add `if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;`
   - All kingdoms: No filter needed

2. **Choose correct Harmony patch type**
   - **Prefix with return false**: Replace original method entirely
   - **Postfix**: Add logic after original method runs
   - **Transpiler**: Modify IL (advanced, avoid if possible)

3. **Follow the Harmony patch naming convention (MANDATORY):**
   - **Class name**: `OriginalClass_MethodName` (underscore separator)
   - **File name**: `OriginalClass.MethodName.cs` (dot separator)
   - **Location**: Appropriate subfolder in `Patches/`

   **Examples:**
   | Patching | Class Name | File Name | Location |
   |----------|------------|-----------|----------|
   | `Offer.DecideAIAnswer` | `Offer_DecideAIAnswer` | `Offer.DecideAIAnswer.cs` | `Patches/Diplomacy/` |
   | `KingdomAI.ThinkArmy` | `KingdomAI_ThinkArmy` | `KingdomAI.ThinkArmy.cs` | `Patches/Military/` |
   | `Castle.AddBuildOption` | `Castle_AddBuildOption` | `Castle.AddBuildOption.cs` | `Patches/Spending/` |

4. **Write the patch following patterns:**
   ```csharp
   // File: Patches/Diplomacy/Offer.DecideAIAnswer.cs
   [HarmonyPatch(typeof(Logic.Offer), "DecideAIAnswer")]
   public class Offer_DecideAIAnswer
   {
       static bool Prefix(Logic.Offer __instance, ref bool __result)
       {
           // Enhanced AI only (if applicable)
           if (!AIOverhaulPlugin.IsEnhancedAI(__instance.from)) return true;

           // Null checks
           if (__instance.from == null) return true;

           // Your logic using VERIFIED APIs only

           // If replacing original:
           __result = yourValue;
           return false; // Skip original
       }
   }
   ```

5. **Add logging for debugging**
   ```csharp
   AIOverhaulPlugin.LogInfo("Your message here", LogCategory.General, kingdom);
   AIOverhaulPlugin.LogWarning("Warning message", LogCategory.General, kingdom);
   AIOverhaulPlugin.LogError("Error message", LogCategory.General, kingdom);
   AIOverhaulPlugin.LogDebug("Debug message (England only)", LogCategory.General, kingdom);
   ```

## Phase 3: Compilation Check (MANDATORY)

1. **Build the project**
   ```bash
   dotnet build
   ```

2. **If errors occur:**
   - Read the EXACT error message
   - Check Sources/Logic/*.cs for correct API
   - Fix and rebuild
   - Repeat until 0 errors

3. **Do NOT proceed to Phase 4 until build succeeds**

## Phase 4: Review and Improve (MANDATORY)

After successful build, review your code for:

**Correctness:**
- [ ] All APIs verified against Sources/Logic/*.cs
- [ ] Null checks on all property accesses
- [ ] Division by zero guards for calculations
- [ ] Edge cases handled (empty lists, defeated kingdoms, etc.)

**Code Quality:**
- [ ] Clear variable names
- [ ] Logical comments for complex logic
- [ ] No magic numbers (use named constants)
- [ ] No numeric values in comments (reference constant names instead)
- [ ] Consistent with existing code style

**Performance:**
- [ ] No expensive operations in hot loops
- [ ] Cache repeated calculations
- [ ] Avoid LINQ where simple loops work

**Integration:**
- [ ] Doesn't break existing patches
- [ ] Logging messages are clear and useful
- [ ] Enhanced AI filter applied correctly
- [ ] No unintended side effects

## Phase 5: Git Commit (MANDATORY)

1. **Check git status**
   ```bash
   git status
   ```

2. **Review changes**
   ```bash
   git diff
   ```

3. **Stage modified files**
   ```bash
   git add <files>
   ```

4. **Commit with descriptive message**

# GAME API REFERENCE (ALWAYS VERIFY FIRST)

## Common Types and Properties

**Logic.Kingdom** (VERIFY in Sources/Logic/Kingdom.cs)
- `k.id` (int)
- `k.Name` (string)
- `k.realms` (List<Realm>)
- `k.armies` (List<Army>)
- `k.resources` (Dictionary - use `[ResourceType.Gold]`)
- `k.income` (use `.Get(ResourceType.Gold)`)
- `k.wars` (List<War>)
- `k.allies` (List<Kingdom>) - NOT `k.pacts`
- `k.vassalStates` (List<Kingdom>) - NOT `k.vassals`
- `k.neighbors` (HashSet - iterate with `foreach`)
- `k.religion` (Religion object with `.name` property)
- `k.royalFamily` (RoyalFamily)
- `k.traditions` (List<Tradition>)
- `k.ai` (KingdomAI)
- `k.game` (Game)
- `k.IsDefeated()` (method)
- `k.IsEnemy(Kingdom k)` (method)
- `k.IsAlly(Kingdom k)` (method)
- `k.GetRelationship(Kingdom k)` (method)
- `k.HasTradeAgreement(Kingdom k)` (method)
- `k.HasStance(Kingdom k, RelationUtils.Stance)` (method)

**Logic.Army** (VERIFY in Sources/Logic/Army.cs)
- `army.realm_in` (Realm)
- `army.kingdom_id` (int)
- `army.units` (List<Unit>)
- `army.EvalStrength()` (method - returns int)
- `army.castle` (Castle?)
- `army.battle` (Battle?)
- `army.ai_status` (string)
- `army.IsValid()` (method)
- `army.Stop()` (method)
- `army.GetTarget()` (method)

**Logic.Realm** (VERIFY in Sources/Logic/Realm.cs)
- `realm.armies` (List<Army>)
- `realm.castle` (Castle)
- `realm.kingdom_id` (int)
- `realm.name` (string)
- `realm.neighbors` (List<Realm>)
- `realm.threat` (Threat object with `.level` property)
- `realm.IsDisorder()` (method)

**Logic.Castle** (VERIFY in Sources/Logic/Castle.cs)
- `castle.name` (string)
- `castle.army` (Army?)
- `castle.realm` (Realm)
- `castle.buildings` (List<Building>)
- `castle.battle` (Battle?)

**Logic.Character** (VERIFY in Sources/Logic/Character.cs)
- `character.GetSkillRank(string skillName)` (method - returns int)
- `character.class_name` (string)
- `character.class_def` (ClassDef with `.id` property)

**Logic.Game** (VERIFY in Sources/Logic/Game.cs)
- `game.kingdoms` (List<Kingdom>)
- `game.session_time` (has `.hours` property)
- `game.time` (float)
- `game.defs` (DefinitionsLibrary)
- `game.GetKingdom(int id)` (method)

## Mod Helper Classes (Extension Methods)

**KingdomHelper** (NewSource/Helpers/KingdomHelper.cs)
```csharp
// Resource access
k.GetGold()              // float - current gold
k.GetFood()              // float - current food
k.GetBooks()             // float - current books
k.GetGoldIncome()        // float - gold income per tick
k.GetFoodIncome()        // float - food income per tick

// Validation
k.IsEnhancedAI()         // bool - is this kingdom using enhanced AI
k.IsValidKingdom()       // bool - not null and not defeated
k.HasDisorder()          // bool - any realm in disorder

// Military
k.GetTotalPower()        // float - total military strength (armies + castles)
k.GetTotalArmyStrength() // float - total army strength only
k.GetNeighborThreat()    // float - total power of enemy neighbors
k.HasHighThreat()        // bool - any realm under attack or siege
k.IsSiegingEnemyCastle() // bool - currently sieging an enemy
k.IsDesperate()          // bool - very low military strength

// Diplomacy
k.IsStrategicNeighbor(Kingdom b)       // bool - shares border
k.HasCommonEnemyWithAlly(Kingdom b)    // bool - fighting same enemy
k.IsMortalEnemy(Kingdom enemy)         // bool - is this our mortal enemy
k.IsDominantIn1v1War()                 // bool - winning 1v1 war by SoloAttackStrengthRatio
k.SelectExpansionTarget()              // Kingdom - best target for expansion
k.FindNonAggressionTarget(Kingdom target) // Kingdom - best NAP candidate
k.WantsInvasionPlan()                  // bool - needs allies for attack
k.ShouldSeekDefensivePact()            // bool - needs defensive allies
k.FindBestDefensivePactTarget()        // Kingdom - best alliance candidate
k.InviteNeighborsToWar(War war, KingdomAI ai) // void - invite friendly neighbors

// Buildings
k.HasBuilding(string buildingName)     // bool - has at least one
k.HasBuildingUpgrade(string upgradeId) // bool - has upgrade
k.GetTradeAgreementCount()             // int - number of trade agreements

// Distance
k.IsRealmWithinDistance(Realm target, int maxDist, out int dist) // bool
```

**WarHelper** (NewSource/Helpers/WarHelper.cs)
```csharp
war.GetEnemiesInWar(Kingdom k)  // List<Kingdom> - enemies in this war
war.GetAlliesInWar(Kingdom k)   // List<Kingdom> - allies in this war
```

**MilitaryHelper** (NewSource/Helpers/MilitaryHelper.cs)
```csharp
MilitaryHelper.HasTwoReadyArmies(Kingdom k)  // bool - has 2 full-strength armies
MilitaryHelper.FindNearbyEnemyRealmInDisorder(Kingdom k, int maxDist)  // Realm or null
```

**AIOverhaulPlugin** (NewSource/Plugin.cs)
```csharp
AIOverhaulPlugin.IsEnhancedAI(Kingdom k)     // bool - check if Enhanced AI
AIOverhaulPlugin.IsBaselineAI(Kingdom k)     // bool - check if Baseline AI
AIOverhaulPlugin.GetMortalEnemy(Kingdom k, Game g) // Kingdom - get mortal enemy
AIOverhaulPlugin.MortalEnemies               // Dictionary<int, int> - kingdom ID to enemy ID
AIOverhaulPlugin.ExpansionTargets            // Dictionary<int, int> - kingdom ID to target ID

// Logging
AIOverhaulPlugin.LogInfo(string msg, LogCategory cat, Kingdom k)
AIOverhaulPlugin.LogWarning(string msg, LogCategory cat, Kingdom k)
AIOverhaulPlugin.LogError(string msg, LogCategory cat, Kingdom k)
AIOverhaulPlugin.LogDebug(string msg, LogCategory cat, Kingdom k)  // England only
```

## Constants (MANDATORY - NEVER hardcode)

**String Constants:**
- `SkillNames.*` - Character skills (e.g., `SkillNames.Leadership`)
- `CharacterClassNames.*` - Classes (e.g., `CharacterClassNames.Merchant`)
- `BattleTacticNames.*` - Battle tactics
- `TraditionNames.*` - Traditions (e.g., `TraditionNames.WritingTradition`)
- `BuildingNames.*` - Buildings (e.g., `BuildingNames.Barracks`)
- `BuildingUpgradeNames.*` - Upgrades (e.g., `BuildingUpgradeNames.Swordsmith`)
- `ActionNames.*` - Actions (e.g., `ActionNames.Trade`)
- `DistrictNames.*` - Districts (e.g., `DistrictNames.Castle`)
- `FeatureNames.*` - Map features (e.g., `FeatureNames.IronOre`)
- `GoodsNames.*` - Trade goods (e.g., `GoodsNames.Iron`)
- `AIStatusNames.*` - AI status identifiers
- `CampaignVarNames.*` - Campaign variable names

**Numeric Constants (GameBalance.cs):**

**IMPORTANT:** Always read `NewSource/Constants/GameBalance.cs` to get current values. Never assume values - they are frequently tuned.

Common constant categories (read file for actual names and values):
- Battle thresholds (win chance, retreat health)
- Army composition (counts, sizes, ratios)
- Buddy system (distances, intervals)
- Evaluation multipliers (boost/penalty factors)
- War score thresholds
- Diplomacy power ratios
- Economy thresholds (gold, books, commerce)

**If a constant doesn't exist, CREATE it in GameBalance.cs** with a descriptive name and comment.

## Common Patterns

**Null-safe property access:**
```csharp
int realmCount = k.realms?.Count ?? 0;
float gold = k.GetGold();  // Use helper method
```

**Iterating safely:**
```csharp
if (k.armies != null)
{
    foreach (var army in k.armies)
    {
        if (army == null) continue;
        // Use army
    }
}
```

**Iterating neighbors (HashSet):**
```csharp
if (k.neighbors != null)
{
    foreach (var neighbor in k.neighbors)
    {
        if (neighbor is Logic.Kingdom nk && !nk.IsDefeated())
        {
            // Use nk
        }
    }
}
```

**Division guards:**
```csharp
float ratio = denominator > 0 ? numerator / denominator : 0f;
```

**Calling private methods via TraverseAPI:**
```csharp
int warSide = TraverseAPI.GetWarSide(war, kingdom);
float maxCommerce = TraverseAPI.GetMaxCommerce(kingdom);
```

## Common Pitfalls (AVOID THESE)

❌ **Using wrong property names:**
- `k.vassals` → Use `k.vassalStates`
- `k.pacts` → Use `k.allies`
- `k.religion.id` → Use `k.religion.name`

❌ **Forgetting null checks:**
- `k.realms.Count` → Use `k.realms?.Count ?? 0`

❌ **Wrong type conversions:**
- LINQ Average() returns double → Cast to `(float)`

❌ **Namespace conflicts:**
- `Path` is ambiguous → Use `using IOPath = System.IO.Path;`

❌ **BLINDLY REMOVING CODE when API doesn't exist:**
- WRONG: Deleting method calls because method doesn't exist
- RIGHT: Understand functionality, implement replacement, restore logic

❌ **Hardcoded strings:**
```csharp
// WRONG
if (building.def.id == "Barracks")

// CORRECT
if (building.def.id == BuildingNames.Barracks)
```

❌ **Hardcoded numeric values:**
```csharp
// WRONG
if (books >= 400)

// CORRECT
if (books >= GameBalance.MinBooksForFirstTradition)
```

❌ **Redundant access modifiers:**
```csharp
// WRONG
private void MyMethod() { }

// CORRECT - private is default
void MyMethod() { }
```

❌ **Verbose object instantiation:**
```csharp
// WRONG
Dictionary<int, int> cache = new Dictionary<int, int>();

// CORRECT
Dictionary<int, int> cache = new();
```

❌ **Hardcoded values in comments (CRITICAL):**
Comments with numeric values become stale when constants change. Always reference constant names.
```csharp
// WRONG - values will become outdated
// Require 2 full armies before attacking
// Retreat at 70% health
// Wait 30 days before re-evaluating
// Boost priority by 1.5x

// CORRECT - reference constant names
// Require MinArmiesToDeclareWar full armies before attacking
// Retreat at HealthRetreatThreshold health
// Wait BuddyReevalIntervalMinutes before re-evaluating
// Boost priority by MediumBoostMultiplier
```

## File Organization

**Patch naming convention (MANDATORY):**
- File name: `OriginalClass.MethodName.cs` (e.g., `Offer.DecideAIAnswer.cs`)
- Class name: `OriginalClass_MethodName` (e.g., `Offer_DecideAIAnswer`)
- One patch per file for clarity

**Where to add new patches:**
- Military/Battle: `Patches/Military/` (e.g., `KingdomAI.ThinkArmy.cs`)
- Diplomacy/War declaration: `Patches/Diplomacy/` (e.g., `Offer.DecideAIAnswer.cs`)
- Economy/Spending: `Patches/Spending/` (e.g., `Castle.AddBuildOption.cs`)
- Court/Characters: `Patches/Court/`
- Royal Family: `Patches/RoyalFamily/`
- Logging only: `Patches/Logging/`
- New category: Create new subfolder in `Patches/`

**Where to add helpers:**
- Kingdom methods: `Helpers/KingdomHelper.cs` (as extension methods)
- Army methods: `Helpers/ArmyHelper.cs`
- War methods: `Helpers/WarHelper.cs`
- New domain: Create new file in `Helpers/`

# YOUR RESPONSIBILITY

- Write production-quality code
- Never guess API names
- Always compile successfully
- Review critically before committing
- Provide clear commit messages
- Explain what you did to the user

The user trusts you to maintain the quality and stability of this mod. Do not compromise on these standards.
