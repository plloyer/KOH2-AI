# AI Evaluation System (CRITICAL REFERENCE)

## CRITICAL RULE: Lower Eval = Higher Priority

The game's AI expense evaluation system is **counter-intuitive**:

- **Lower eval score = Higher priority**
- **Higher eval score = Lower priority**
- **eval >= 30 = Rejected (MAX_EVAL threshold)**

## What is "eval"?

The `eval` score represents **"turns to wait before affordable"** or **"affordability delay"**:

- `eval = 0` → Can afford immediately → **HIGHEST PRIORITY**
- `eval = 5` → Need to wait 5 turns → Medium priority
- `eval = 10` → Need to wait 10 turns → Lower priority
- `eval = 30` → **BLOCKED/REJECTED** (expense is discarded)

## How eval is Calculated

From `Sources/Logic/KingdomAI.cs:231-275`:

```csharp
public float EvaluateCost()
{
    // For each resource (Gold, Books, etc.)
    float required = kingdom_cost[resourceType];
    float available = kingdom.resources[resourceType];

    if (available >= required)
        continue; // Can afford now, eval = 0

    float income_per_turn = kingdom.income[resourceType] - kingdom.expenses[resourceType];

    if (income_per_turn <= 0)
        return 30f; // Can never afford, BLOCKED

    int turns_to_wait = (int)((required - available) / income_per_turn);

    return turns_to_wait; // Higher wait time = lower priority
}
```

## How to INCREASE Priority (Lower eval)

### ✅ CORRECT Methods:

1. **Divide by a value > 1.0:**
   ```csharp
   option.eval /= GameBalance.SwordsmithBoost;  // 30.0 → eval /= 30 → very high priority
   ```

2. **Multiply by a value < 1.0:**
   ```csharp
   option.eval *= 0.5f;  // Cut eval in half → higher priority
   option.eval *= GameBalance.StrongBoostMultiplier; // 0.5f
   ```

3. **Set to a low absolute value:**
   ```csharp
   option.eval = 1.0f;  // Very high priority (affordable in 1 turn)
   option.eval = 0.0f;  // Immediate (can afford now)
   ```

### ❌ WRONG Methods (Common Mistakes):

1. **Multiplying by values > 1.0 (DECREASES priority):**
   ```csharp
   option.eval *= 30f;  // WRONG! Makes priority much LOWER
   option.eval *= GameBalance.SwordsmithBoost; // WRONG if SwordsmithBoost = 30f
   ```

2. **Dividing by values < 1.0 (DECREASES priority):**
   ```csharp
   option.eval /= 0.5f;  // WRONG! Doubles eval → lower priority
   ```

## How to DECREASE Priority (Higher eval)

### ✅ CORRECT Methods:

1. **Multiply by a value > 1.0:**
   ```csharp
   option.eval *= GameBalance.StrongPenaltyMultiplier; // 10.0f → much lower priority
   ```

2. **Divide by a value < 1.0:**
   ```csharp
   option.eval /= 0.5f;  // Doubles eval → lower priority
   ```

3. **Set to MAX_EVAL to block:**
   ```csharp
   option.eval = 30f;  // Block/reject this expense entirely
   option.eval *= GameBalance.StrictBlockMultiplier; // 100f (exceeds MAX_EVAL)
   ```

## GameBalance Constants Reference

### Building Priority Multipliers (Use with MULTIPLICATION to increase priority)

```csharp
// These are MULTIPLIERS - multiply eval by these to increase priority
public const float SwordsmithPriorityMultiplier = 1/30f;    // eval *= 0.0333 (1/30th) → very high priority
public const float FletcherPriorityMultiplier = 0.01f;      // eval *= 0.01 (1% of original) → extremely high priority
public const float BarracksPriorityMultiplier = 0.01f;      // eval *= 0.01 (1% of original) → very high priority
```

### True Multipliers (values < 1.0 increase priority when multiplied)

```csharp
// These are true multipliers - multiply eval by these to increase priority
public const float StrongBoostMultiplier = 0.5f;   // eval *= 0.5 → cut in half
public const float MediumBoostMultiplier = 0.67f;  // eval *= 0.67 → reduce by 33%
public const float WeakBoostMultiplier = 0.77f;    // eval *= 0.77 → reduce by 23%
public const float HighPriorityMultiplier = 0.7f;  // eval *= 0.7 → reduce by 30%
public const float UrgentPriorityMultiplier = 0.01f; // eval *= 0.01 → extremely high priority
```

### Penalty Multipliers (values > 1.0 decrease priority when multiplied)

```csharp
// These are penalty multipliers - multiply eval by these to decrease priority
public const float StrongPenaltyMultiplier = 10.0f;  // eval *= 10 → much lower priority
public const float MediumPenaltyMultiplier = 5.0f;   // eval *= 5 → lower priority
public const float StrictBlockMultiplier = 100.0f;   // eval *= 100 → blocked (exceeds MAX_EVAL)
```

## Usage Examples

### Example 1: Prioritize Swordsmith

```csharp
// CORRECT ✅
option.eval *= GameBalance.SwordsmithPriorityMultiplier;  // Multiply by 1/30 → very low eval → high priority

// WRONG ❌
option.eval /= GameBalance.SwordsmithPriorityMultiplier;  // Divide by 1/30 → very high eval → low priority
```

### Example 2: Block Fletcher until Swordsmith

```csharp
// CORRECT ✅
option.eval *= GameBalance.StrongPenaltyMultiplier;  // Multiply by 10 → high eval → low priority

// WRONG ❌
option.eval *= GameBalance.StrongBoostMultiplier;  // Multiply by 0.5 → low eval → HIGH priority (opposite of intent!)
```

### Example 3: Urgent Merchant Hiring

```csharp
// CORRECT ✅
expense.eval *= GameBalance.UrgentPriorityMultiplier;  // Multiply by 0.01 → extremely low eval → urgent priority

// WRONG ❌
expense.eval /= GameBalance.UrgentPriorityMultiplier;  // Divide by 0.01 → extremely high eval → blocked!
```

### Example 4: Religion Building Boost

```csharp
// CORRECT ✅
int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
float divisor = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
option.eval /= divisor;  // More slots → higher divisor → lower eval → higher priority

// WRONG ❌
float boost = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
option.eval *= boost;  // More slots → higher eval → LOWER priority (opposite!)
```

## Debugging eval Scores

The DebugOverlay shows eval scores in the "Considered Expenses" list:

```
[0.0] HireArmyUnit: Swordsmen (Military)        ← Immediate, highest priority
[5.2] BuildStructure: Market (Economy)          ← Wait 5 turns, medium priority
[10.3] HireChacacter: Merchant (Economy)        ← Wait 10 turns, lower priority
[30.0] HireArmyEquipment: FoodWagon (Military)  ← BLOCKED (upkeep budget exceeded)
```

**Key Insight:** Sort by score ascending (lowest first) to see what the AI will actually do.

## Expense Rejection Criteria

From `Sources/Logic/KingdomAI.cs:2204`:

```csharp
private void ConsiderExpense(Expense expense)
{
    if (expense.eval >= 30f || ...)  // MAX_EVAL check
        return;  // Rejected immediately, never added to queue

    // ... rest of logic to queue expense
}
```

An expense is rejected if:
1. **eval >= 30** → Cannot afford or upkeep budget exceeded
2. **!CanAfford()** → Insufficient resources right now
3. **!Validate()** → Prerequisites not met (e.g., no free court slot)
4. **Category weight <= 0** → Category spending disabled

## Upkeep Budget System

Equipment and units have ongoing upkeep costs. The AI has strict budget limits:

```csharp
void CheckUpkeepBudget()
{
    if (upkeep_gold > 0 && !CheckUpkeep(upkeep_gold, category, subcategory))
        eval = 30f;  // BLOCKED if upkeep exceeds budget
}

bool CheckUpkeep(float upkeep, CategoryData.UpkeepData ud)
{
    float income = kingdom.income.Get(ResourceType.Gold);
    float budget = ud.budget * income / 100f;  // Budget as % of income

    if (ud.upkeep + upkeep > budget)
        return false;  // Would exceed budget

    return true;
}
```

**Important:** Even with `eval = 0` (can afford now), an expense will be blocked if it would push upkeep over budget.

## Priority Enum (Separate from eval)

The `Priority` enum is a separate flag:

```csharp
public enum Priority
{
    Low = 1,
    Normal = 10,
    High = 1000,
    Urgent = 1000000
}
```

**Key Difference:**
- `Priority.Urgent` expenses **skip the upkeep budget check** (line 118: `if (eval < 30f && priority != Priority.Urgent)`)
- `Priority` affects which queue the expense goes into (urgent vs regular)
- `Priority` does NOT directly affect eval score

## Best Practices

1. **Always add comments:** Clarify whether you're increasing or decreasing priority
   ```csharp
   option.eval /= GameBalance.SwordsmithBoost;  // Lower eval = higher priority
   ```

2. **Use named constants:** Never hardcode multipliers
   ```csharp
   // GOOD ✅
   option.eval /= GameBalance.FletcherBoost;

   // BAD ❌
   option.eval /= 100f;  // Magic number, unclear intent
   ```

3. **Test your changes:** Use the Debug Overlay (F9) to verify eval scores are correct
   - Lower scores should appear for high-priority items
   - Check that items aren't accidentally blocked (eval = 30)

4. **Document in AI_ENHANCEMENTS.md:** When changing priorities, update the documentation

## Common Bugs to Avoid

### Bug #1: Inverted Boost
```csharp
// WRONG ❌
option.eval *= GameBalance.SwordsmithBoost;  // Intended to boost, actually penalties!

// CORRECT ✅
option.eval /= GameBalance.SwordsmithBoost;  // Actually boosts priority
```

### Bug #2: Inverted Penalty
```csharp
// WRONG ❌
option.eval *= GameBalance.StrongBoostMultiplier;  // Intended to penalize, actually boosts!

// CORRECT ✅
option.eval *= GameBalance.StrongPenaltyMultiplier;  // Actually penalizes
```

### Bug #3: Forgetting eval >= 30 Rejection
```csharp
// This gets BLOCKED, not just deprioritized
option.eval = 50f;  // Exceeds MAX_EVAL = 30, expense is rejected
```

### Bug #4: Mixing up Priority and eval
```csharp
// Priority.Urgent doesn't change eval score!
// It only skips upkeep budget checks and goes to urgent queue
expense.priority = Priority.Urgent;  // Still need to lower eval for actual priority
expense.eval /= 100f;  // THIS is what increases priority
```

## Summary Cheat Sheet

| Goal | Method | Example |
|------|--------|---------|
| **Increase Priority** | Divide by value > 1.0 | `eval /= 30f` |
| **Increase Priority** | Multiply by value < 1.0 | `eval *= 0.5f` |
| **Decrease Priority** | Multiply by value > 1.0 | `eval *= 10f` |
| **Decrease Priority** | Divide by value < 1.0 | `eval /= 0.5f` |
| **Block Entirely** | Set to 30 or higher | `eval = 30f` |
| **Skip Upkeep Check** | Set Priority flag | `priority = Priority.Urgent` |

**Remember:** When in doubt, check the debug overlay - lower numbers = happens first!
