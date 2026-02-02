# Land Diplomacy System - Deep Dive

## Overview

The land transfer system in Knights of Honor II allows kingdoms to **demand** or **offer** provinces (realms) through diplomatic offers. This document provides a comprehensive breakdown of how the system works.

---

## Class Hierarchy

```
Offer (base class)
└── DemandLand
    └── OfferLand (inherits DemandLand, reverses source/target)
```

### Key Files
- **`DemandLand.cs`** - Core logic for demanding land
- **`OfferLand.cs`** - Offering land (inherits from DemandLand, just reverses kingdoms)
- **`Offer.cs`** - Base offer class with validation, evaluation, AI decision-making
- **`RealmCost.cs`** - Calculates the "value" of a realm for evaluation

---

## How It Works

### 1. DemandLand vs OfferLand

**DemandLand**: "Kingdom A demands Kingdom B give them Province X"
- `from` = Kingdom A (demander)
- `to` = Kingdom B (giver)
- `GetSourceObj()` returns `to` (giver owns the land)
- `GetTargetObj()` returns `from` (demander wants the land)

**OfferLand**: "Kingdom A offers Kingdom B one of A's provinces"
- `from` = Kingdom A (giver)
- `to` = Kingdom B (receiver)
- `GetSourceObj()` returns `from` (giver owns the land)
- `GetTargetObj()` returns `to` (receiver wants the land)
- **Simply reverses the source/target from DemandLand**

### 2. Which Realms Can Be Demanded/Offered?

The `GetPossibleArgValues()` method (lines 93-128 in DemandLand.cs) determines valid realms:

#### Always Blocked:
- ❌ Realms without castles
- ❌ Occupied realms (under enemy control)
- ❌ HQ realms for Papacy/Ecumenical Patriarchate
- ❌ Realms in battle (unless the demanding/receiving kingdom is involved in that battle)
- ❌ If giver has only 1 realm (need at least 2)

#### If At War:
**Can ONLY demand/offer if ONE of these is true:**
- Realm's population majority is loyal to the receiver
- Realm borders the receiver (in `externalBorderRealms` list)
- Receiver is actively sieging the realm (battle.attacker_kingdom)

#### If At Peace (or Vassal):
**Can ONLY demand/offer if:**
- Realm's culture matches receiver's culture
- OR receiver is the sovereign (vassal relationship)

---

## 3. Realm Cost Calculation

The value of a realm is calculated in `RealmCost.CalcRealmCost()` (lines 172-199):

### Cost Components (Default Values from RealmCost.Def):

| Component | Base Value | Notes |
|-----------|------------|-------|
| **Base Cost** | 1,000 | Every realm starts here |
| **Per Settlement** | 1,000 each | Villages, farms (not castles) |
| **Province Features** | 2,000 each | Resources (Iron, Cattle, etc.) |
| **Feature (duplicate)** | 1,000 each | If receiver already has that resource elsewhere |
| **Trade Center** | 5,000 base | +100 per realm in trade zone |
| **Religious Center (own religion)** | 20,000 | Rome, Mecca, etc. |
| **Religious Center (other religion)** | 5,000 | Less valuable if different religion |
| **Siege Defense** | 10 per point | Current fortification level |
| **Population Loyal to Receiver** | 3,000 | Pop majority matches receiver |
| **Population Same Religion** | 2,000 | Pop majority shares receiver's religion |
| **Buildings** | 25% of gold cost | Existing structures |
| **Kingdom Size Factor** | 20,000 / realm count | Smaller kingdoms value land more |
| **Multiplier** | x1.0 | Final multiplier on total |

**Formula:**
```
RealmCost = (BaseCost + Settlements + Features + TradeCenter +
             ReligiousCenter + SiegeDefense + Population +
             Buildings + KingdomSizeFactor) * Multiplier
```

**Example Calculation:**
```
Province with:
- 3 settlements (3,000)
- 2 unique features (4,000)
- Trade center (5,000 + 500 for 5 realms in zone)
- Loyal population (3,000)
- 10,000 gold worth of buildings (2,500)
- Kingdom has 5 realms (4,000)
= 1,000 + 3,000 + 4,000 + 5,500 + 3,000 + 2,500 + 4,000
= 23,000 gold value
```

---

## 4. Offer Evaluation System

### AI Decision Process (Offer.cs lines 1292-1339):

When an offer is received, the AI calls `DecideAIAnswer()`:

1. **Check if ProsAndCons exist** for "accept" threshold
   - If no ProsAndCons defined → decline

2. **Evaluate the offer**: `float eval = Eval("accept")`
   - Returns positive if good for receiver, negative if bad

3. **If eval > 0**: ACCEPT immediately

4. **If eval < 0**: Try to create a **CounterOffer**
   - Calculate how much additional value needed: `eval * cover_perc_min/max`
   - Use `OfferGenerator` to find sweeteners (gold, books, etc.)
   - If valid counter-offer created → send it
   - Otherwise → DECLINE

### Eval() Method (DemandLand.cs lines 159-180):

```csharp
public override float Eval(string threshold_name, bool reverse_kingdoms = false)
{
    Realm realm = GetArg<Realm>(0);
    Kingdom forKingdom = GetSourceObj(); // Giver kingdom

    // For "Propose" threshold, evaluate from demander's perspective
    if (threshold_name == "Propose")
        forKingdom = GetTargetObj();

    // For "Accept" threshold (default), evaluate from giver's perspective

    ProsAndCons pc = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);

    return realm.CalcCost(forKingdom) * pc.def.cost.Float(pc, 100000f);
}
```

**Key Points:**
- **"Propose" threshold**: AI evaluates if THEY should demand this land (from demander's POV)
- **"Accept" threshold**: AI evaluates if THEY should give up this land (from giver's POV)
- **Positive eval** = good deal, **negative eval** = bad deal
- Multiplied by ProsAndCons cost multiplier (usually in data files)

---

## 5. AI Behavior & Restrictions

### AI Cannot Autonomously Offer Land (KingdomAI.cs lines 2483-2486):

```csharp
if (offer.def.field.key == "OfferLand")
{
    return null; // AI will NEVER propose OfferLand on its own
}
```

**This means:**
- ✅ AI CAN demand land from others (DemandLand)
- ✅ AI CAN accept player's land offers (OfferLand)
- ❌ AI will NEVER proactively offer land to player or other AI

### When AI Can Demand Land:

From `DemandLand.IsValidForAI()` (lines 137-143):
```csharp
public override bool IsValidForAI()
{
    if (AI && parent == null)
        return false; // AI can ONLY demand land as part of a parent offer

    return base.IsValidForAI();
}
```

**AI can demand land ONLY:**
- As part of a **parent offer** (e.g., peace treaty, alliance negotiation)
- NOT as standalone demands

---

## 6. Validation Rules

### HasValidParent() (lines 32-46):

Land demands/offers must satisfy:
1. If no parent offer AND kingdoms are enemies → **INVALID**
2. Giver must have at least 2 realms after accounting for other pending land transfers

### Validate() Method (lines 49-91):

Checks all of the following:
- ✅ Realm has a castle
- ✅ Realm is owned by giver kingdom
- ✅ Realm is not occupied
- ✅ Not the religious HQ for Papacy/Patriarchate
- ✅ If in battle, one of the kingdoms must be involved
- ✅ **At war**: Realm must have loyal pop OR border receiver OR be under siege by receiver
- ✅ **At peace**: Realm's culture must match receiver (unless vassal relationship)

---

## 7. What Happens When Accepted

### OnAccept() (lines 130-135):

```csharp
public override void OnAccept()
{
    base.OnAccept();
    Kingdom targetKingdom = GetTargetObj() as Kingdom;
    Realm realm = GetArg<Realm>(0);

    realm.SetKingdom(targetKingdom.id,
        ignore_victory: false,
        check_cancel_battle: true,
        via_diplomacy: true);
}
```

**The realm transfers ownership:**
- Realm.SetKingdom() changes the owner
- Checks for victory conditions
- Cancels battles if needed
- Marked as diplomatic transfer (not conquest)

---

## 8. ProsAndCons System Integration

The evaluation uses **ProsAndCons definitions** (not found in code, likely in data files):

### Expected Structure:
```
PC_Accept_DemandLand:
    thresholds:
        accept:
            cost: [multiplier expression]
            factors:
                realm_importance: [weight]
                relation_with_demander: [weight]
                military_threat: [weight]
                ...

PC_Propose_DemandLand:
    thresholds:
        propose:
            cost: [multiplier expression]
            factors:
                realm_strategic_value: [weight]
                claim_strength: [weight]
                likelihood_of_success: [weight]
                ...
```

**The system multiplies:**
```
Eval = RealmCost * ProsAndCons_Multiplier
```

---

## 9. Potential Modding Opportunities

### To Make AI Offer Land More Actively:

**Option 1: Remove the block** (KingdomAI.cs:2483-2486)
```csharp
// DELETE THESE LINES:
if (offer.def.field.key == "OfferLand")
{
    return null;
}
```

**Option 2: Allow standalone demands** (DemandLand.cs:137-143)
```csharp
public override bool IsValidForAI()
{
    // Remove the parent requirement:
    // if (AI && parent == null)
    //     return false;

    return base.IsValidForAI();
}
```

### To Change Realm Valuation:

**Modify `RealmCost.Def` values:**
- Increase `population_is_loyal_to_us` to make AI value loyal provinces more
- Decrease `religious_center_from_our_religion` to make AI willing to trade holy sites
- Adjust `multiplier` globally (e.g., 0.5 makes all land worth half)

### To Make AI More Generous with Land:

**Create/modify ProsAndCons data:**
- Lower the cost multiplier for "Accept" threshold
- Add positive factors when at peace with demander
- Weight relation level heavily (friendly kingdoms more willing to trade land)

### To Restrict Land Demands in War:

**Modify validation rules** (DemandLand.cs:113-118):
```csharp
if (kingdom.IsEnemy(kingdom2))
{
    // Make it stricter:
    if (realm.pop_majority.kingdom != kingdom2 &&
        !kingdom2.externalBorderRealms.Contains(realm) &&
        realm.castle.battle?.attacker_kingdom != kingdom2)
    {
        // ADD: Also require siege progress > 50%
        if (realm.castle.battle == null ||
            realm.castle.battle.siege_progress < 0.5f)
        {
            continue; // Skip this realm
        }
    }
}
```

---

## 10. Summary

### Core Mechanics:
1. **DemandLand** = Taking someone else's land
2. **OfferLand** = Giving your own land (just DemandLand reversed)
3. **Realm cost** calculated from settlements, resources, buildings, population loyalty
4. **AI accepts** if evaluation is positive (cost * ProsAndCons multiplier > 0)
5. **AI counteroffers** if evaluation negative but close enough to sweeten

### Current AI Limitations:
- ❌ Never offers land proactively
- ❌ Only demands land within parent offers (peace, alliance)
- ❌ Strict validation for which realms can be transferred

### Key Tuning Knobs:
- **RealmCost.Def** values (how much is land worth?)
- **ProsAndCons** definitions (when to accept/propose?)
- **Validation rules** (which realms are valid?)
- **AI blocks** in KingdomAI (allow OfferLand autonomously?)

---

## Related Files for Reference

- `Sources/Logic/DemandLand.cs` - Core demand logic
- `Sources/Logic/OfferLand.cs` - Offer wrapper
- `Sources/Logic/Offer.cs` - Base offer system, AI decision-making
- `Sources/Logic/RealmCost.cs` - Realm valuation
- `Sources/Logic/ProsAndCons.cs` - Evaluation factor system
- `Sources/Logic/KingdomAI.cs` - AI offer generation (line 2483: OfferLand block)
- `Sources/Logic/Realm.cs` - CalcCost() method (line 4109)

---

## IMPLEMENTED MODIFICATION

### Border Province Exception (DemandLand.BorderException.cs)

**Status**: ✅ **IMPLEMENTED** in `NewSource/Patches/Diplomacy/DemandLand.BorderException.cs`

**What it does:**
Adds an exception to the culture matching rule - players (and AI) can now offer/demand land to/from kingdoms even if cultures don't match, **as long as the province borders the receiver's territory**.

**Technical implementation:**
- Patches `DemandLand.Validate()` with a Postfix
  - Intercepts the "pop_majoirt_not_from_recieving" error
  - Checks if the realm is in receiver's `externalBorderRealms`
  - If yes, changes result to "ok"

- Patches `DemandLand.GetPossibleArgValues()` with a Postfix
  - Adds realms that were filtered out due to culture mismatch
  - Only includes those that border the receiver
  - Validates all other requirements (not occupied, not in battle, not HQ, etc.)

**Vanilla behavior:**
- At peace, can ONLY offer land if culture matches (or vassal relationship)
- Many border provinces blocked due to culture mismatch

**New behavior:**
- At peace, can offer land if:
  - Culture matches (vanilla), OR
  - Vassal relationship (vanilla), OR
  - **Province borders receiver** (NEW)

**Example:**
France (French culture) can now offer a German-culture province to Germany if that province borders Germany, even though cultures don't match.

**Logging:**
- `[DemandLand] Allowing culture mismatch - [Realm] borders [Kingdom]`
- `[DemandLand] Added border realm: [Realm] ([Giver] -> [Receiver])`

---

*Generated via deep dive into Knights of Honor II source code.*
*All line numbers reference the decompiled Sources/Logic/*.cs files.*
