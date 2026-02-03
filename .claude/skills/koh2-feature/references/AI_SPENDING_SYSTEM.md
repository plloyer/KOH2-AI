# AI Spending System in Knights of Honor II

This document explains the AI's expense/spending system - how it decides what to purchase and when.

---

## The Dual "Eval" System (CRITICAL)

There is a critical naming collision in the codebase where the variable name `eval` is used for two completely opposite concepts.

### Quick Reference

| Feature | `BuildOption.eval` | `Expense.eval` |
| :--- | :--- | :--- |
| **Concept** | **Utility** ("Do I want this?") | **Difficulty** ("Can I afford this?") |
| **Location** | `Castle.cs` (Struct `BuildOption`) | `Expense.cs` (Class `Expense`) |
| **Scale** | **Higher is Better** (0 to ∞) | **Lower is Better** (0 to 30) |
| **Fail Condition** | `eval <= 0` (Ignored) | `eval >= 30` (Rejected - Too Expensive) |
| **Used In** | `Castle.ChooseBuildOption` | `KingdomAI.ConsiderExpense` |

### 1. The Selection Logic (`BuildOption.eval`)

**Context:** The AI is deciding *which* building it would like to build next.

*   **Logic:** Calculates how valuable a building is based on what it produces (Gold, Books, Piety) multiplied by the AI's current strategic weights.
*   **Formula:** `BaseProduction * StrategyWeights + Bonuses`
*   **Bonuses:** Large flat bonuses (e.g., +2500, +5000) are applied if the building completes a District or Power Fantasy set.
*   **Interaction:**
    *   The `Castle.ChooseBuildOption` method sums up the `eval` of all options.
    *   It rolls a random number between 0 and `TotalSum`.
    *   It iterates through the list; the higher the `eval`, the larger the "slice of the pie" that option has, and the more likely it is to be picked.
*   **Modding Tip:** To force the AI to pick a building, set this value to an absurdly high number (e.g., 1,000,000).

### 2. The Affordability Logic (`Expense.eval`)

**Context:** The AI has picked a desire (from step 1) and converted it into a proposed `Expense`. It now checks if it can actually pay for it without going bankrupt.

*   **Logic:** Calculates the economic burden of the cost relative to the kingdom's income.
*   **Formula:** `(Cost - StoredResources) / (NetIncome + Inflation)`
    *   Essentially: "How many turns of income will this cost me?"
*   **Thresholds:**
    *   **< 30**: Allowable.
    *   **>= 30**: **REJECTED**. The AI considers this "Impossible" or "Too burdensome".
*   **Interaction:**
    *   Even if `BuildOption.eval` was 1,000,000 (Desire is infinite), `Expense.eval` is recalculated based on the price tag.
    *   If the building costs 10,000 Gold and you only make 5 Gold/turn, `Expense.eval` will exceed 30, and the action will be cancelled despite the high desire.

### 3. The Execution Selection Logic (Weighting)

**Context:** The AI has verified it *can* afford a set of actions (from Step 2). Now it must decide which ones to actually execute *first* (if multiple threads like Military, General, etc. are competing).

*   **Logic:** Expenses are added to a `WeightedRandom` pool.
*   **Formula:** `Weight = (30 - Expense.eval) * Expense.Priority`
*   **Implication:**
    *   **Cheaper is Better**: Lower `Expense.eval` (easier to afford) = Higher Weight.
    *   **Priority Matters**: `Urgent` (1,000,000) makes the weight massive, guaranteeing it executes before `Normal` (10) actions.
    *   **Zero Weight**: If `Expense.eval >= 30`, weight is 0 (or negative), so it's never picked.

### The Eval Flow Summary

1.  **Selection Phase (`ThinkBuild`)**:
    *   Code calculates `BuildOption.eval` for every possible building.
    *   **CRITICAL:** If any option is `Urgent` priority, *all non-urgent options are ignored*.
    *   AI picks the winner based on weighted probability.
    *   Winner is stored in `next_build_expense`.

2.  **Affordability Phase (`ConsiderExpense`)**:
    *   The `next_build_expense` is converted into an actual `Expense` object.
    *   Code calculates `Expense.eval` (Burden).
    *   If `Expense.eval >= 30`, it is rejected immediately.

3.  **Execution Phase (`SpendExpenses`)**:
    *   Accepted expenses are added to a pool with `Weight = (30 - Expense.eval) * Priority`.
    *   The system picks one to execute.

> [!IMPORTANT]
> When debugging AI decisions, check the chain:
> 1.  **Filtering**: Did an `Urgent` priority action hide your action?
> 2.  **Selection**: Was `BuildOption.eval` high enough?
> 3.  **Affordability**: Was specific `Expense.eval` < 30?
> 4.  **Execution**: Did `Priority` weighting push it to the front?
> *   "Why didn't it pick X?" -> Check `BuildOption.eval`.
> *   "Why did it pick X but not build it?" -> Check `Expense.eval`.

---

## Expense Class Structure

The `Expense` class (`KingdomAI.Expense`) represents a proposed purchase.

### Expense.Type Enum

```csharp
public enum Type
{
    None,
    HireChacacter,        // Hire court member (note: typo in game code)
    HireArmyUnit,         // Recruit army unit
    HireGarrison,         // Recruit garrison unit
    BuildStructure,       // Build new building
    Upgrade,              // Upgrade existing building
    ExpandCity,           // Expand city district
    UpgradeFortifications,// Upgrade castle walls
    IncreaseCrownAuthority,
    AdoptTradition,       // Adopt kingdom tradition
    ExecuteAction,        // Execute character action
    ExecuteOpportunity,   // Execute opportunity action
    HireMercenaryArmy,    // Hire mercenary company
    HireArmyEquipment     // Purchase siege equipment
}
```

### Expense.Category Enum

```csharp
public enum Category
{
    None,
    Military,    // Army units, garrison, fortifications
    Economy,     // Buildings, traditions
    Diplomacy,   // Diplomatic actions
    Espionage,   // Spy actions
    Religion,    // Cleric actions
    Other,       // Crown authority, misc
    COUNT
}
```

### Expense.Priority Enum

```csharp
public enum Priority
{
    Low = 1,           // Can wait, low weight
    Normal = 10,       // Standard priority
    High = 1000,       // Important, weighted heavily
    Urgent = 1000000   // Must execute immediately
}
```

### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `kingdom` | `Kingdom` | Kingdom making the expense |
| `type` | `Type` | What action to perform |
| `category` | `Category` | Budget category |
| `priority` | `Priority` | Execution priority |
| `defParam` | `BaseObject` | Definition (Unit.Def, Building.Def, etc.) |
| `objectParam` | `Object` | Target (Castle, Army, etc.) |
| `cost` | `Resource` | Total cost |
| `kingdom_cost` | `Resource` | Kingdom-level cost portion |
| `eval` | `float` | Affordability score (0-30, lower = better) |
| `upkeep_gold` | `float` | Ongoing gold upkeep |

---

## Call Hierarchy Diagram

```
                          ┌─────────────────────────────┐
                          │      PHASE 1: ORIGIN        │
                          │   (Where desires come from) │
                          └─────────────┬───────────────┘
                                        │
        ┌───────────────────────────────┼───────────────────────────────┐
        │                               │                               │
        ▼                               ▼                               ▼
┌───────────────┐              ┌───────────────┐              ┌───────────────┐
│  ThinkBuild() │              │ThinkMilitary()│              │ThinkGeneral() │
│               │              │               │              │               │
│ Castle.Add-   │              │ ThinkThreats  │              │ ThinkHire-    │
│ BuildOptions()│              │ ThinkHireUnits│              │   Court       │
│ ChooseBuild-  │              │ ThinkArmies   │              │ ThinkActions  │
│   Option()    │              │               │              │ ThinkAdopt-   │
│               │              │               │              │   Tradition   │
│ Sets:         │              │ Calls:        │              │               │
│ next_build_   │              │ Consider-     │              │ Calls:        │
│   expense     │              │   Expense()   │              │ Consider-     │
│ next_upgrade_ │              │   directly    │              │   Expense()   │
│   expense     │              │               │              │   directly    │
└───────┬───────┘              └───────┬───────┘              └───────┬───────┘
        │                               │                               │
        └───────────────────────────────┼───────────────────────────────┘
                                        │
                          ┌─────────────▼───────────────┐
                          │  PHASE 2: CREATION/ROUTING  │
                          │    ConsiderExpense()        │
                          └─────────────┬───────────────┘
                                        │
                          ┌─────────────▼───────────────┐
                          │      Expense.Set()          │
                          │  - Sets all parameters      │
                          │  - Calls Evaluate()         │
                          │    └─► CalcCost()           │
                          │    └─► EvaluateCost()       │
                          │    └─► CalcUpkeep()         │
                          │    └─► CheckUpkeepBudget()  │
                          └─────────────┬───────────────┘
                                        │
                    ┌───────────────────┼───────────────────┐
                    │                   │                   │
                    ▼                   ▼                   ▼
         ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
         │  eval >= 30?     │  │ cost.IsZero()?   │  │ category.weight  │
         │  REJECTED        │  │ Spend immediately│  │ <= 0?            │
         │  (too expensive) │  │ (free action)    │  │ SKIP (budget 0)  │
         └────────┬─────────┘  └──────────────────┘  └────────┬─────────┘
                  │                                            │
                  │         ┌──────────────────────────────────┘
                  │         │
                  ▼         ▼
        ┌─────────────────────────────────┐
        │     PHASE 3: QUEUE ROUTING      │
        │                                 │
        │  Route based on CoopThread:     │
        │  - think_general_thread         │
        │      → general_expenses         │
        │  - think_build_thread           │
        │      → general_expenses         │
        │  - think_military_thread        │
        │      Urgent? → urgent_expenses  │
        │      Normal  → military_expenses│
        └─────────────┬───────────────────┘
                      │
                      ▼
        ┌─────────────────────────────────┐
        │     AddExpense(queue, expense)  │
        │                                 │
        │  weight = (30 - eval) * priority│
        │  queue.AddOption(expense, wt)   │
        └─────────────┬───────────────────┘
                      │
                      │
        ┌─────────────▼───────────────────┐
        │    PHASE 4: QUEUE SELECTION     │
        │       SpendExpenses(queue)      │
        └─────────────┬───────────────────┘
                      │
                      ▼
        ┌─────────────────────────────────┐
        │   WeightedRandom.Choose()       │
        │                                 │
        │  - Selects based on weight      │
        │  - Higher weight = more likely  │
        │  - Removes from queue           │
        └─────────────┬───────────────────┘
                      │
                      ▼
        ┌─────────────────────────────────┐
        │      expense.Validate()         │
        │                                 │
        │  - Check conditions still valid │
        │  - Court slot available?        │
        │  - Unit still purchasable?      │
        │  - Castle still owns army?      │
        │                                 │
        │  FAIL → Delete, try next        │
        └─────────────┬───────────────────┘
                      │
                      ▼
        ┌─────────────────────────────────┐
        │    PHASE 5: EXECUTION           │
        │      SpendExpense(expense)      │
        └─────────────┬───────────────────┘
                      │
                      ▼
        ┌─────────────────────────────────┐
        │       expense.Spend()           │
        │                                 │
        │  Switch on expense.type:        │
        │  - HireCharacter → HireChar()   │
        │  - BuildStructure → Build()     │
        │  - HireArmyUnit → BuyUnit()     │
        │  - AdoptTradition → Adopt()     │
        │  - etc.                         │
        │                                 │
        │  SUCCESS → Log, track budget    │
        └─────────────────────────────────┘
```

---

## Phase 1: Expense Origin

Expenses originate from three main AI thinking threads:

### ThinkBuild Thread

Runs periodically to decide what to build.

1. **Castle.AddBuildOptions()** - Each castle evaluates all possible buildings
2. **Castle.ChooseBuildOption()** - Weighted random selection based on `BuildOption.eval`
3. Results stored in `next_build_expense` and `next_upgrade_expense`

### ThinkMilitary Thread

Runs periodically to handle military spending.

- **ThinkThreats()** - Evaluate realm threats
- **ThinkHireUnits()** - Consider hiring army/garrison units
- **ThinkArmies()** - Army management, equipment, mercenaries

Direct `ConsiderExpense()` calls for:
- `HireArmyUnit` - Recruit army units
- `HireGarrison` - Recruit garrison units
- `HireArmyEquipment` - Buy siege equipment
- `HireMercenaryArmy` - Hire mercenary companies
- `UpgradeFortifications` - Upgrade castle walls

### ThinkGeneral Thread

Runs periodically for non-military expenses.

- **ConsiderHireCourt()** - Hire marshals, merchants, clerics
- **ConsiderAdoptTradition()** - Adopt kingdom traditions
- **ConsiderIncreaseCrownAuthority()** - Increase crown authority
- **ThinkActions()** - Execute character actions
- **ThinkOpportunities()** - Execute opportunity actions

Also processes `next_build_expense` and `next_upgrade_expense` from ThinkBuild.

---

## Phase 2: Creation & Routing

### ConsiderExpense() Methods

```csharp
// Primary overload - creates expense from parameters
private void ConsiderExpense(
    Expense.Type type,
    BaseObject defParam,
    Object objectParam,
    Expense.Category category = Category.None,
    Expense.Priority priority = Priority.Normal,
    List<Value> args = null)
{
    tmp_expense.Set(kingdom, type, category, priority, defParam, objectParam, args);
    ConsiderExpense(tmp_expense);
}

// Secondary overload - processes existing expense
private void ConsiderExpense(Expense expense)
{
    // Reject if type is None or too expensive
    if (expense.type == Type.None || expense.eval >= 30f)
        return;

    // Immediate execution for free actions
    if (expense.kingdom_cost.IsZero() && SpendExpense(expense))
        return;

    // Skip if category budget is zero (unless Urgent)
    if (expense.priority < Priority.Urgent && categories[expense.category].weight <= 0f)
        return;

    // Route to appropriate queue based on thread
    // ... (see queue routing below)
}
```

### Expense.Set() and Evaluate()

```csharp
public void Set(Kingdom kingdom, Type type, Category category, Priority priority,
                BaseObject defParam, Object objectParam, List<Value> args)
{
    // Set all parameters...
    Evaluate();  // Calculate cost and affordability
}

public void Evaluate()
{
    CalcCost(GetCost());      // Calculate total cost
    eval = EvaluateCost();    // Calculate affordability (0-30)
    CalcUpkeep();             // Calculate ongoing upkeep

    if (eval < 30f && priority != Priority.Urgent)
        CheckUpkeepBudget();  // May set eval=30 if over budget
}
```

### EvaluateCost() - Affordability Calculation

```csharp
public float EvaluateCost()
{
    float eval = 0f;

    foreach (ResourceType type in kingdom_resources)  // Gold, Books, Piety, Trade, Levy
    {
        float cost = kingdom_cost[type];
        if (cost <= 0f) continue;

        float stored = kingdom.resources[type];
        if (stored >= cost)
            continue;  // Can afford outright

        if (type == ResourceType.Trade)
            return 30f;  // Trade must be paid immediately

        // Calculate turns to save up
        float netIncome = kingdom.income[type] - kingdom.expenses[type];
        if (type == ResourceType.Gold)
            netIncome += kingdom.inflation;

        if (netIncome <= 0f)
            return 30f;  // No income = impossible

        int turnsNeeded = (int)((cost - stored) / netIncome);
        turnsNeeded &= -8;  // Round down to nearest 8

        if (turnsNeeded > eval)
            eval = turnsNeeded;
    }

    // Characters get 50% discount on eval
    if (type == Type.HireChacacter)
        eval *= 0.5f;

    return eval;
}
```

---

## Phase 3: Queue Management

### Three Expense Queues

| Queue | Source Thread | Contents |
|-------|---------------|----------|
| `general_expenses` | ThinkGeneral, ThinkBuild | Buildings, traditions, court, actions |
| `military_expenses` | ThinkMilitary | Normal military (units, garrison, equipment) |
| `urgent_expenses` | ThinkMilitary | Urgent military only |

### AddExpense() - Weight Calculation

```csharp
private void AddExpense(WeightedRandom<Expense> expenses, Expense expense)
{
    float weight = (30f - expense.eval) * (float)expense.priority;
    expenses.AddOption(expense, weight);
}
```

**Example weights:**

| eval | Priority | Weight | Likely? |
|------|----------|--------|---------|
| 0 | Normal (10) | 300 | Very likely |
| 15 | Normal (10) | 150 | Moderate |
| 29 | Normal (10) | 10 | Unlikely |
| 0 | Urgent (1M) | 30,000,000 | Guaranteed |
| 15 | High (1000) | 15,000 | Very likely |

---

## Phase 4: Selection

### SpendExpenses() Loop

```csharp
private IEnumerator SpendExpenses(WeightedRandom<Expense> expenses)
{
    while (true)
    {
        yield return null;

        Expense expense = expenses.Choose(null, del_option: true);
        if (expense == null)
            break;  // Queue empty

        // Track as next expense for category
        categories[expense.category].next_expense.Set(expense);

        // Validate still possible
        if (!expense.Validate())
        {
            expense.Delete();
            continue;  // Try next
        }

        // Attempt to spend
        if (!SpendExpense(expense))
        {
            expense.Delete();
            continue;  // Try next
        }

        // Success - update tracking
        // ...
    }
}
```

### Queue Processing Order

In **ThinkMilitary**:
```csharp
if (urgent_expenses.options.Count > 0)
    yield return SpendExpenses(urgent_expenses);   // Urgent first
else
    yield return SpendExpenses(military_expenses); // Normal military
```

In **ThinkGeneral**:
```csharp
ConsiderExpense(next_build_expense);   // Add building expense
ConsiderExpense(next_upgrade_expense); // Add upgrade expense
yield return SpendExpenses(general_expenses);
```

---

## Phase 5: Execution

### SpendExpense()

```csharp
private bool SpendExpense(Expense expense)
{
    Kingdom.in_AI_spend = true;
    bool success = expense.Spend();
    Kingdom.in_AI_spend = false;

    if (!success)
        return false;

    // Log and track
    LogSpentExpense(expense);
    categories[expense.category].spent.Add(expense.kingdom_cost, 1f);
    last_expense.Set(expense);

    // Track upkeep if applicable
    if (expense.upkeep_gold > 0f)
        AddUpkeep(expense.upkeep_gold, expense.category, expense.upkeep_subcategory);

    return true;
}
```

### Expense.Spend() - Type-Specific Execution

```csharp
public bool Spend()
{
    switch (type)
    {
        case Type.HireChacacter:
            // kingdom.HireCharacter(def.id)
            // If Marshal, also SpawnArmy at castle

        case Type.HireArmyUnit:
            // castle.BuyUnit(def, army)

        case Type.HireGarrison:
            // castle.BuyGarrisonUnit(def)

        case Type.BuildStructure:
            // castle.Build(def)

        case Type.Upgrade:
            // castle.Upgrade(def)

        case Type.ExpandCity:
            // castle.ExpandCity()

        case Type.AdoptTradition:
            // kingdom.AdoptTradition(def)

        // ... etc
    }
}
```

---

## Key Behaviors

### Immediate Spending for Free Actions

If an expense has zero `kingdom_cost`, it executes immediately in `ConsiderExpense()` without going through the queue:

```csharp
if (expense.kingdom_cost.IsZero() && SpendExpense(expense))
    return;  // Spent immediately, skip queue
```

### Validation Before Execution

`Validate()` re-checks conditions before spending:
- Court slots still available?
- Castle still owns army?
- Unit still purchasable?
- Resources still sufficient?

### Upkeep Budget Checking

Non-urgent expenses check upkeep budget:
```csharp
if (eval < 30f && priority != Priority.Urgent)
    CheckUpkeepBudget();  // May reject if over budget
```

### Priority Escalation

Threat level affects priority for military expenses:
- `Threat.Level.Invaded` → `Priority.Urgent`
- `Threat.Level.Neighbors` → `Priority.High`
- `Threat.Level.Safe` → `Priority.Low`

---

## Modding Considerations

When patching spending behavior:

1. **Patch `ConsiderExpense`** to filter/modify expense proposals
2. **Patch `AddExpense`** to adjust weights
3. **Patch `SpendExpense`** to log or intercept execution
4. **Patch `Expense.Evaluate`** to change affordability calculations

See `Patches/Spending/` for examples:
- `KingdomAI.ConsiderExpense.cs` - Filter character hiring
- `KingdomAI.AddExpense.cs` - Log queue additions
- `KingdomAI.SpendExpense.cs` - Log executions
- `KingdomAI.SpendExpenses.cs` - Log queue state
- `Castle.ChooseBuildOption.cs` - Log building selection
