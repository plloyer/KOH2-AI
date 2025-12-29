# Enhanced Performance Logging Guide

## Overview

The AI Overhaul mod now includes a statistically rigorous performance logging system to compare Enhanced AI vs Baseline AI kingdoms. This system addresses common pitfalls in AI testing by:

- **Tracking initial conditions** to normalize results
- **Calculating growth rates** instead of absolute values
- **Recording comprehensive metrics** including economic, military, and diplomatic success
- **Providing aggregate statistics** for easy comparison
- **Increasing sample size** to 30% for statistical validity

## Log Files

Three CSV files are generated in `BepInEx/config/`:

### 1. `AI_Baseline_Initial.csv`
Records the **starting conditions** of each tracked kingdom when they're first selected.

**Key Fields:**
- Initial realms, gold, armies, strength
- Geographic factors: neighbor count, neighbor average strength, island status
- Religion type
- Game year when recorded

**Purpose:** Allows you to normalize results and control for starting advantages.

### 2. `AI_Performance_Enhanced.csv`
Main performance log with **detailed metrics** recorded periodically during gameplay.

**Current State Metrics:**
- `RealmsCount`, `Gold`, `ArmiesCount`, `TotalStrength`
- `WarsCount`, `TraditionsCount`, `BooksCount`
- `VassalsCount`, `AlliesCount`

**Growth Rate Metrics** (per year):
- `RealmsGrowthRate` - How many realms gained/lost per year
- `GoldGrowthRate` - Gold change per year
- `StrengthGrowthRate` - Military strength growth per year
- `TraditionsGrowthRate`, `BooksGrowthRate`

**Normalized Metrics** (relative to starting position):
- `RealmsRatio` - Current realms / Initial realms (e.g., 2.0 = doubled territory)
- `StrengthRatio` - Current strength / Initial strength
- `GoldPerRealm` - Economic efficiency
- `StrengthPerRealm` - Military efficiency

**Survival Tracking:**
- `IsDefeated` - Whether kingdom was eliminated
- `SurvivalYears` - How many years survived before defeat

### 3. `AI_Aggregate_Stats.csv`
**Summary statistics** comparing Enhanced vs Baseline groups as a whole.

**Recorded every 50 AI cycles** to show trends over time:
- Average realms, strength, gold, books for each group
- **Ratios** showing Enhanced performance relative to Baseline
  - `RealmsRatio` > 1.0 = Enhanced has more territory
  - `StrengthRatio` > 1.0 = Enhanced is stronger militarily
- Defeat counts and survival rates
- Active kingdom counts

**Also logged to console** for real-time monitoring during gameplay.

## Interpreting Results

### What Makes a Valid Conclusion?

✅ **Look for consistent patterns across multiple metrics:**
- If Enhanced AI has higher realms, strength, AND books
- If growth rates are consistently faster
- If survival rates are better

✅ **Check normalized metrics, not just absolute values:**
- `RealmsRatio` of 1.5 means Enhanced grew 50% more than their starting size
- Compare this to Baseline's ratio to see relative performance

✅ **Consider statistical significance:**
- 30% sample size gives ~6-9 kingdoms per group (typical game)
- Larger differences (>20%) more likely to be meaningful
- Multiple games strengthen conclusions

❌ **Don't conclude based on:**
- Single snapshot in time
- Absolute values without checking starting conditions
- Small sample sizes (1-2 kingdoms)
- Early game data (< 50 years elapsed)

### Example Analysis

**AI_Aggregate_Stats.csv excerpt:**
```
Timestamp,GameYear,EnhancedCount,BaselineCount,EnhancedAvgRealms,BaselineAvgRealms,RealmsRatio,...
2025-12-28 14:30:00,150.5,7,6,12.3,8.5,1.45,...
```

**Interpretation:**
- Year 150 of game
- Enhanced AI: 7 kingdoms averaging 12.3 realms
- Baseline AI: 6 kingdoms averaging 8.5 realms
- **Enhanced has 45% more territory** on average
- Need to check initial conditions and growth rates to confirm this is due to AI quality

**AI_Performance_Enhanced.csv for one kingdom:**
```
...,RealmsCount,InitialRealms,RealmsGrowthRate,RealmsRatio,...
...,15,5,0.12,3.0,...
```

**Interpretation:**
- Started with 5 realms, now has 15 realms
- **Tripled in size** (RealmsRatio = 3.0)
- Growing at 0.12 realms/year
- Strong performance indicator

### Key Metrics to Compare

**Territorial Success:**
- `RealmsRatio` (Enhanced avg vs Baseline avg)
- `RealmsGrowthRate` (faster expansion = better)

**Military Power:**
- `StrengthRatio` (relative to starting strength)
- `StrengthGrowthRate`
- `StrengthPerRealm` (military efficiency)

**Economic Success:**
- `GoldPerRealm` (economic efficiency)
- `GoldGrowthRate`

**Cultural/Diplomatic:**
- `BooksCount` (technology advancement)
- `TraditionsCount`
- `AlliesCount`

**Survival:**
- `SurvivalRate` (%) in aggregate stats
- `SurvivalYears` for defeated kingdoms

## Statistical Validity Improvements

### Compared to Original System:

| Aspect | Original | Enhanced | Improvement |
|--------|----------|----------|-------------|
| Sample Size | 10% (~2-3 kingdoms) | 30% (~6-9 kingdoms) | ⬆️ 3x more data points |
| Starting Conditions | Not tracked | Full baseline | ✅ Can normalize results |
| Metrics | 7 absolute values | 20+ including rates & ratios | ⬆️ Better success indicators |
| Survival Tracking | Defeat flag only | Survival time recorded | ✅ Time-to-failure analysis |
| Aggregate Stats | None | Periodic summaries | ✅ Easy comparison |
| Growth Rates | None | Per-year calculations | ✅ Shows velocity, not just position |

### Remaining Limitations

⚠️ **Still uncontrolled:**
- Geographic randomness (island vs landlocked, resource quality)
- Random events (crusades, great people, rebellions)
- Neighbor strength variance

⚠️ **For strongest conclusions:**
- Run multiple games and average results
- Look for patterns across different maps/scenarios
- Combine quantitative data with qualitative observation

## Console Monitoring

During gameplay, you'll see periodic summary logs:

```
[AI-Stats] Year 100: Enhanced vs Baseline |
  Realms: 10.5 vs 7.2 (146%) |
  Strength: 8500 vs 6200 (137%) |
  Survival: 86% vs 71%
```

This provides **real-time feedback** without opening CSV files.

## Recommendations

1. **Play at least 3 full games** to 200+ years
2. **Compare aggregate stats** across games
3. **Look for patterns** in multiple metrics
4. **Check if Enhanced maintains lead over time** (not just early game luck)
5. **Use normalized metrics** (ratios, growth rates) for fairest comparison

## Technical Notes

- Logging anchored to Kingdom ID 1's AI cycle to avoid duplicates
- Aggregate stats logged every 50 cycles (~every few minutes of gameplay)
- Both old and new logging systems active for compatibility
- Baselines recorded once per game session when kingdoms selected
- Growth rates calculated from time elapsed since baseline recording

---

**For questions or suggestions about the logging system, please open an issue on GitHub.**
