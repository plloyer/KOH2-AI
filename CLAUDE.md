# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**AIOverhaul** is a BepInEx mod for Knights of Honor II that enhances the game's AI through Harmony patches. The mod uses a sophisticated evaluation-based expense system where **lower eval scores = higher priority** (counterintuitive). It implements a comparative testing framework where 30% of AI kingdoms use enhanced logic while another 30% use baseline logic, with comprehensive performance logging.

## Build Commands

### Standard Build
```bash
dotnet build AIOverhaul.csproj
```
The build automatically copies the compiled DLL to `..\BepInEx\plugins\` if that directory exists (PostBuild event).

### Build Verification
Always verify 0 errors before committing:
```bash
dotnet build AIOverhaul.csproj 2>&1 | grep -i error
```

### Manual Installation
If PostBuild event doesn't run:
```bash
copy /Y "bin\Debug\AIOverhaul.dll" "..\BepInEx\plugins\AIOverhaul.dll"
copy /Y "bin\Debug\AIOverhaul.pdb" "..\BepInEx\plugins\AIOverhaul.pdb"
```

## Architecture

### Directory Structure

- **`NewSource/`** - All mod code
  - **`Logic/`** - Core AI behavior patches
    - `EconomyLogic.cs` - Economy, building construction, character hiring
    - `MilitaryLogic.cs` - Army composition, unit hiring, fortifications
    - `WarDiplomacyLogic.cs` - War declarations, peace offers, diplomacy
    - `Governor.Eval.cs` - Governor assignment logic
    - `RoyalFamily.AddChild.cs` - Royal family management
    - `Character.ChooseNewSkill.cs` - Character skill selection
    - `Kingdom.HireCharacter.cs` - Character hiring validation
    - **`Spending/`** - Expense evaluation patches (eval score manipulation)
      - `Castle.AddBuildOption.cs` - Building priority (religion, barracks placement)
      - `KingdomAI.ConsiderExpense.cs` - Character hiring gates
      - `KingdomAI.ConsiderAdoptTradition.cs` - Tradition selection
      - `KingdomAI.ConsiderIncreaseCrownAuthority.cs` - Crown authority blocking
      - `KingdomAI.AddExpense.cs` - Trade action priority boost
  - **`Constants/`** - All string and numeric constants
    - `BuildingNames.cs`, `CharacterClassNames.cs`, `ActionNames.cs`, etc. - String constants
    - `GameBalance.cs` - ALL numeric tuning values and multipliers
    - `LogCategory.cs` - Logging categories
  - **`Helpers/`** - Utility classes
    - `BuildingHelper.cs`, `KingdomHelper.cs`, `DistrictHelper.cs`, etc.
    - `ModLog.cs` - Logging wrapper
  - **`Debug/`** - Debug tools
    - `DebugOverlay.cs` - F9 overlay showing AI state
  - **`Log/`** - Performance logging
    - `EnhancedPerformanceLogger.cs` - Comparative performance tracking
  - `Plugin.cs` - Main BepInEx plugin entry point
  - `TraverseAPI.cs` - Centralized Harmony Traverse API for private member access

- **`Sources/Logic/`** - Decompiled game source (900+ files, READ-ONLY reference)
  - Used to verify API signatures before writing patches
  - Never modify these files

### Key Concepts

#### The Evaluation System (CRITICAL)

The AI uses an **inverted priority system**:
- **`eval = 0`** → Can afford immediately → **HIGHEST priority**
- **`eval = 5`** → Wait 5 turns → Medium priority
- **`eval = 30`** → **BLOCKED** (expense discarded, MAX_EVAL threshold)

**To INCREASE priority** (make AI do something sooner):
- Multiply eval by values **< 1.0**: `eval *= 0.5f` (cuts priority delay in half)
- Divide eval by values **> 1.0**: `eval /= 30f` (extreme priority boost)

**To DECREASE priority** (delay or block):
- Multiply eval by values **> 1.0**: `eval *= 10f` (10x delay)
- Set to 30 or higher to block: `eval = 30f`

See `AI_EVAL_SYSTEM.md` for comprehensive documentation with examples.

#### Game Entity Hierarchy

```
Kingdom (faction)
├── Realms[] (owned provinces)
│   ├── Castle (province capital)
│   │   ├── Buildings[]
│   │   ├── Districts[] (building slots)
│   │   └── Governor (optional court character)
│   ├── Settlements[] (villages, farms)
│   └── Features[] (resources: IronOre, Cattle, etc.)
├── Court (characters)
│   ├── King/Queen
│   ├── Marshals (military)
│   ├── Merchants (trade)
│   ├── Clerics (religion)
│   ├── Diplomats (diplomacy)
│   └── Spies (espionage)
└── Armies[] (military forces)
    ├── Leader (usually Marshal)
    └── Squads[] (unit types)
```

See `GAME_CONCEPTS.md` for detailed entity relationships.

#### Harmony Patching Strategy

All patches use **Postfix** or **Prefix** patterns:
- **Postfix**: Run after vanilla method, modify results or add new behavior
- **Prefix**: Run before vanilla method, optionally skip original (`return false`)

Patches are filtered by `AIOverhaulPlugin.IsEnhancedAI(kingdom)` to only affect 30% of kingdoms.

#### Enhanced vs Baseline AI

On game start, kingdoms are randomly assigned:
- **30% Enhanced** - Use all mod improvements
- **30% Baseline** - Vanilla behavior
- **40% Untracked** - Not logged

Player kingdom is always Enhanced when Spectator Mode is enabled (F9).

### TraverseAPI Pattern

All private game API access goes through `TraverseAPI.cs` to centralize reflection:

```csharp
// GOOD ✅
float maxCommerce = TraverseAPI.GetMaxCommerce(kingdom);

// BAD ❌ - Don't scatter Traverse calls
float maxCommerce = Traverse.Create(kingdom).Method("GetMaxCommerce").GetValue<float>();
```

## Development Workflow

### Before Writing Code

1. **Verify API exists** - Read `Sources/Logic/*.cs` to confirm method/property names
2. **Check null safety** - Game APIs frequently return null
3. **Use constants** - Never hardcode strings or numbers

### While Writing Code

1. **Constants First**:
   - String constants → `Constants/BuildingNames.cs`, `ActionNames.cs`, etc.
   - Numeric values → `Constants/GameBalance.cs`
2. **Filter for Enhanced AI** if needed:
   ```csharp
   if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true;
   ```
3. **Add logging** with appropriate `LogCategory`:
   ```csharp
   AIOverhaulPlugin.LogInfo($"Message here", LogCategory.Economy, kingdom);
   ```
4. **Respect eval direction** - See AI_EVAL_SYSTEM.md before modifying eval scores

### After Writing Code

1. **Build**: `dotnet build` → Must succeed with **0 errors**
2. **Review**: Check for null safety, hardcoded values, correct eval operations
3. **Update docs**: Sync `AI_ENHANCEMENTS.md` with code changes
4. **Commit**: Descriptive message following repo style

### Common Mistakes to Avoid

❌ **Wrong property names**: e.g., `k.vassals` instead of `k.vassalStates`
❌ **Missing null checks**: e.g., `k.realms.Count` instead of `k.realms?.Count ?? 0`
❌ **Hardcoded strings**: Always use `BuildingNames.*`, `CharacterClassNames.*`, etc.
❌ **Hardcoded numbers**: Always use `GameBalance.*` constants
❌ **Inverted eval operations**: Multiplying by boost value instead of dividing (see AI_EVAL_SYSTEM.md)
❌ **Removing code blindly**: Understand what it does first, then reimplement properly
❌ **Forgetting to update AI_ENHANCEMENTS.md**: Keep docs in sync with code

## Important Files Reference

### Must-Read Before Coding
- **`AI_INSTRUCTIONS.md`** - Critical rules, workflow, common pitfalls
- **`AI_EVAL_SYSTEM.md`** - Comprehensive eval system documentation with examples
- **`GAME_CONCEPTS.md`** - Game entity architecture and terminology
- **`AI_ENHANCEMENTS.md`** - Complete list of all AI behavior changes (keep in sync!)

### Development Guides
- **`PATCH_VALIDATION.md`** - All active Harmony patches validation status
- **`PERFORMANCE_LOGGING_GUIDE.md`** - How to interpret performance comparison logs

### Code Organization
- **`NewSource/Constants/GameBalance.cs`** - Single source of truth for ALL tuning values
- **`NewSource/TraverseAPI.cs`** - Centralized private API access
- **`Sources/Logic/*.cs`** - Decompiled game source (reference only, never modify)

## Debug Tools

### F9 Debug Overlay
Toggles real-time AI state visualization:
- Kingdom stats (Gold, Piety, Books)
- Mortal Enemy (permanent grudge, shown in red)
- Expansion Target (current target, shown in orange)
- All neighbors with relationship colors
- Considered Expenses log (shows eval scores)

Also toggles **Spectator Mode** - Enhanced AI controls player kingdom.

### Performance Logging
Three CSV files generated in `BepInEx/config/`:
- `AI_Baseline_Initial.csv` - Starting conditions for normalization
- `AI_Performance_Enhanced.csv` - Detailed metrics (growth rates, ratios)
- `AI_Aggregate_Stats.csv` - Summary statistics comparing Enhanced vs Baseline

See `PERFORMANCE_LOGGING_GUIDE.md` for interpretation.

## Mod-Specific Patterns

### Eval Score Manipulation
Always use `GameBalance.cs` constants and comment the direction:
```csharp
// Lower eval = higher priority
option.eval *= GameBalance.SwordsmithPriorityMultiplier; // 0.033 → very high priority

// Higher eval = lower priority
option.eval *= GameBalance.StrongPenaltyMultiplier; // 10.0 → much lower priority

// Block entirely
option.eval = 30f; // MAX_EVAL threshold
```

### Enhanced AI Filtering
Most patches start with:
```csharp
static bool/void Prefix/Postfix(...)
{
    if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true; // Skip for baseline

    // Enhanced AI logic here
}
```

### Logging Categories
Use appropriate categories for filtering:
```csharp
LogCategory.Economy    // Building construction, resources
LogCategory.Military   // Army composition, unit hiring
LogCategory.War        // War declarations, battles
LogCategory.Diplomacy  // Offers, relations
LogCategory.Character  // Court members, skills
LogCategory.General    // Everything else
```

### Null Safety Pattern
Game APIs return null frequently:
```csharp
int realmCount = kingdom.realms?.Count ?? 0;
bool hasIronOre = realm.features?.Contains(FeatureNames.IronOre) ?? false;
```

## Testing & Validation

### Manual Testing
1. Launch game with mod installed
2. Start new game (Shattered World recommended for quick testing)
3. Press F9 to enable Spectator Mode
4. Observe AI decisions in debug overlay
5. Check BepInEx logs for errors

### Performance Comparison
1. Play 3+ full games to 200+ years
2. Compare aggregate stats CSV across games
3. Look for consistent patterns in multiple metrics (realms, strength, gold)
4. Use normalized metrics (ratios, growth rates) for fair comparison

### Build Validation
Check for Harmony warnings in `BepInEx/LogOutput.log`:
```bash
grep -i "harmony" BepInEx/LogOutput.log | grep -i "fail\|error\|warning"
```

## Kingdom Balance Constants

All tuning values in `GameBalance.cs`:

**Economy**:
- `CommercePerMerchant = 10` - Required commerce per merchant
- `MinBooksForTraditionRush = 400` - Books threshold to start saving gold

**Military**:
- `EarlyGameArmySize = 8` - First two armies size
- `RangedToMeleeRatio = 0.8f` - 80% ranged in subsequent armies
- `HealthRetreatThreshold = 0.7f` - Retreat when army < 70% HP

**Eval Multipliers** (use with eval scores):
- `SwordsmithPriorityMultiplier = 1/30f` - Very high priority
- `FletcherPriorityMultiplier = 0.01f` - Extremely high priority
- `StrongBoostMultiplier = 0.5f` - Cut eval in half
- `StrongPenaltyMultiplier = 10.0f` - 10x delay
- `StrictBlockMultiplier = 100.0f` - Exceeds MAX_EVAL, blocks entirely

**Building Priority**:
- `ReligionBuildingBoostPerSlot = 0.2f` - Priority boost per district slot
- `BarracksSlotBoostMultiplier = 0.25f` - Barracks boost per castle district slot

## Critical Rules (Never Violate)

1. **NEVER HALLUCINATE APIs** - Always verify against `Sources/Logic/*.cs` before using any API
2. **ALWAYS COMPILE** - Build must succeed with 0 errors before committing
3. **VERIFY THEN CODE** - Read relevant source files first, then implement
4. **NO HARDCODED STRINGS** - Always use constants from `Constants/` folder
5. **NO HARDCODED VALUES** - Always use (or create) constants in `GameBalance.cs`
6. **KEEP AI_ENHANCEMENTS.md IN SYNC** - Update when implementing/modifying AI features
7. **NEVER BLINDLY REMOVE CODE** - Understand functionality first, then reimplement replacement
8. **RESPECT EVAL DIRECTION** - Lower = higher priority (see AI_EVAL_SYSTEM.md)
