using System;
using HarmonyLib;
using Logic;
using UnityEngine;


namespace AIOverhaul
{
    // "AddChild" is called when a new prince/princess is born or added to the family tree.
    // Intent: HeirClassSelectionPatch
    [HarmonyPatch(typeof(Logic.RoyalFamily), "AddChild")]
    public class RoyalFamily_AddChild
    {
        const string k_LogPrefix = "[RoyalFamily]";
        static void Postfix(Logic.RoyalFamily __instance, Logic.Character child)
        {
            // Only relevant for Enhanced AI kingdoms and alive children
            if (child == null || !child.IsAlive() || !AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
            
            // Only relevant for males (Princes) who get classes
            // Applying to ALL princes (Heir or not) to enforce the distribution
            if (child.sex != Logic.Character.Sex.Male) return;

            var game = __instance.game;

            float weightMarshal = 1.0f;
            float weightMerchant = 0.8f;
            float weightCleric = 0.5f;
            float totalWeight = weightMarshal + weightMerchant + weightCleric;

            float roll = game.Random(0f, totalWeight);
            string selectedClassName = CharacterClassNames.Cleric; // Default fallback

            if (roll < weightMarshal)
                selectedClassName = CharacterClassNames.Marshal;
            else if (roll < weightMarshal + weightMerchant)
                selectedClassName = CharacterClassNames.Merchant;
            // else remain Cleric

            var classDef = game.defs.Get<CharacterClass.Def>(selectedClassName);
            
            if (classDef != null)
            {
                // Override the default class if it's different
                if (child.class_def != classDef)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Assigning {selectedClassName} to Prince {child.Name} (Roll: {roll:F2}/{totalWeight})", LogCategory.RoyalFamily, child.GetKingdom());
                    child.SetClass(classDef);
                }
                else
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Prince {child.Name} already has class {selectedClassName} (Roll: {roll:F2}/{totalWeight}) — no change", LogCategory.RoyalFamily, child.GetKingdom());
                }
            }
        }
    }
}
