# AI Army Management Guide (Threat & Eval)

This guide details the internal game logic for `KingdomAI.Threat` and `ArmiesEval`, crucial for implementing strategic (`ThinkArmy`) and tactical (`ThinkFight`) decisions.

## 1. Threat Levels (`KingdomAI.Threat.Level`)

The AI calculates a `Threat` object for **every realm** (province) it owns. The `level` enum determines the severity of the situation.

| Level | Value | Description |
| :--- | :--- | :--- |
| `Safe` | 0 | No enemies, peaceful borders. |
| `Border` | 1 | Neighboring a neutral kingdom (or potential future threat). |
| `Neighbors` | 2 | Neighboring an **Enemy** kingdom. |
| `Attack` | 3 | **NOT OWNED**. Realm is a target for conquest (foreign realm). |
| `Invaded` | 4 | **Enemy Army Present** inside the realm. |
| `Siege` | 5 | **Castle Under Siege**. Highest priority. |

## 2. Threat Object Structure

The `Threat` class contains several `ArmiesEval` fields that break down the forces in and around the realm.

### Key Fields for Decision Making

*   **`enemies_in`**: All **enemy** armies currently inside the realm.
    *   *Includes*: Armies from kingdoms you are at war with.
    *   *Eval*: Sum of `Army.EvalStrength()` for all these armies.
*   **`ours_in`**: Your **own** armies currently inside the realm.
*   **`friends_in`**: Armies belonging to **friendly** or **neutral** kingdoms (excluding yours).
    *   *Critical*: This does **NOT** include your own armies.
    *   *Usage*: To get total defending strength, calculate `ours_in.eval + friends_in.eval` (and optionally `garrison_eval`).
*   **`enemies_nearby`**: Enemy armies in neighboring realms (used for anticipating attacks).
*   **`received_help`**: (Calculated separately) Strength of friendly armies explicitly sent to help this realm.

## 3. Strength Evaluation (`Eval`)

The `.eval` property on any `ArmiesEval` struct sums up the strength of the armies in that list.

### Calculation Logic
1.  **Iterates** through every army in the specific category (e.g., all enemies in the realm).
2.  **Sums** `army.EvalStrength()` for each one.
3.  **Siege Exception**: If an army is in a siege battle, its `ooc_eval` (Out of Combat eval) might be 0, but its standard `eval` remains valid.

### Strategic Usage Tips (`ThinkArmy`)

*   **Defending**: Compare `enemyStrength` (threat.enemies_in.eval) vs `myStrength` (army.EvalTotalStrength()).
    *   Use `threat.ours_in.eval + threat.friends_in.eval` to see if the realm is *already* defended enough.
*   **Siege Breaking**:
    *   Check `threat.level == Threat.Level.Siege`.
    *   Access `realm.castle.battle` to find the specific siege battle instance.
    *   Only attack if `myStrength > enemyStrength * 1.2f` (buffer).

### Tactical Usage Tips (`ThinkFight`)

*   **Reinforcing**:
    *   Check `buddy.battle` or `realm.battle`.
    *   Calculate `winChance = friendlyStrength / (friendlyStrength + enemyStrength)`.
    *   Only join if `winChance` shifts from `< 0.45` (Losing) to `>= 0.45` (Winning).
