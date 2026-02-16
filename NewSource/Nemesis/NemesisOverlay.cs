using System.Text;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    /// <summary>
    /// Always-visible UI overlay showing the nemesis team status in multiplayer.
    /// Positioned in the top-right corner. Only visible when a nemesis team is active.
    /// </summary>
    public class NemesisOverlay : MonoBehaviour
    {
        Rect m_WindowRect;
        Texture2D m_BackgroundTexture;
        GUIStyle m_Style;
        GUIStyle m_HeaderStyle;
        string m_CachedText;
        float m_LastUpdateTime;
        const float k_UpdateInterval = 2f; // Update every 2 seconds to avoid per-frame cost

        void OnGUI()
        {
            if (!NemesisTeamManager.IsInitialized || NemesisTeamManager.NemesisKingdomIds.Count == 0) return;

            // Only show in multiplayer
            var game = AIOverhaulPlugin.CurrentGame;
            if (game?.multiplayer == null) return;

            // Lazy-init styles
            if (m_Style == null)
            {
                m_Style = new GUIStyle(GUI.skin.label);
                m_Style.fontSize = 18;
                m_Style.richText = true;
                m_Style.wordWrap = false;
                m_Style.margin = new RectOffset(0, 0, 0, 0);
                m_Style.padding = new RectOffset(0, 0, 0, 0);

                m_HeaderStyle = new GUIStyle(m_Style);
                m_HeaderStyle.fontSize = 22;
                m_HeaderStyle.fontStyle = UnityEngine.FontStyle.Bold;
            }

            // Position: top-right corner, lowered to avoid overlapping other UI
            float width = 380f;
            int humanCount = NemesisTeamManager.HumanTeamKingdomIds.Count;
            int nemesisCount = NemesisTeamManager.NemesisKingdomIds.Count;
            float height = 40f + (humanCount + nemesisCount) * 28f + 32f + 36f;
            m_WindowRect = new Rect(Screen.width - width - 10f, 100f, width, height);

            // Draw background
            if (m_BackgroundTexture == null)
            {
                m_BackgroundTexture = new Texture2D(1, 1);
                m_BackgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
                m_BackgroundTexture.Apply();
            }
            GUI.DrawTexture(m_WindowRect, m_BackgroundTexture);

            // Update cached text periodically
            if (m_CachedText == null || UnityEngine.Time.time - m_LastUpdateTime > k_UpdateInterval)
            {
                m_CachedText = BuildOverlayText(game);
                m_LastUpdateTime = UnityEngine.Time.time;
            }

            GUILayout.BeginArea(new Rect(m_WindowRect.x + 10f, m_WindowRect.y + 6f, m_WindowRect.width - 20f, m_WindowRect.height - 12f));
            GUILayout.Label("<color=#C8B078>SCOREBOARD</color>", m_HeaderStyle);

            // Draw each line from cached text
            GUILayout.Label(m_CachedText, m_Style);
            GUILayout.EndArea();
        }

        string BuildOverlayText(Game game)
        {
            var sb = new StringBuilder();

            // Human players section
            foreach (int id in NemesisTeamManager.HumanTeamKingdomIds)
            {
                Logic.Kingdom k = game.GetKingdom(id);
                if (k == null) continue;
                int realms = k.realms?.Count ?? 0;
                int fame = (int)k.fame;
                int power = (int)k.GetTotalPower();
                string color = GetWarColor(k, game, NemesisTeamManager.NemesisKingdomIds);
                sb.AppendLine($"<color={color}>{k.Name} - R:{realms} | F:{fame} | P:{power}</color>");
            }

            // Separator
            sb.AppendLine("<color=#555555>───────────────</color>");
            sb.AppendLine("<color=#C87868>NEMESIS TEAM</color>");

            // Nemesis kingdoms section
            int totalRealms = 0, totalFame = 0, totalPower = 0;
            foreach (int id in NemesisTeamManager.NemesisKingdomIds)
            {
                Logic.Kingdom k = game.GetKingdom(id);
                if (k == null) continue;

                int realms = k.realms?.Count ?? 0;
                int fame = (int)k.fame;
                int power = (int)k.GetTotalPower();

                string color;
                if (k.IsDefeated())
                {
                    color = "#706860";
                }
                else
                {
                    totalRealms += realms;
                    totalFame += fame;
                    totalPower += power;
                    color = GetWarColor(k, game, NemesisTeamManager.HumanTeamKingdomIds);
                }

                string status = k.IsDefeated() ? " [X]" : "";
                sb.AppendLine($"<color={color}>{k.Name}{status} - R:{realms} | F:{fame} | P:{power}</color>");
            }

            sb.Append($"<color=#D0C8B0>TOTAL: R:{totalRealms} | F:{totalFame} | P:{totalPower}</color>");
            return sb.ToString();
        }

        /// <summary>Returns coral if at war with opponents, amber if at war with anyone, sage if at peace.</summary>
        static string GetWarColor(Logic.Kingdom k, Game game, System.Collections.Generic.HashSet<int> opponentIds)
        {
            bool warWithOpponent = false;
            foreach (int oid in opponentIds)
            {
                Logic.Kingdom ok = game.GetKingdom(oid);
                if (ok != null && k.IsEnemy(ok)) { warWithOpponent = true; break; }
            }

            if (warWithOpponent) return "#CC6E5E";
            if (k.wars != null && k.wars.Count > 0) return "#C8A850";
            return "#7CA87C";
        }
    }
}
