# AI Army Management Guide

This guide documents the vanilla Knights of Honor II three-tier military AI architecture: **AssignArmy**, **ThinkArmy**, and **ThinkFight**.

---

## 1. Overview: Three-Tier Military Architecture

The AI's military decision-making follows a hierarchical structure:

| Method | Role | Scope | When Called |
|--------|------|-------|-------------|
| **AssignArmy** | "Army Allocator" | Kingdom-wide | During `ThinkThreat` loop, assigns armies to threats |
| **ThinkArmy** | "Strategic General" | Per-army | `ThinkMilitary` → `ThinkArmies` loop, once per army |
| **ThinkFight** | "Field Commander" | Per-army, local | Called by `ThinkArmy` when army is in position |

### Call Hierarchy

```
ThinkMilitary()                              // Main military AI entry point (coroutine)
│
├─> CalcBudget()                             // Calculate available gold budget
├─> ClearExpenses(military_expenses)         // Reset expense tracking
├─> ClearExpenses(urgent_expenses)
│
├─> CalcThreat()                             // [Coroutine] Evaluate all threats
│   ├─> For each realm: realm.threat.Recalc(kingdom)
│   ├─> For each army: Restore army.tgt_realm assignments
│   └─> Sort threats by priority (highest first)
│
├─> ThinkThreats()                           // [Coroutine] Assign armies to threats
│   ├─> Pass 0: Assign armies to meet min_needed
│   │   └─> For each threat (sorted by priority):
│   │       └─> ThinkThreat(threat, pass=0)
│   │           └─> While assigned.eval < min_needed:
│   │               └─> AssignArmy(threat, pass)    // Find closest available army
│   │                   ├─> CanAssign(army, threat) // Check if army can be assigned
│   │                   ├─> Select closest valid army
│   │                   ├─> Remove army from old threat
│   │                   ├─> Set army.tgt_realm = threat.realm
│   │                   └─> threat.assigned.Add(army)
│   │
│   ├─> Pass 1: Assign armies to meet max_needed
│   │   └─> (Same as Pass 0 but with max_needed threshold)
│   │
│   └─> Cleanup: Unassign armies from threats with no need
│
├─> ThinkHireUnits()                         // [Coroutine] Unit recruitment
│   └─> For each realm with castle (not in battle):
│       ├─> ConsiderTakeGarrison(army)       // Take garrison units into army
│       ├─> ConsiderHireArmy(army)           // Hire new units for army
│       ├─> ConsiderHireEquipment(army)      // Equip army with gear
│       ├─> ConsiderHealArmy(army)           // Heal damaged army
│       ├─> ConsiderHealUnits(army)          // Heal individual units
│       ├─> ConsiderHireGarrison(castle)     // Hire garrison troops
│       └─> ConsiderUpgradeFortifications(castle)
│
├─> ThinkArmies()                            // [Coroutine] Per-army decisions
│   └─> For each army:
│       └─> ThinkArmy(army)                  // See Section 3 below
│
└─> SpendExpenses()                          // Execute queued military expenses
    ├─> If urgent_expenses exist: SpendExpenses(urgent_expenses)
    └─> Else: SpendExpenses(military_expenses)
```

### ThinkArmy Detailed Flow

```
ThinkArmy(army)                              // Per-army strategic decisions
│
├─> [If army in battle]:
│   ├─> ThinkRetreat(army)                   // Consider retreating
│   │   ├─> Check CanLeaveBattle()
│   │   ├─> Calculate win estimation
│   │   ├─> Check lost units threshold
│   │   └─> If conditions met: battle.DoAction("retreat")
│   │
│   ├─> ThinkBreakSiege(army)                // [If defender in siege]
│   │   ├─> Check food levels
│   │   ├─> Check strength estimation
│   │   └─> If desperate: battle.BreakSiege()
│   │
│   └─> ThinkAssaultSiege(army)              // [If attacker in siege]
│       └─> If win estimation <= 20%: battle.Assault()
│
├─> [If army NOT in battle]:
│   ├─> [If fleeing]: Return (let army flee)
│   │
│   ├─> [If has tgt_realm assignment]:
│   │   ├─> ShouldWait(army)                 // Wait for other armies?
│   │   │   └─> If waiting: Stop army, set "wait_others"
│   │   ├─> If not at target: Send(army, tgt_realm.castle)
│   │   └─> If not in target realm yet: Return (still traveling)
│   │
│   ├─> [If not low on units]:
│   │   └─> ThinkFight(army)                 // See Section 4 below
│   │       └─> If engaged: Return
│   │
│   ├─> [If in own realm, idle]:
│   │   └─> Send(army, realm.castle, "defend")
│   │
│   ├─> [If in castle without supplies]:
│   │   └─> castle.ResupplyArmy(army)
│   │
│   ├─> ConsiderHireMercenaries(army)
│   │
│   ├─> [If needs resupply/refill]:
│   │   ├─> DecideOwnCastleForArmy(army)     // Find best castle
│   │   └─> Send(army, castle, "resupply" or "refill")
│   │
│   └─> ThinkHelpWithRebels(army)            // Help allies with rebels
```

### ThinkFight Detailed Flow

```
ThinkFight(army)                             // Tactical combat decisions
│
├─> [Pre-checks]:
│   ├─> If no realm_in or no castle: Return false
│   └─> TooSoonRetreat(army): Return false if recently retreated
│
├─> [Scan realm for forces]:
│   ├─> Calculate own strength (num2), enemy strength (num)
│   ├─> Find ongoing battles we can join
│   ├─> Find closest attackable enemy army (army2)
│   └─> Calculate strength ratios
│
├─> [If enemies too strong (1.5x)]:
│   ├─> If in own realm + castle available:
│   │   └─> Send(army, castle, "enemies_too_strong")
│   ├─> If ally in battle:
│   │   └─> Send(army, battle, "reinforce_desperate")
│   └─> If in enemy territory:
│       └─> Stop and set "wait_for_battle"
│
├─> [Priority 1: Join existing battle]:
│   └─> Send(army, battle, "reinforce")
│
├─> [Priority 2: Attack enemy army]:
│   └─> Send(army, enemy_army, "attack_army")
│
├─> [Priority 3: Attack castle]:
│   ├─> Check if can siege (Battle.CanSiege)
│   ├─> Check strength vs garrison
│   └─> Send(army, castle, "attack_castle")
│
└─> [Priority 4: Plunder settlements]:
    └─> ThinkPlunder(army)
        └─> Find closest unrazed enemy settlement
        └─> Send(army, settlement, "plunder")
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
- Iterates all kingdom armies looking for assignable ones
- Calls `CanAssign(army, threat, pass)` to validate each army
- Selects the **closest** valid army (by `SqrDist` to threat realm castle)
- Removes the army from any previous threat assignment
- Sets `army.tgt_realm = threat.realm`
- Adds army to `threat.assigned`
- Returns `true` if an army was assigned, `false` if none available

### Two-Pass System
- **Pass 0**: Assigns armies until `threat.assigned.eval >= threat.min_needed`
- **Pass 1**: Assigns additional armies until `threat.assigned.eval >= threat.max_needed`

---

## 3. ThinkArmy - The Strategic General

**Purpose**: Manages the high-level state of an individual army: logistics, movement between realms, retreating from hopeless wars, and deciding when to engage.

**When Called**: During `ThinkArmies()` coroutine, once per army per cycle.

### State Checks
- `IsArmyInOwnRealm(army)` - Is the army in friendly territory?
- `IsFull(army)` - Does army have max units?
- `IsLow(army)` - Is army below 50% effective strength?
- `IsLowSupplies(army)` - Does army need resupply?
- `HasSupplies(army)` - Does army have supplies?

### In-Battle Logic
If `army.battle != null`:
1. **ThinkRetreat**: Consider retreating if losing badly
2. **ThinkBreakSiege**: If defending a siege, consider sallying out
3. **ThinkAssaultSiege**: If attacking a siege, consider assault

### Out-of-Battle Logic
If `army.battle == null`:
1. **Movement**: If has `tgt_realm`, move toward it
2. **Wait**: If `ShouldWait()` returns true, stop and wait for allies
3. **Fight**: If in position, call `ThinkFight(army)`
4. **Defend**: If in own realm and idle, garrison in castle
5. **Resupply**: If low supplies/units, find castle to resupply
6. **Rebels**: Consider helping allies fight rebels

---

## 4. ThinkFight - The Field Commander

**Purpose**: Tactical combat decisions once the army is in position. Scans the local province to decide what specifically to attack.

**When Called**: By `ThinkArmy()` when the army has arrived at its destination realm.

### Strength Calculation
Scans all armies in `army.realm_in`:
- `num` = Total enemy strength
- `num2` = Total own-kingdom strength
- `num3` = Enemy strength NOT in battle
- `num4` = Own strength NOT in battle

### Target Priority (highest to lowest)
1. **Join Ongoing Battle**: If allies fighting, reinforce them
2. **Attack Enemy Army**: Target closest non-fleeing enemy army
3. **Attack Castle**: If can siege and strong enough (1.5x garrison)
4. **Plunder Settlements**: Attack villages, farms, etc.

### Retreat Conditions
If enemy strength >= 1.5x own strength:
- In own realm: Retreat to castle ("enemies_too_strong")
- Ally in battle: Join anyway ("reinforce_desperate")
- In enemy territory: Stop and wait ("wait_for_battle")

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
| `min_needed` | Minimum strength needed to handle threat |
| `max_needed` | Maximum strength desired for threat |
| `garrison_eval` | Strength of castle garrison |

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
| `army.ai_thinks` | Counter incremented each ThinkArmy call |
| `army.last_retreat_time` | Timestamp of last retreat (for cooldown) |

### Army Status Values

The `army.ai_status` field tracks current state:
- `"idle"` - No current task
- `"wait_others"` - Waiting for other armies to arrive
- `"wait_for_battle"` - Waiting in enemy territory for opportunity
- `"defend_realm"` - Moving to defend own territory
- `"attack_realm"` - Moving to attack enemy territory
- `"defend"` - Garrisoning in castle
- `"resupply"` - Moving to resupply
- `"refill"` - Moving to refill units
- `"reinforce"` - Moving to reinforce a battle
- `"reinforce_desperate"` - Reinforcing despite bad odds
- `"attack_army"` - Attacking enemy army
- `"attack_desperate"` - Attacking despite bad odds
- `"attack_castle"` - Sieging a castle
- `"enemies_too_strong"` - Retreating to castle
- `"plunder"` - Plundering a settlement
- `"help_with_rebels"` - Helping allies fight rebels
- `"resupplied"` - Just finished resupplying

---

## 9. Helper Methods Reference

### Army State Checks
| Method | Description |
|:-------|:------------|
| `IsFull(army)` | `units.Count >= MaxUnits() + 1` |
| `IsLow(army)` | Effective strength < 50% of max |
| `IsLowSupplies(army)` | Needs supplies |
| `HasSupplies(army)` | Has supplies |
| `IsArmyInOwnRealm(army)` | In friendly territory |
| `TooSoonRetreat(army)` | Recently retreated (cooldown active) |

### Sending Armies
| Method | Description |
|:-------|:------------|
| `Send(army, target, status)` | Move army to target, set status |
| `DecideOwnCastleForArmy(leader)` | Find best castle for resupply |
| `ResolveTarget(target)` | Convert target to appropriate object |
