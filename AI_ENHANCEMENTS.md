# AI Enhancements

This document describes all the behavioral changes made to the enhanced AI.

## Economy

### Merchant Hiring
- **URGENT Priority**: If kingdom has 10+ available commerce (maxCommerce - usedCommerce) and has at least one merchant without an active trade route, hire a merchant immediately
- **Early Game**: First 2 merchants are guaranteed (bypass commerce checks)
- **Commerce Requirement**: For 3rd+ merchant, only hire if `(merchants + 1) * 10 <= maxCommerce`

### Crown Authority
- Block Crown Authority increase until kingdom has built Barracks with both Swordsmith and Fletcher upgrades
- Block Crown Authority increase if rushing tradition (see Tradition section)
- Block Crown Authority increase if any province can upgrade fortifications to level 1

### Trade Actions
- Trade agreements have high priority (lower eval = higher priority in AI expense system)
- Free diplomatic action but prioritized over other free actions

## Royal Family

### King Skill Priority
- **Goal**: Ensure "Writing Tradition" is accessible as early as possible.
- **Behavior**:
    - When the King learns a new skill, priority is strictly given to:
        1. **Writing** (LiteracySkill)
        2. **Learning** (LearningSkill)
    - Applies only if these skills are available options.

## Military

### Army Composition
- **First Two Armies**: Exactly 4 archers + 4 swordsmen each
- **Subsequent Armies**: 80% ranged-to-melee ratio (roughly 3.5 ranged : 4.5 melee per 8-unit army)

### Army Healing
- **In Own Territory**: Camp if any unit has any damage
- **In Enemy Territory**: Retreat and camp if army health < 70%

### Fortifications
- After first two armies are ready, upgrading fortifications becomes URGENT priority for all levels (not just level 1)

### Buddy System (Army Coordination)
- **Distance Limit**: Armies will only pair up if they are within 300 units (Support Range).
- **Break Distance**: Link is broken if buddies drift apart by > 600 units.
- **Leadership**: The army with the higher ID is the designated Leader. The lower ID army is the Follower.
- **Wait Logic**: Armies will not wait indefinitely for a buddy. They will only wait if the buddy is:
  - Not in another battle
  - Within 200 units distance

## Buildings & Upgrades

### Barracks
- **First Barracks in Kingdom**: Allow in any province, but boost priority heavily for provinces with Castle district
  - Boost scales with Castle district building slots: `1.0 + (slots * 0.25)`
- **Subsequent Barracks**: Only allow in provinces with Castle district OR IronOre feature (strictly enforce)

### Swordsmith
- **Very High Priority** if kingdom doesn't have Swordsmith upgrade yet
- Always boost Swordsmith evaluation significantly
- **Fletcher Blocking**: Block Fletcher upgrade until Swordsmith is built

### Fletcher
- **Very High Priority** if kingdom has Swordsmith but no Fletcher yet
- Must have Swordsmith before Fletcher is allowed
- **Direct Injection**: If Fletcher doesn't appear in upgrade options naturally, it is forcibly injected into the upgrade list with very high priority (eval: 1000 / GameBalance.FletcherBoost = ~10)
- This ensures Fletcher is built immediately after Swordsmith, even if the game's internal logic doesn't include it in available upgrades

### Religion Buildings
- **Strict Requirement**: Religious buildings can ONLY be built in provinces with Religion district
- **Priority Boost**: In provinces with Religion district, boost religious building priority based on district slots: `1.0 + (slots * 0.2)`
- Religious buildings include: Church, Masjid, Temple, Cathedral, GreatMosque

### Construction Blocking
- **Tradition Rush**: Block ALL construction when saving gold for first tradition (400+ books, Writing/Learning available)

## Court & Characters

### Character Hiring

**Target Composition Goal** (including King):
- 4 Marshals
- 1 Cleric
- 1 Diplomat OR 1 Spy (not both)
- 3 Merchants

Note: When king dies, composition changes and AI must adapt to maintain target ratios.

**Current Hiring Gates**:
- **Diplomat Hiring**: Only hire if:
  - 2+ stronger neighboring kingdoms
  - Gold income > 150/turn
- **Spy Hiring**: Only if gold income > 500/turn
- **Cleric Hiring**: Only if gold income > 50/turn
- **Merchant Hiring**: See Economy section

### Governor Assignment
- **Early Game (2-3 provinces)**: Marshal should govern the province with highest military potential (most districts, iron ore, etc.)
- **Merchant Governors**: Boost priority for towns with Market Square (+20 eval bonus)

## Traditions

### Tradition Rush
- **Trigger**: When kingdom has 0 traditions, 400+ books, and Writing or Learning tradition is available
- **Behavior**:
  - Block all construction (save gold)
  - Block all unit hiring (save gold)
  - Block Crown Authority increases (save gold)
  - Prioritize tradition adoption

### Tradition Selection
- Prefer Writing or Learning as first tradition when available

## Character Development

### Skill Selection
- **Ruler**: Prioritize Leadership and Administration skills
- **Marshal**: Prioritize Leadership and Combat skills
- Avoid Commerce skills for non-merchants
- Avoid Combat skills for non-martial characters

## Diplomacy

### War Declaration
- **Mortal Enemies**: Strictly require 1.5x power advantage (Army + Castle Strength) before declaring war.
- Consider defensive pacts against you in strength calculations (not yet implemented - see TODO.md)


## Constants & Thresholds

All numerical values are defined in `GameBalance.cs`:
- Min commerce for merchant: 10
- Commerce per merchant: 10
- Min books for tradition rush: 400
- Health retreat threshold: 0.7 (70%)
- Early game army size: 4 ranged + 4 melee
- Full army size: 8 units
- Ranged/melee ratio: 0.8 (80%)
- Religion building boost per slot: 0.2
- Barracks slot boost per slot: 0.25

## Debug Tools
- **Overlay**: Press **F9** to toggle the AI Debug Overlay.
    - Shows stats for the player kingdom (Gold, Piety, Books).
    - Lists Mortal Enemies and Neighbors with relationship status.
    - Logs "Considered Expenses" in real-time to see what the AI is thinking.
- **Spectator Mode**: Toggling the overlay also enables/disables Enhanced AI control for the player kingdom.
