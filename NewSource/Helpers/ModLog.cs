using AIOverhaul.Constants;

namespace AIOverhaul.Helpers
{
    public static class ModLog
    {
        public static void Log(Logic.Kingdom k, string message)
        {
            if (k == null)
            {
                AIOverhaulPlugin.LogMod(message);
                return;
            }
            AIOverhaulPlugin.LogMod($"[{k.Name}] {message}");
        }
    }
}
