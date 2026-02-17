using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // Vanilla mercenary.def defines: bool valid_in_lands = (merc.is_hired || merc.army.realm_in.kingdom == hire_kingdom)
    // The UI evaluates this via the DT expression engine (DefsContext.GetVar → field tree),
    // bypassing any C# Validate() patches. Override at the data level to allow foreign hiring.
    // Cost multiplier for foreign territory is handled by MercenaryMissionDef.GetCost postfix.
    [HarmonyPatch(typeof(MercenaryMission), "LoadDefs")]
    public class MercenaryMissionDef_Validate
    {
        static void Postfix()
        {
            if (MercenaryMission.defs == null) return;

            foreach (var def in MercenaryMission.defs)
            {
                if (def?.field == null) continue;
                def.field.SetValue("valid_in_lands", new Value(true));
            }
        }
    }
}
