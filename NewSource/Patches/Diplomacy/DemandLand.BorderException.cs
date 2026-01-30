using System.Collections.Generic;
using HarmonyLib;
using Logic;
using System;
using System.Collections;

namespace AIOverhaul.Patches.Diplomacy
{
    [HarmonyPatch(typeof(DemandLand))]
    public class DemandLand_BorderException
    {
        // Kingdom fields
        const string FIELD_EXTERNAL_BORDER_REALMS = "externalBorderRealms"; 
        const string FIELD_REALMS = "realms"; 
        const string FIELD_SOVEREIGN_STATE = "sovereignState"; 
        const string FIELD_CULTURE = "culture"; 
        const string FIELD_RELIGION = "religion";
        const string FIELD_IS_ECUMENICAL = "is_ecumenical_patriarchate";

        // Kingdom methods
        const string METHOD_IS_PAPACY = "IsPapacy";
        const string METHOD_IS_ENEMY = "IsEnemy";

        // Realm fields
        const string FIELD_CASTLE = "castle";
        const string FIELD_REALM_CULTURE = "culture";

        // Realm methods
        const string METHOD_IS_OCCUPIED = "IsOccupied";

        // Battle fields
        const string FIELD_BATTLE = "battle";
        const string FIELD_ATTACKER_KINGDOM = "attacker_kingdom";
        const string FIELD_DEFENDER_KINGDOM = "defender_kingdom";

        // Religion fields
        const string FIELD_HQ_REALM = "hq_realm";

        // Validation error string
        const string ERROR_CULTURE_MISMATCH = "pop_majoirt_not_from_recieving";
        const string VALIDATION_OK = "ok";

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

        [HarmonyPatch("Validate")]
        [HarmonyPostfix]
        static void Validate_Postfix(DemandLand __instance, ref string __result)
        {
            try
            {
                if (__result != ERROR_CULTURE_MISMATCH)
                    return;

                var realm = __instance.GetArg<object>(0);
                if (realm == null) return;

                var receiverObj = __instance.GetTargetObj();
                if (receiverObj == null) return;

                var receiverTraverse = Traverse.Create(receiverObj);
                var borderRealmsObj = receiverTraverse.Field(FIELD_EXTERNAL_BORDER_REALMS).GetValue();
                
                if (borderRealmsObj == null) return;

                if (borderRealmsObj is IList externalBorderRealms)
                {
                    if (externalBorderRealms.Contains(realm))
                    {
                        __result = VALIDATION_OK;
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

                var kingdomTraverse = Traverse.Create(kingdom);
                var kingdom2Traverse = Traverse.Create(kingdom2);

                var realmsObj = kingdomTraverse.Field(FIELD_REALMS).GetValue();
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
                var sobj = kingdom2Traverse.Field(FIELD_SOVEREIGN_STATE).GetValue();
                
                // Safe access to border realms
                var borderRealmsObj = kingdom2Traverse.Field(FIELD_EXTERNAL_BORDER_REALMS).GetValue();
                IList kingdom2BorderRealms = borderRealmsObj as IList;

                foreach (object realm in kingdomRealms)
                {
                    if (realm == null) continue;
                    if (existingRealms.Contains(realm)) continue;

                    var realmTraverse = Traverse.Create(realm);
                    
                    var realmCastle = realmTraverse.Field(FIELD_CASTLE).GetValue();
                    if (realmCastle == null) continue;
                    
                    bool isOccupied = GetBoolSafe(realmTraverse.Method(METHOD_IS_OCCUPIED));
                    if (isOccupied) continue;

                    var castleTraverse = Traverse.Create(realmCastle);
                    var castleBattle = castleTraverse.Field(FIELD_BATTLE).GetValue();
                    
                    if (castleBattle != null)
                    {
                        var battleTraverse = Traverse.Create(castleBattle);
                        var att = battleTraverse.Field(FIELD_ATTACKER_KINGDOM).GetValue();
                        var def = battleTraverse.Field(FIELD_DEFENDER_KINGDOM).GetValue();
                        
                        if (att != kingdom2 && def != kingdom2)
                            continue;
                    }

                    // Religion checks
                    var kingdomReligion = kingdomTraverse.Field(FIELD_RELIGION).GetValue();
                    if (kingdomReligion != null)
                    {
                         var hqRealm = Traverse.Create(kingdomReligion).Field(FIELD_HQ_REALM).GetValue();
                         bool isPapacy = GetBoolSafe(kingdomTraverse.Method(METHOD_IS_PAPACY));
                         bool isEcumenical = GetBoolSafe(kingdomTraverse.Field(FIELD_IS_ECUMENICAL));
                         
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
                        var isEnemyObj = kingdomTraverse.Method(METHOD_IS_ENEMY, kingdom2).GetValue();
                        if (isEnemyObj is bool b) isEnemy = b;
                    } 
                    catch {isEnemy = true;} // Default to unsafe if check fails

                    
                    if (!isEnemy && sobj != kingdom)
                    {
                        string realmCulture = GetStringSafe(realmTraverse.Field(FIELD_REALM_CULTURE));
                        string k2Culture = GetStringSafe(kingdom2Traverse.Field(FIELD_CULTURE));
                        
                        if (realmCulture != k2Culture && realmCulture != null && k2Culture != null)
                        {
                            if (kingdom2BorderRealms != null && kingdom2BorderRealms.Contains(realm))
                            {
                                // Casting to Logic.Object for Value insertion
                                Logic.Object rBase = realm as Logic.Object;
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
