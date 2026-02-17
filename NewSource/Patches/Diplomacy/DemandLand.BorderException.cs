using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Logic;
using Object = Logic.Object;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(DemandLand))]
    public class DemandLand_BorderException
    {
        // Kingdom fields
        const string k_FieldExternalBorderRealms = "externalBorderRealms";
        const string k_FieldRealms = "realms";
        const string k_FieldSovereignState = "sovereignState";
        const string k_FieldCulture = "culture";
        const string k_FieldReligion = "religion";
        const string k_FieldIsEcumenical = "is_ecumenical_patriarchate";

        // Kingdom methods
        const string k_MethodIsPapacy = "IsPapacy";
        const string k_MethodIsEnemy = "IsEnemy";

        // Realm fields
        const string k_FieldCastle = "castle";
        const string k_FieldRealmCulture = "culture";

        // Realm methods
        const string k_MethodIsOccupied = "IsOccupied";

        // Battle fields
        const string k_FieldBattle = "battle";
        const string k_FieldAttackerKingdom = "attacker_kingdom";
        const string k_FieldDefenderKingdom = "defender_kingdom";

        // Religion fields
        const string k_FieldHqRealm = "hq_realm";

        // Validation error string
        const string k_ErrorCultureMismatch = "pop_majoirt_not_from_recieving";
        // Helper to safely get value as type (for reference types)
        static T GetSafe<T>(Traverse t) where T : class
        {
            try
            {
                object val = t.GetValue();
                return val as T;
            }
            catch
            {
                return null;
            }
        }

        // Helper for value types
        static bool GetBoolSafe(Traverse t)
        {
            try
            {
                object val = t.GetValue();
                if (val is bool b) return b;
                return false;
            }
            catch
            {
                return false;
            }
        }

        static string GetStringSafe(Traverse t)
        {
             try
             {
                 object val = t.GetValue();
                 return val as string;
             }
             catch
             {
                 return null;
             }
        }

        static void AddAllRealmsForCoop(Logic.Kingdom sourceK, Logic.Kingdom targetK, List<Value> lst, HashSet<object> existingRealms)
        {
            var realmsObj = Traverse.Create(sourceK).Field(k_FieldRealms).GetValue();
            if (realmsObj == null) return;
            IList realms = realmsObj as IList;
            if (realms == null) return;

            foreach (object realm in realms)
            {
                if (realm == null) continue;
                if (existingRealms.Contains(realm)) continue;

                var realmTraverse = Traverse.Create(realm);
                var castle = realmTraverse.Field(k_FieldCastle).GetValue();
                if (castle == null) continue;

                bool isOccupied = GetBoolSafe(realmTraverse.Method(k_MethodIsOccupied));
                if (isOccupied) continue;

                var castleBattle = Traverse.Create(castle).Field(k_FieldBattle).GetValue();
                if (castleBattle != null)
                {
                    var battleTraverse = Traverse.Create(castleBattle);
                    var att = battleTraverse.Field(k_FieldAttackerKingdom).GetValue();
                    var def = battleTraverse.Field(k_FieldDefenderKingdom).GetValue();
                    if (att != targetK && def != targetK) continue;
                }

                Object rBase = realm as Object;
                if (rBase != null) lst.Add(new Value(rBase));
            }
        }

        [HarmonyPatch("Validate")]
        [HarmonyPostfix]
        static void Validate_Postfix(DemandLand __instance, ref string __result)
        {
            try
            {
                if (__result != k_ErrorCultureMismatch)
                    return;

                // Coop bypass: human teammates can give any realm regardless of culture
                var source = __instance.GetSourceObj() as Logic.Kingdom;
                var target = __instance.GetTargetObj() as Logic.Kingdom;
                if (source != null && target != null && NemesisTeamManager.IsHumanTeam(source) && NemesisTeamManager.IsHumanTeam(target))
                {
                    __result = DiplomacyConstants.ValidationOk;
                    return;
                }

                var realm = __instance.GetArg<object>(0);
                if (realm == null) return;

                var receiverObj = __instance.GetTargetObj();
                if (receiverObj == null) return;

                var receiverTraverse = Traverse.Create(receiverObj);
                var borderRealmsObj = receiverTraverse.Field(k_FieldExternalBorderRealms).GetValue();
                
                if (borderRealmsObj == null) return;

                if (borderRealmsObj is IList externalBorderRealms)
                {
                    if (externalBorderRealms.Contains(realm))
                    {
                        __result = DiplomacyConstants.ValidationOk;
                    }
                }
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"[DemandLand] Validate_Postfix exception: {ex.Message}");
            }
        }

        [HarmonyPatch("GetPossibleArgValues")]
        [HarmonyPostfix]
        static void GetPossibleArgValues_Postfix(DemandLand __instance, int idx, List<Value> lst)
        {
            try
            {
                if (idx != 0) return;

                var kingdom = __instance.GetSourceObj(); 
                var kingdom2 = __instance.GetTargetObj(); 

                if (kingdom == null || kingdom2 == null) return;

                // Coop bypass: human teammates see all realms regardless of culture/border
                Logic.Kingdom sourceK = kingdom as Logic.Kingdom;
                Logic.Kingdom targetK = kingdom2 as Logic.Kingdom;
                if (sourceK != null && targetK != null && NemesisTeamManager.IsHumanTeam(sourceK) && NemesisTeamManager.IsHumanTeam(targetK))
                {
                    HashSet<object> existing = new HashSet<object>();
                    foreach (var val in lst)
                    {
                        if (val.obj_val != null) existing.Add(val.obj_val);
                    }
                    AddAllRealmsForCoop(sourceK, targetK, lst, existing);
                    return;
                }

                var kingdomTraverse = Traverse.Create(kingdom);
                var kingdom2Traverse = Traverse.Create(kingdom2);

                var realmsObj = kingdomTraverse.Field(k_FieldRealms).GetValue();
                if (realmsObj == null) return;
                
                IList kingdomRealms = realmsObj as IList;
                if (kingdomRealms == null) return;

                HashSet<object> existingRealms = new HashSet<object>();
                foreach (var val in lst)
                {
                    var r = val.obj_val;
                    if (r != null) existingRealms.Add(r);
                }

                // Safe access to sovereign
                var sobj = kingdom2Traverse.Field(k_FieldSovereignState).GetValue();
                
                // Safe access to border realms
                var borderRealmsObj = kingdom2Traverse.Field(k_FieldExternalBorderRealms).GetValue();
                IList kingdom2BorderRealms = borderRealmsObj as IList;

                foreach (object realm in kingdomRealms)
                {
                    if (realm == null) continue;
                    if (existingRealms.Contains(realm)) continue;

                    var realmTraverse = Traverse.Create(realm);
                    
                    var realmCastle = realmTraverse.Field(k_FieldCastle).GetValue();
                    if (realmCastle == null) continue;
                    
                    bool isOccupied = GetBoolSafe(realmTraverse.Method(k_MethodIsOccupied));
                    if (isOccupied) continue;

                    var castleTraverse = Traverse.Create(realmCastle);
                    var castleBattle = castleTraverse.Field(k_FieldBattle).GetValue();
                    
                    if (castleBattle != null)
                    {
                        var battleTraverse = Traverse.Create(castleBattle);
                        var att = battleTraverse.Field(k_FieldAttackerKingdom).GetValue();
                        var def = battleTraverse.Field(k_FieldDefenderKingdom).GetValue();
                        
                        if (att != kingdom2 && def != kingdom2)
                            continue;
                    }

                    // Religion checks
                    var kingdomReligion = kingdomTraverse.Field(k_FieldReligion).GetValue();
                    if (kingdomReligion != null)
                    {
                         var hqRealm = Traverse.Create(kingdomReligion).Field(k_FieldHqRealm).GetValue();
                         bool isPapacy = GetBoolSafe(kingdomTraverse.Method(k_MethodIsPapacy));
                         bool isEcumenical = GetBoolSafe(kingdomTraverse.Field(k_FieldIsEcumenical));
                         
                         if (realm == hqRealm && (isPapacy || isEcumenical))
                             continue;
                    }

                    // Culture check
                    // IsEnemy method call
                    // Traverse Method parameters are tricky if types mismatch
                    // Method(name, params object[] args)
                    // If kingdom2 type mismatch, this might fail to find method signature?
                    // Safe approach: IsEnemy takes Logic.Object usually.
                    
                    bool isEnemy = false;
                    try 
                    {
                        var isEnemyObj = kingdomTraverse.Method(k_MethodIsEnemy, kingdom2).GetValue();
                        if (isEnemyObj is bool b) isEnemy = b;
                    } 
                    catch {isEnemy = true;} // Default to unsafe if check fails

                    
                    if (!isEnemy && sobj != kingdom)
                    {
                        string realmCulture = GetStringSafe(realmTraverse.Field(k_FieldRealmCulture));
                        string k2Culture = GetStringSafe(kingdom2Traverse.Field(k_FieldCulture));
                        
                        if (realmCulture != k2Culture && realmCulture != null && k2Culture != null)
                        {
                            if (kingdom2BorderRealms != null && kingdom2BorderRealms.Contains(realm))
                            {
                                // Casting to Logic.Object for Value insertion
                                Object rBase = realm as Object;
                                if (rBase != null)
                                {
                                     lst.Add(new Value(rBase)); 
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"[DemandLand] GetPossibleArgValues_Postfix exception: {ex.Message} {ex.StackTrace}");
            }
        }
    }
}
