# DemandLand.BorderException Patch Fixes

## Issues Fixed (2026-01-17)

### Issue 3: Invalid Type Casts - "Specified cast is not valid"
**Location**: Lines 147, 173-174 in GetPossibleArgValues_Postfix
**Problem**: Using wrong types for `GetValue<T>()` calls, causing runtime cast exceptions
**Root Cause Log Error**: `[Error  :AI Overhaul] [DemandLand] GetPossibleArgValues_Postfix exception: Specified cast is not valid.`

**Wrong Types Used**:
```csharp
// Line 147 - WRONG: sovereignState is Kingdom, not Logic.Object
var kingdom2Sovereign = kingdom2Traverse.Field(FIELD_SOVEREIGN_STATE).GetValue<Logic.Object>();

// Lines 173-174 - WRONG: attacker_kingdom and defender_kingdom are Kingdom, not Logic.Object
var attackerKingdom = Traverse.Create(castleBattle).Field(FIELD_ATTACKER_KINGDOM).GetValue<Logic.Object>();
var defenderKingdom = Traverse.Create(castleBattle).Field(FIELD_DEFENDER_KINGDOM).GetValue<Logic.Object>();
```

**Correct Types** (verified from source):
```csharp
// Line 147 - Kingdom.cs:4659: public Kingdom sovereignState
var kingdom2Sovereign = kingdom2Traverse.Field(FIELD_SOVEREIGN_STATE).GetValue<Kingdom>();

// Lines 173-174 - Battle.cs:1925/1927: public Kingdom attacker_kingdom/defender_kingdom
var attackerKingdom = Traverse.Create(castleBattle).Field(FIELD_ATTACKER_KINGDOM).GetValue<Kingdom>();
var defenderKingdom = Traverse.Create(castleBattle).Field(FIELD_DEFENDER_KINGDOM).GetValue<Kingdom>();
```

**Additional Fix**: Added object casts for comparisons between Kingdom and Object types:
```csharp
if ((object)attackerKingdom != kingdom2 && (object)defenderKingdom != kingdom2)
if ((object)kingdom2Sovereign != kingdom)
```

---

## Previously Fixed Issues

### Issue 1: Null Reference Exception - Religion Field
**Location**: Line 179-180 in GetPossibleArgValues_Postfix
**Problem**: Accessed `kingdomReligion.hq_realm` without checking if `kingdomReligion` is null first
**Fix**: Added null check before accessing religion fields:
```csharp
var kingdomReligion = kingdomTraverse.Field(FIELD_KINGDOM_RELIGION).GetValue<Religion>();
if (kingdomReligion != null)
{
    var hqRealm = Traverse.Create(kingdomReligion).Field(FIELD_HQ_REALM).GetValue<Realm>();
    var isPapacy = kingdomTraverse.Method(METHOD_IS_PAPACY).GetValue<bool>();
    var isEcumenical = kingdomTraverse.Field(FIELD_IS_ECUMENICAL).GetValue<bool>();
    if (realm == hqRealm && (isPapacy || isEcumenical))
        continue;
}
```

### Issue 2: Wrong Type for Culture Fields
**Location**: Lines 160-161 in GetPossibleArgValues_Postfix
**Problem**: Retrieved culture as `object` instead of `string`, causing incorrect comparison
**Before**:
```csharp
var realmCulture = realmTraverse.Field(FIELD_REALM_CULTURE).GetValue<object>();
var kingdom2Culture = kingdom2Traverse.Field(FIELD_KINGDOM_CULTURE).GetValue<object>();
```
**After**:
```csharp
var realmCulture = realmTraverse.Field(FIELD_REALM_CULTURE).GetValue<string>();
var kingdom2Culture = kingdom2Traverse.Field(FIELD_KINGDOM_CULTURE).GetValue<string>();
```
**Verification**: Confirmed via `Sources/Logic/Realm.cs:959` and `Sources/Logic/Kingdom.cs:4937` that culture is `public string culture`

## Patch Validation Summary

### Validate_Postfix
**Target Method**: `public override string Validate()` (DemandLand.cs:49)
**Harmony Signature**: `[HarmonyPatch("Validate")]` + `[HarmonyPostfix]`
**Method Signature**: `static void Validate_Postfix(DemandLand __instance, ref string __result)`
**Status**: ✅ **VALID**

**Logic Flow**:
1. Only activates if original method returned `ERROR_CULTURE_MISMATCH` ("pop_majoirt_not_from_recieving")
2. Gets the realm argument and receiver kingdom
3. Checks if realm is in receiver's `externalBorderRealms` list
4. If yes, changes result to "ok" to allow the offer
5. Logs the exception granted

**Error Handling**:
- ✅ Try-catch around entire method
- ✅ Null check for realm argument
- ✅ Null check for receiver object
- ✅ Null check for externalBorderRealms list
- ✅ Specific error messages for each failure point

### GetPossibleArgValues_Postfix
**Target Method**: `public override bool GetPossibleArgValues(int idx, List<Value> lst)` (DemandLand.cs:93)
**Harmony Signature**: `[HarmonyPatch("GetPossibleArgValues")]` + `[HarmonyPostfix]`
**Method Signature**: `static void GetPossibleArgValues_Postfix(DemandLand __instance, int idx, List<Value> lst)`
**Status**: ✅ **VALID**

**Logic Flow**:
1. Only processes realm argument (idx == 0)
2. Gets giver (source) and receiver (target) kingdoms
3. Builds HashSet of realms already in lst to avoid duplicates
4. Iterates through giver's realms that were NOT added by vanilla method
5. Replicates vanilla validation checks (castle exists, not occupied, battle logic, HQ realm logic)
6. For realms filtered due to culture mismatch at peace:
   - Checks if realm borders receiver (`externalBorderRealms.Contains(realm)`)
   - If yes, adds it to the list with logging
7. Does NOT modify return value (vanilla still returns correct bool)

**Error Handling**:
- ✅ Try-catch around entire method
- ✅ Null check for kingdom objects
- ✅ Null check for realms list
- ✅ Null check for religion before accessing hq_realm (FIXED)
- ✅ Specific error messages for each failure point

**Validation Checks Replicated from Vanilla**:
- ✅ Castle exists
- ✅ Not occupied
- ✅ Battle logic (if battle exists, receiver must be involved)
- ✅ HQ realm logic (can't give up Papacy/Ecumenical HQ)
- ✅ Enemy check (at peace vs at war)
- ✅ Vassal check (sovereign state)
- ✅ Culture mismatch check

**New Logic**:
- ✅ Border check (`externalBorderRealms.Contains(realm)`)

## Build Status

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**DLL Copied**: `BepInEx\plugins\AIOverhaul.dll`
**PDB Copied**: `BepInEx\plugins\AIOverhaul.pdb`

## Expected Runtime Behavior

### Vanilla Behavior
**At peace with culture mismatch**: Cannot offer land unless vassal relationship
- ❌ France (French culture) cannot offer German-culture province to Germany

### New Behavior
**At peace with culture mismatch BUT province borders receiver**:
- ✅ France can offer German-culture province to Germany IF it borders Germany
- ✅ Logs: `[DemandLand] Allowing culture mismatch - [Province] borders [Kingdom]`
- ✅ Logs: `[DemandLand] Added border realm: [Province] ([Giver] -> [Receiver])`

### Unchanged Behavior
**At war**: Still follows vanilla rules (loyal population OR borders OR under siege)
**Vassal relationship**: Still allows any culture
**Other validations**: Castle, occupation, HQ, battles all unchanged

## Testing Checklist

- [ ] Game loads without errors
- [ ] Land offer menu shows border provinces with culture mismatch
- [ ] Offering border province with culture mismatch succeeds
- [ ] Non-border provinces with culture mismatch still blocked
- [ ] Logs show "[DemandLand]" entries when condition triggers
- [ ] No null reference exceptions in BepInEx logs

---

**Last Updated**: 2026-01-17
**Build**: Debug
**All String Constants**: Verified against KOH2 source code (see STRING_VERIFICATION.md)
