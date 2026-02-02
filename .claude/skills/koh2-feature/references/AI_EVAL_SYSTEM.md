# The Dual "Eval" System in Knights of Honor II AI

There is a critical naming collision in the codebase where the variable name `eval` is used for two completely opposite concepts. This document clarifies the distinction.

## Quick Reference

| Feature | `BuildOption.eval` | `Expense.eval` |
| :--- | :--- | :--- |
| **Concept** | **Utility** ("Do I want this?") | **Difficulty** ("Can I afford this?") |
| **Location** | `Castle.cs` (Struct `BuildOption`) | `Expense.cs` (Class `Expense`) |
| **Scale** | **Higher is Better** (0 to ∞) | **Lower is Better** (0 to 30) |
| **Fail Condition** | `eval <= 0` (Ignored) | `eval >= 30` (Rejected - Too Expensive) |
| **Used In** | `Castle.ChooseBuildOption` | `KingdomAI.ConsiderExpense` |

---

## 1. The Selection Logic (`BuildOption.eval`)

**Context:** The AI is deciding *which* building it would like to build next.

*   **Logic:** Calculates how valuable a building is based on what it produces (Gold, Books, Piety) multiplied by the AI's current strategic weights.
*   **Formula:** `BaseProduction * StrategyWeights + Bonuses`
*   **Bonuses:** Large flat bonuses (e.g., +2500, +5000) are applied if the building completes a District or Power Fantasy set.
*   **Interaction:**
    *   The `Castle.ChooseBuildOption` method sums up the `eval` of all options.
    *   It rolls a random number between 0 and `TotalSum`.
    *   It iterates through the list; the higher the `eval`, the larger the "slice of the pie" that option has, and the more likely it is to be picked.
*   **Modding Tip:** To force the AI to pick a building, set this value to an absurdly high number (e.g., 1,000,000).

## 2. The Affordability Logic (`Expense.eval`)

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

## 3. The Execution Selection Logic (Weighting)

**Context:** The AI has verified it *can* afford a set of actions (from Step 2). Now it must decide which ones to actually execute *first* (if multiple threads like Military, General, etc. are competing).

*   **Logic:** Expenses are added to a `WeightedRandom` pool.
*   **Formula:** `Weight = (30 - Expense.eval) * Expense.Priority`
*   **Implication:**
    *   **Cheaper is Better**: Lower `Expense.eval` (easier to afford) = Higher Weight.
    *   **Priority Matters**: `Urgent` (1,000,000) makes the weight massive, guaranteeing it executes before `Normal` (10) actions.
    *   **Zero Weight**: If `Expense.eval >= 30`, weight is 0 (or negative), so it's never picked.

## The Flow

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
