# AI Army Management Guide

This guide documents the vanilla Knights of Honor II three-tier military AI architecture: **AssignArmy**, **ThinkArmy**, and **ThinkFight**.

## 1. Overview: Three-Tier Military Architecture

The AI's military decision-making follows a hierarchical structure:

| Method | Role | Scope | When Called |
|--------|------|-------|-------------|
| **AssignArmy** | "Army Allocator" | Kingdom-wide | During `ThinkThreat` loop, assigns armies to threats |
| **ThinkArmy** | "Strategic General" | Per-army | `ThinkMilitary` → `ThinkArmies` loop, once per army |
| **ThinkFight** | "Field Commander" | Per-army, local | Called by `ThinkArmy` when army is in position |

### Call Hierarchy

```
ThinkMilitary()
  └─> ThinkThreat() [for each threat]
      └─> AssignArmy() [loops until threat has enough strength]
  └─> ThinkArmies() [coroutine]
      └─> ThinkArmy(army) [for each army]
          └─> ThinkFight(army) [if army in position]
```

### Decision Flow Summary

1. **AssignArmy** answers: *"Which armies should respond to this threat?"*
2. **ThinkArmy** answers: *"What should this specific army do right now?"*
3. **ThinkFight** answers: *"Should this army engage, and what should it attack?"*

---

## 2. AssignArmy - The Army Allocator

**Purpose**: Assigns specific armies to specific threats across the entire kingdom.

**When Called**: During `ThinkThreat()` processing, loops until the threat has sufficient assigned strength.

### Logic
- Evaluates available armies based on proximity and strength
- Assigns armies to threats until `threat.assigned.eval` meets the required threshold
- Prioritizes closer armies to reduce travel time
- Considers army availability (not already assigned, not in battle)

---

## 3. ThinkArmy - The Strategic General

**Purpose**: Manages the high-level state of an individual army: logistics, movement between realms, retreating from hopeless wars, and deciding when to engage.

**When Called**: During `ThinkArmies()` coroutine, once per army per cycle.

### Logic
- Movement to assigned threat realm (`army.tgt_realm`)
- Resupply decisions when low on manpower
- Retreat from superior forces
- Siege assault timing
- Calls `ThinkFight()` when army reaches destination and is in position

---

## 4. ThinkFight - The Field Commander

**Purpose**: Tactical combat decisions once the army is in position. Scans the local province to decide what specifically to attack.

**When Called**: By `ThinkArmy()` when the army has arrived at its destination realm.

### Logic
- Evaluates all potential targets in the current realm
- Selects attack target priority: enemy army > castle > village
- Considers local strength balance before engaging
- May retreat if odds are unfavorable

---

## 5. Threat Levels (`KingdomAI.Threat.Level`)

The AI calculates a `Threat` object for **every realm** (province) it owns. The `level` enum determines the severity.

| Level | Value | Description |
|:------|:------|:------------|
| `Safe` | 0 | No enemies, peaceful borders |
| `Border` | 1 | Neighboring a neutral kingdom |
| `Neighbors` | 2 | Neighboring an **Enemy** kingdom |
| `Attack` | 3 | **NOT OWNED** - Realm is a target for conquest |
| `Invaded` | 4 | **Enemy Army Present** inside the realm |
| `Siege` | 5 | **Castle Under Siege** - Highest priority |

---

## 6. Threat Object Structure

The `Threat` class contains several `ArmiesEval` fields that break down forces in and around the realm.

### Key Fields

| Field | Description |
|:------|:------------|
| `enemies_in` | All **enemy** armies currently inside the realm |
| `ours_in` | Your **own** armies currently inside the realm |
| `friends_in` | Armies from **allied** kingdoms (excludes your own!) |
| `enemies_nearby` | Enemy armies in neighboring realms |
| `assigned` | Armies assigned to respond to this threat |
| `received_help` | Strength of allied armies sent to help |

**Critical Note**: `friends_in` does **NOT** include your own armies. To get total defending strength:
```csharp
float totalDefense = threat.ours_in.eval + threat.friends_in.eval + garrison_eval;
```

---

## 7. Strength Evaluation (`Eval`)

The `.eval` property on any `ArmiesEval` struct sums the strength of armies in that category.

### Calculation Logic
1. Iterates through every army in the category
2. Sums `army.EvalStrength()` for each
3. **Siege Exception**: Armies in siege battles may have `ooc_eval` = 0, but standard `eval` remains valid

### Strategic Usage (ThinkArmy)

**Defending**:
```csharp
float enemyStrength = threat.enemies_in.eval;
float myStrength = army.EvalTotalStrength();
float alreadyDefending = threat.ours_in.eval + threat.friends_in.eval;
```

**Siege Breaking**:
```csharp
if (threat.level == Threat.Level.Siege)
{
    var siegeBattle = realm.castle.battle;
    // Compare strength before engaging
}
```

### Tactical Usage (ThinkFight)

**Win Chance Calculation**:
```csharp
float totalFriendly = ownStrength + friendStrength;
float winChance = totalFriendly / (totalFriendly + enemyStrength);
```

**Reinforcement Decision**:
```csharp
float winChanceBefore = friendlyStr / (friendlyStr + enemyStr);
float winChanceAfter = (friendlyStr + myStr) / (friendlyStr + myStr + enemyStr);

// Join if it improves the outcome
```

---

## 8. Integration Points

### How the Three Tiers Communicate

1. **AssignArmy → ThinkArmy**: Sets `army.tgt_realm` to the threat's realm
2. **ThinkArmy → ThinkFight**: Calls `ThinkFight()` when army reaches destination

### Key Army Properties

| Property | Description |
|:---------|:------------|
| `army.tgt_realm` | Target realm assigned by AssignArmy |
| `army.realm_in` | Current realm the army is physically in |
| `army.battle` | Current battle the army is engaged in (null if none) |
| `army.castle` | Castle the army is garrisoned in (null if in field) |
| `army.ai_status` | Current AI state string |
| `army.movement` | Movement component for travel state |

### Army Status Values

The `army.ai_status` field tracks current state:
- `"idle"` - No current task
- Various statuses for movement, combat, resupply, etc.
