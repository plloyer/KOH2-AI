# Claude Configuration for AI Overhaul Project

This directory contains project-specific Claude Code configurations.

## Skills

### koh2-feature

Feature implementation skill with rigorous API validation and quality assurance.

**Usage:**
```bash
/koh2-feature
```

Or invoke it by saying:
- "Use the koh2-feature skill"
- "Implement a feature for..."
- "Add functionality to..."

**What it does:**
- **Phase 1: API Discovery** - Reads Sources/Logic/*.cs to verify APIs before coding
- **Phase 2: Implementation** - Writes patch code using only verified APIs
- **Phase 3: Compilation** - Builds and fixes errors until 0 errors
- **Phase 4: Review** - Checks for correctness, quality, performance, integration
- **Phase 5: Commit** - Git commits with descriptive message

**Enforces strict rules:**
- ✅ NEVER hallucinate APIs - always verify against decompiled sources
- ✅ ALWAYS compile successfully before committing
- ✅ VERIFY APIs first, then code
- ✅ REVIEW and improve after implementation
- ✅ COMMIT with clear messages when done

**When to use it:**
- Implementing new AI behaviors or logic
- Adding new patches to existing systems
- Creating new features that interact with game objects
- Modifying economy, war, diplomacy, or military logic

**Example usage:**
- "Use koh2-feature skill to add a feature that prevents war when gold is low"
- "/koh2-feature - Make Enhanced AI prioritize defensive buildings when losing wars"
- "Implement naval invasion logic using the feature skill"

**What it knows:**
- Complete game API reference (Kingdom, Army, Castle, Realm, Character, etc.)
- Common pitfalls and correct patterns
- Where to add patches (EconomyLogic.cs, WarDiplomacyLogic.cs, etc.)
- Harmony patching techniques (Prefix, Postfix, Traverse)
- Null safety patterns and edge case handling

---

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