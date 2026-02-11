using System;
using HarmonyLib;
using UnityEngine;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Movement), "MoveTo",
        new Type[] { typeof(PPos), typeof(float), typeof(bool),
                     typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public class Movement_MoveTo_ForceDirectPath
    {
        static void Prefix(ref bool low_level_only)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                low_level_only = true;
                AIOverhaulPlugin.LogInfo("[PathFinding] CTRL+RClick: forcing low-level-only path");
            }
        }
    }

    [HarmonyPatch(typeof(Movement), "AddMoveTo",
        new Type[] { typeof(PPos), typeof(float), typeof(bool),
                     typeof(bool), typeof(bool), typeof(bool) })]
    public class Movement_AddMoveTo_ForceDirectPath
    {
        static void Prefix(ref bool low_level_only)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                low_level_only = true;
                AIOverhaulPlugin.LogInfo("[PathFinding] CTRL+RClick: forcing low-level-only path (queued)");
            }
        }
    }
}
