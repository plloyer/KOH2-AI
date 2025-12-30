using AIOverhaul.Constants;

namespace AIOverhaul.Helpers
{
    public static class ModLog
    {
        public static void Log(Logic.Kingdom k, string message, LogCategory category = LogCategory.General)
        {
            if (k == null)
            {
                AIOverhaulPlugin.LogMod(message, category);
                return;
            }
            AIOverhaulPlugin.LogMod($"[{k.Name}] {message}", category);
        }
    }
}
