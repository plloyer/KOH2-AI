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
  - **`Constants/`** - All string and numeric constants
    - `ActionNames.cs`, `BuildingNames.cs`, `GameBalance.cs` (Tuning), `LogCategory.cs`, etc.
  - **`Helpers/`** - Utility classes
    - `MilitaryHelper.cs`, `KingdomHelper.cs`, `CourtHelper.cs`, `MultiplayerAIHelper.cs`, etc.
  - **`Patches/`** - Logic modifications by category
    - **`Military/`** - `KingdomAI.ThinkArmy.cs`, `BuddySystem.cs`, `Army.AddUnit.cs`
    - **`Diplomacy/`** - `KingdomAI.ThinkDiplomacy.cs`, `Offer.DecideAIAnswer.cs`
    - **`Spending/`** - `Castle.AddBuildOption.cs`, `KingdomAI.ConsiderExpense.cs`
    - **`RoyalFamily/`** - `RoyalFamily.AddChild.cs`
    - **`Emperor/`** - `EmperorOfTheWorld.StartVote.cs`
    - **`Multiplayer/`** - Chat commands and AI forcing logic
  - **`Debug/`** - Debug tools
    - `DebugOverlay.cs` - F9 overlay showing AI state
  - **`Log/`** - Performance logging (`EnhancedPerformanceLogger.cs`)
  - `Plugin.cs` - Main BepInEx plugin entry point
  - `TraverseAPI.cs` - Centralized Harmony Traverse API for private member access
  - `AutoStarter.cs` - Automated game start for testing

- **Documentation Files** (Root Directory)
  - `ARMY_MANAGEMENT_GUIDE.md` - Threat levels and Army evaluation logic
  - `AI_EVAL_SYSTEM.md` - AI Expense Evaluation System details
  - `GAME_CONCEPTS.md` - Game entity key concepts
  - `PERFORMANCE_LOGGING_GUIDE.md` - How to use/read performance logs
  - `CLAUDE.md` - This file

- **`Sources/Logic/`** - Decompiled game source (900+ files, READ-ONLY reference)
  - Used to verify API signatures before writing patches
  - Never modify these files

### Key Concepts

#### The Evaluation System (CRITICAL)

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
   AIOverhaulPlugin.LogDebug($"Message here", LogCategory.Economy, kingdom);
   ```
4. **Respect eval direction** - See AI_EVAL_SYSTEM.md before modifying eval scores

### After Writing Code

1. **Build**: `dotnet build` → Must succeed with **0 errors**
2. **Review**: Check for null safety, hardcoded values, correct eval operations
4. **Commit**: Descriptive message following repo style

### Common Mistakes to Avoid

❌ **Wrong property names**: Using non-existent properties (e.g., `k.vassals` instead of `k.vassalStates`)
❌ **Missing null checks**: Accessing lists/objects without null checks (e.g., `k.realms.Count` crashes if null)
❌ **Hardcoded strings**: Using string literals (e.g., "Barracks") instead of defined Constants
❌ **Hardcoded numbers**: Using magic numbers (e.g., `0.3f`) instead of `GameBalance` Constants
❌ **Removing code blindly**: Deleting existing game logic without understanding/reimplementing it

## Important Files Reference

### Must-Read Before Coding
- **`AI_INSTRUCTIONS.md`** - Critical rules, workflow, common pitfalls
- **`AI_EVAL_SYSTEM.md`** - Comprehensive eval system documentation with examples
- **`GAME_CONCEPTS.md`** - Game entity architecture and terminology

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
ALWAYS use `GameBalance.cs` (or top of the class) constants rather than hardcoding values in the functions.
IMPORTANT: Look for the "Constants" folder, all string should be stored in there. No hardcoding within a function.

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
General,      // Miscellaneous logs
War,          // War declarations, peace, surrenders
Military,     // Army management, battles, fortifications
Diplomacy,    // NAPs, alliances, trade agreements
Economy,      // Merchants, resources
Spending,     // About spending gold
Knights,      // Character hiring (all court members)
Tradition,    // Tradition selection and adoption
RoyalFamily,  // Heirs, succession, family management
Governor,     // Governor assignments
Spectator     // F9 spectator mode toggles
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
1. Play full game automated games
2. Compare aggregate stats CSV across games
3. Look for consistent patterns in multiple metrics (realms, strength, gold)
4. Use normalized metrics (ratios, growth rates) for fair comparison

### Build Validation
Check for Harmony warnings in `BepInEx/LogOutput.log`:
```bash
grep -i "harmony" BepInEx/LogOutput.log | grep -i "fail\|error\|warning"
```

### Automation test
Sovereign.exe -autoStart -provinces 2 -difficulty 2

## Critical Rules (Never Violate)

1. **NEVER HALLUCINATE APIs** - Always verify against `Sources/Logic/*.cs` before using any API
2. **ALWAYS COMPILE** - Build must succeed with 0 errors before committing
3. **VERIFY THEN CODE** - Read relevant source files first, then implement
4. **NO HARDCODED STRINGS** - Always use constants from `Constants/` folder
5. **NO HARDCODED VALUES** - Always use (or create) constants in `GameBalance.cs`
7. **NEVER BLINDLY REMOVE CODE** - Understand functionality first, then reimplement replacement
8. **RESPECT EVAL DIRECTION** - (see AI_EVAL_SYSTEM.md)
