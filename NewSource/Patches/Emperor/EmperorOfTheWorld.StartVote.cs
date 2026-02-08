using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul.Patches.Emperor
{
    [HarmonyPatch(typeof(EmperorOfTheWorld), "StartVote")]
    public static class EmperorOfTheWorld_StartVote
    {
        public static bool Prefix()
        {
            return false; // Never allow emperor vote
        }
    }
}
