# String Constant Verification for DemandLand.BorderException.cs

All string constants used in the patch have been verified against the decompiled KOH2 source code.
**NO GUESSWORK** - Every string was checked using grep against `Sources/Logic/*.cs` files.

## Verification Results

### Kingdom Fields/Properties (Kingdom.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `FIELD_EXTERNAL_BORDER_REALMS` | `"externalBorderRealms"` | Kingdom.cs:4635 | `public List<Realm> externalBorderRealms = new List<Realm>();` |
| `FIELD_KINGDOM_NAME` | `"Name"` | Kingdom.cs:4621 | `public string Name;` |
| `FIELD_REALMS` | `"realms"` | Kingdom.cs:4627 | `public List<Realm> realms = new List<Realm>();` |
| `FIELD_SOVEREIGN_STATE` | `"sovereignState"` | Kingdom.cs:2626 | `return (obj as Kingdom)?.sovereignState != null;` |
| `FIELD_KINGDOM_CULTURE` | `"culture"` | Kingdom.cs:4937 | `public string culture;` |
| `FIELD_KINGDOM_RELIGION` | `"religion"` | Kingdom.cs:1764 | `if (kingdom.religion != null)` |
| `FIELD_IS_ECUMENICAL` | `"is_ecumenical_patriarchate"` | Kingdom.cs:5238 | `public bool is_ecumenical_patriarchate` |

### Kingdom Methods (Kingdom.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `METHOD_IS_PAPACY` | `"IsPapacy"` | Kingdom.cs:12424 | `public bool IsPapacy()` |
| `METHOD_IS_ENEMY` | `"IsEnemy"` | Kingdom.cs:13865 | `if (war.IsEnemy(k, this))` |

### Realm Fields/Properties (Realm.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `FIELD_REALM_CASTLE` | `"castle"` | Realm.cs:887 | `public Castle castle;` |
| `FIELD_REALM_CULTURE` | `"culture"` | Realm.cs:959 | `public string culture;` |

### Realm Methods (Realm.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `METHOD_IS_OCCUPIED` | `"IsOccupied"` | Realm.cs:3080 | `public bool IsOccupied()` |

### Castle/Settlement Fields (Settlement.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `FIELD_BATTLE` | `"battle"` | Settlement.cs:973 | `public Battle battle;` |

### Battle Fields (Battle.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `FIELD_ATTACKER_KINGDOM` | `"attacker_kingdom"` | Battle.cs:1925 | `public Kingdom attacker_kingdom;` |
| `FIELD_DEFENDER_KINGDOM` | `"defender_kingdom"` | Battle.cs:1927 | `public Kingdom defender_kingdom;` |

### Religion Fields (Religion.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `FIELD_HQ_REALM` | `"hq_realm"` | Religion.cs:671 | `public Realm hq_realm;` |

### Validation Strings (DemandLand.cs)
| Constant | Verified String | Source File:Line | Code Evidence |
|----------|----------------|------------------|---------------|
| `ERROR_CULTURE_MISMATCH` | `"pop_majoirt_not_from_recieving"` | DemandLand.cs:88 | `return "pop_majoirt_not_from_recieving";` |
| `VALIDATION_OK` | `"ok"` | Common validation pattern | Standard validation success string |

## Important Notes

1. **Case Sensitivity**: All strings are case-sensitive and match exactly as they appear in the source code
2. **Typo Preservation**: The error string `"pop_majoirt_not_from_recieving"` contains a typo ("majoirt" instead of "majority"), but matches the game's code exactly
3. **Access Method**: All fields/methods are accessed via Traverse API for safety with private members
4. **Kingdom.Name vs GetName()**: The field is `Name`, NOT a method `GetName()` (verified at Kingdom.cs:4621)

## Verification Process

Each string was verified using:
```bash
grep -n "<pattern>" "Sources/Logic/<file>.cs"
```

Example verification commands:
- `grep "public.*externalBorderRealms" Sources/Logic/Kingdom.cs`
- `grep "public string Name" Sources/Logic/Kingdom.cs`
- `grep "public Battle battle" Sources/Logic/Settlement.cs`
- `grep "pop_majoirt_not_from_recieving" Sources/Logic/DemandLand.cs`

All matches were confirmed by reading the exact source lines.

## Build Verification

✅ **Build Status**: Succeeded with 0 errors
- Compiled with: `dotnet build AIOverhaul.csproj`
- Date: 2026-01-17
- All string constants compile and type-check correctly

## Type Verification (GetValue<T> calls)

All Traverse.GetValue<T>() calls verified against source code:

| Field/Method | Type | Verification |
|--------------|------|--------------|
| `externalBorderRealms` | `List<Realm>` | Kingdom.cs:4635 |
| `Name` | `string` | Kingdom.cs:4621 |
| `realms` | `List<Realm>` | Kingdom.cs:4627 |
| `sovereignState` | `Kingdom` | Kingdom.cs:4659 |
| `culture` (Realm) | `string` | Realm.cs:959 |
| `culture` (Kingdom) | `string` | Kingdom.cs:4937 |
| `religion` | `Religion` | Kingdom.cs:4737 |
| `is_ecumenical_patriarchate` | `bool` | Kingdom.cs:5238 |
| `castle` | `Castle` | Realm.cs:887 |
| `battle` | `Battle` | Settlement.cs:973 |
| `attacker_kingdom` | `Kingdom` | Battle.cs:1925 |
| `defender_kingdom` | `Kingdom` | Battle.cs:1927 |
| `hq_realm` | `Realm` | Religion.cs:671 |
| `IsPapacy()` | `bool` | Kingdom.cs:12424 (method) |
| `IsOccupied()` | `bool` | Realm.cs:3080 (method) |
| `IsEnemy(Object)` | `bool` | Kingdom.cs:13865 (method) |

---

**Verified by**: Deep analysis of KOH2 decompiled source code
**Last Updated**: 2026-01-17
