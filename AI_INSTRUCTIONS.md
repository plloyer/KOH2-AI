# AI Instructions & Preferences

This file contains persistent instructions, coding styles, and preferences for the AI to follow in this project.
The AI should consult this file at the beginning of tasks to ensure compliance with user preferences.

## CRITICAL RULES - NEVER VIOLATE THESE
1. **NEVER HALLUCINATE APIs** - Always verify against `Sources/Logic/*.cs` before using any API.
2. **ALWAYS COMPILE** - Build must succeed with 0 errors before committing.
3. **VERIFY THEN CODE** - Read relevant source files first, then implement.
4. **REVISE BEFORE COMMIT** - Review for issues and improvements after implementation.
5. **COMMIT WHEN DONE** - Git commit with descriptive message when complete.
6. **NEVER BLINDLY REMOVE CODE** - If a method/API doesn't exist, understand what it does first, then implement a replacement. DO NOT just delete calls without preserving functionality.
7. **NO HARDCODED STRINGS** - Always use constants from `Constants/` folder (`BuildingNames.*`, `CharacterClassNames.*`, `ActionNames.*`, etc.).
8. **NO HARDCODED VALUES** - Always use (or create) constants in `GameBalance.cs` for all numeric values.
9. **KEEP AI_ENHANCEMENTS.md IN SYNC** - When implementing/modifying AI features, update `AI_ENHANCEMENTS.md` to match. When updating `AI_ENHANCEMENTS.md`, verify code matches documentation.

## Workflow
1. **API Discovery**: Identify game objects -> Read decompiled source (`Sources/Logic/*.cs`) -> Document EXACT properties/methods -> Check null safety.
2. **Implementation**: Filter for Enhanced AI if needed -> Choose patch type -> Write patch -> Add logging.
3. **Compilation Check**: `dotnet build` -> Fix errors -> Repeat until 0 errors.
4. **Review**: Check correctness, code quality, and performance.
5. **Documentation**: Update `AI_ENHANCEMENTS.md`.
6. **Commit**: Stage files -> Commit with detailed message.

## Common Pitfalls (AVOID THESE)
- ❌ **Using wrong property names**: e.g., using `k.vassals` instead of `k.vassalStates`.
- ❌ **Forgetting null checks**: e.g., `k.realms.Count` instead of `k.realms?.Count ?? 0`.
- ❌ **Hardcoded strings/values**: Always use constants.
- ❌ **Blindly removing code**: Never delete code just to make it compile; reimplement functionality.

## Project Context
- **Mod Name**: AIOverhaul for Knights of Honor II.
- **Goal**: Implement features with rigorous API validation.
- **Reference File**: `C:\Program Files (x86)\Steam\steamapps\common\Knights of Honor II\AIOverhaul\.claude\skills\koh2-feature.yaml`
