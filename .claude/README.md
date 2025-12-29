# Claude Configuration for AI Overhaul Project

This directory contains project-specific Claude Code configurations.

## Skills

### koh2-analytics

Expert analytics skill for Knights of Honor II AI Overhaul mod.

**Usage:**
```bash
/koh2-analytics
```

Or invoke it by saying:
- "Use the koh2-analytics skill"
- "Analyze my AI performance data"
- "Help me understand the CSV logs"

**What it does:**
- Becomes an expert in the AI Overhaul mod architecture
- Understands the logging system and CSV schema
- Can analyze performance data with statistical rigor
- Helps interpret results and identify patterns
- Provides data analysis code (Excel formulas, Python, R)
- Debugs data quality issues
- Suggests experimental design improvements

**When to use it:**
- After running a match, when you have CSV logs to analyze
- When designing new experiments or metrics
- When debugging logging issues
- When interpreting statistical significance of results
- When comparing multiple matches

**Example questions:**
- "Analyze the latest CSV logs and tell me if Enhanced AI is better"
- "What's the statistical significance of these results?"
- "Create a Python script to visualize the growth rates"
- "Why are some kingdoms showing NaN values?"
- "How should I design the next experiment?"

## Project Structure

This skill has comprehensive knowledge of:
- All source files (Plugin.cs, EconomyLogic.cs, WarDiplomacyLogic.cs, etc.)
- The logging system (EnhancedPerformanceLogger, KingdomBaseline)
- The experimental design (30% Enhanced, 70% Baseline)
- The game's Logic API from Sources/Logic/
- Common compilation and runtime issues
- Statistical analysis best practices

The skill is automatically available when working in this project directory.