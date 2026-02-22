using System.Collections.Generic;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    /// <summary>
    /// In-game settings menu (F5) allowing the player to configure auto-refuse for specific diplomatic offer types
    /// and AI behavior settings for spectator mode. Settings persist via Kingdom.SetVar/GetVar.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        public static SettingsMenu Instance { get; private set; }

        static readonly HashSet<string> s_AutoRefusedOffers = new HashSet<string>();
        bool m_IsVisible;
        Vector2 m_ScrollPosition;
        int m_WindowId;

        // Flat-UI cached textures and styles
        Texture2D m_BgTex;
        Texture2D m_SepTex;
        Texture2D m_BtnTex;
        Texture2D m_BtnHoverTex;
        Texture2D m_ToggleOffTex;
        Texture2D m_ToggleOnTex;
        GUIStyle m_WindowStyle;
        GUIStyle m_TitleStyle;
        GUIStyle m_SectionStyle;
        GUIStyle m_CategoryStyle;
        GUIStyle m_ToggleStyle;
        GUIStyle m_CloseStyle;

        // --- AI Behavior settings (spectator mode) ---
        // Static fields are for local UI display only. Query methods read from kingdom vars
        // so the host always gets the correct per-kingdom setting in multiplayer.
        static bool s_CanDeclareWar = true;
        static bool s_IsDefensive = false;   // false = Offensive, true = Defensive
        static bool s_RushCastle = false;
        static bool s_CanNAP = true;
        static bool s_CanDemandSupport = true;
        static bool s_MercCastleOnly = false;
        bool m_AutoRefuseExpanded;

        // Offer type definitions: (className, category, displayName)
        static readonly (string key, string category, string displayName)[] s_OfferTypes =
        {
            // Pacts
            ("SignTrade",                  "Pacts",      "Trade Agreement"),
            ("SignNonAggression",          "Pacts",      "Non-Aggression Pact"),
            ("OfferJoinInDefensivePact",   "Pacts",      "Defensive Pact"),
            ("OfferJoinInOffensivePact",   "Pacts",      "Offensive Pact"),
            ("DemandJoinInDefensivePact",  "Pacts",      "Defensive Pact (Demand)"),
            ("DemandJoinInOffensivePact",  "Pacts",      "Offensive Pact (Demand)"),

            // Military
            ("OfferSupportInWar",          "Military",   "War Support"),
            ("DemandSupportInWar",         "Military",   "War Support (Demand)"),
            ("OfferAttackKingdom",         "Military",   "Attack Kingdom"),
            ("DemandAttackKingdom",        "Military",   "Attack Kingdom (Demand)"),
            ("OfferHelpWithRebels",        "Military",   "Help With Rebels"),
            ("DemandHelpWithRebels",       "Military",   "Help With Rebels (Demand)"),

            // Peace
            ("WhitePeaceOffer",            "Peace",      "White Peace"),
            ("PeaceOfferTribute",          "Peace",      "Peace (Tribute Offered)"),
            ("PeaceDemandTribute",         "Peace",      "Peace (Tribute Demanded)"),

            // Royal
            ("MarriageOffer",              "Royal",      "Marriage Proposal"),
            ("PrincessClaimInheritanceOffer", "Royal",   "Inheritance Claim"),

            // Vassalage
            ("OfferVassalage",             "Vassalage",  "Vassalage Offer"),
            ("DemandVassalage",            "Vassalage",  "Vassalage Demand"),
            ("OfferReleaseVassalage",      "Vassalage",  "Release Vassalage"),

            // Resources
            ("OfferLand",                  "Resources",  "Land Offer"),
            ("DemandLand",                 "Resources",  "Land Demand"),
            ("OfferGold",                  "Resources",  "Gold Offer"),
            ("DemandGold",                 "Resources",  "Gold Demand"),

            // Religion
            ("OfferChangeReligion",        "Religion",   "Change Religion"),
            ("DemandChangeReligion",       "Religion",   "Change Religion (Demand)"),
            ("OfferAbandonCaliphate",      "Religion",   "Abandon Caliphate"),
            ("DemandAbandonCaliphate",     "Religion",   "Abandon Caliphate (Demand)"),
            ("LeadCrusadeOffer",           "Religion",   "Lead Crusade"),
        };

        void Awake()
        {
            Instance = this;
            m_WindowId = GetInstanceID();
        }

        public void Toggle()
        {
            m_IsVisible = !m_IsVisible;
        }

        public static bool IsAutoRefused(string offerTypeName)
        {
            return s_AutoRefusedOffers.Contains(offerTypeName);
        }

        // --- AI Behavior query methods (read from kingdom vars so host gets correct per-kingdom value in MP) ---
        public static bool CanDeclareWar(Logic.Kingdom k) { return !k.is_player || ReadBool(k, CampaignVarNames.AIDeclareWar, true); }
        public static bool IsDefensive(Logic.Kingdom k) { return k.is_player && ReadBool(k, CampaignVarNames.AIDefensive, false); }
        public static bool ShouldRushCastle(Logic.Kingdom k) { return k.is_player && ReadBool(k, CampaignVarNames.AIRushCastle, false); }
        public static bool CanProposeNAP(Logic.Kingdom k) { return !k.is_player || ReadBool(k, CampaignVarNames.AICanNAP, true); }
        public static bool CanDemandSupportInWar(Logic.Kingdom k) { return !k.is_player || ReadBool(k, CampaignVarNames.AIDemandSupport, true); }
        public static bool MercsCastleOnly(Logic.Kingdom k) { return k.is_player && ReadBool(k, CampaignVarNames.AIMercCastleOnly, false); }

        static bool ReadBool(Logic.Kingdom k, string varName, bool defaultValue)
        {
            Value v = k.GetVar(varName);
            if (v.type == Value.Type.Int) return (int)v == 1;
            return defaultValue;
        }

        /// <summary>
        /// Load auto-refuse and AI behavior settings from kingdom vars (call on game init for each player kingdom).
        /// </summary>
        public static void LoadSettings(Logic.Kingdom k)
        {
            if (k == null) return;
            s_AutoRefusedOffers.Clear();

            foreach (var entry in s_OfferTypes)
            {
                string varName = CampaignVarNames.AutoRefusePrefix + entry.key;
                Value v = k.GetVar(varName);
                if (v.type == Value.Type.Int && (int)v == 1)
                    s_AutoRefusedOffers.Add(entry.key);
            }

            if (s_AutoRefusedOffers.Count > 0)
                AIOverhaulPlugin.LogInfo($"Loaded {s_AutoRefusedOffers.Count} auto-refuse settings for {k.Name}", LogCategory.Diplomacy, k);

            // Load AI behavior settings into static fields for UI display
            s_CanDeclareWar = ReadBool(k, CampaignVarNames.AIDeclareWar, true);
            s_IsDefensive = ReadBool(k, CampaignVarNames.AIDefensive, false);
            s_RushCastle = ReadBool(k, CampaignVarNames.AIRushCastle, false);
            s_CanNAP = ReadBool(k, CampaignVarNames.AICanNAP, true);
            s_CanDemandSupport = ReadBool(k, CampaignVarNames.AIDemandSupport, true);
            s_MercCastleOnly = ReadBool(k, CampaignVarNames.AIMercCastleOnly, false);
        }

        static void SaveSetting(Logic.Kingdom k, string offerType, bool refused)
        {
            if (k == null) return;
            string varName = CampaignVarNames.AutoRefusePrefix + offerType;
            k.SetVar(varName, new Value(refused ? 1 : 0));
        }

        static void SaveAISetting(Logic.Kingdom k, string varName, bool value)
        {
            if (k == null) return;
            k.SetVar(varName, new Value(value ? 1 : 0));
        }

        Logic.Kingdom GetPlayerKingdom()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return null;
            return game.GetLocalPlayerKingdom();
        }

        static Texture2D MakeTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        void InitStyles()
        {
            if (m_BgTex != null) return;

            m_BgTex = MakeTex(new Color(0.11f, 0.11f, 0.13f, 0.96f));
            m_SepTex = MakeTex(new Color(0.24f, 0.24f, 0.27f));
            m_BtnTex = MakeTex(new Color(0.18f, 0.18f, 0.20f));
            m_BtnHoverTex = MakeTex(new Color(0.26f, 0.26f, 0.29f));
            m_ToggleOffTex = MakeTex(new Color(0.25f, 0.25f, 0.28f));
            m_ToggleOnTex = MakeTex(new Color(0.35f, 0.72f, 0.48f));

            m_WindowStyle = new GUIStyle(GUI.skin.window);
            m_WindowStyle.normal.background = m_BgTex;
            m_WindowStyle.onNormal.background = m_BgTex;
            m_WindowStyle.focused.background = m_BgTex;
            m_WindowStyle.onFocused.background = m_BgTex;
            m_WindowStyle.border = new RectOffset(0, 0, 0, 0);
            m_WindowStyle.padding = new RectOffset(16, 16, 8, 12);
            m_WindowStyle.contentOffset = Vector2.zero;

            m_TitleStyle = new GUIStyle(GUI.skin.label);
            m_TitleStyle.fontSize = 15;
            m_TitleStyle.fontStyle = UnityEngine.FontStyle.Bold;
            m_TitleStyle.normal.textColor = new Color(0.82f, 0.82f, 0.84f);
            m_TitleStyle.alignment = UnityEngine.TextAnchor.MiddleCenter;

            m_SectionStyle = new GUIStyle(GUI.skin.label);
            m_SectionStyle.fontSize = 13;
            m_SectionStyle.fontStyle = UnityEngine.FontStyle.Bold;
            m_SectionStyle.normal.textColor = new Color(0.55f, 0.70f, 0.85f);

            m_CategoryStyle = new GUIStyle(GUI.skin.label);
            m_CategoryStyle.fontSize = 11;
            m_CategoryStyle.normal.textColor = new Color(0.50f, 0.50f, 0.52f);

            m_ToggleStyle = new GUIStyle(GUI.skin.toggle);
            m_ToggleStyle.fontSize = 12;
            m_ToggleStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            m_ToggleStyle.onNormal.textColor = new Color(0.1f, 0.1f, 0.1f);
            m_ToggleStyle.hover.textColor = new Color(0.85f, 0.85f, 0.85f);
            m_ToggleStyle.onHover.textColor = new Color(0.1f, 0.1f, 0.1f);
            m_ToggleStyle.normal.background = m_ToggleOffTex;
            m_ToggleStyle.onNormal.background = m_ToggleOnTex;
            m_ToggleStyle.hover.background = m_ToggleOffTex;
            m_ToggleStyle.onHover.background = m_ToggleOnTex;
            m_ToggleStyle.active.background = m_ToggleOffTex;
            m_ToggleStyle.onActive.background = m_ToggleOnTex;
            m_ToggleStyle.focused.background = m_ToggleOffTex;
            m_ToggleStyle.onFocused.background = m_ToggleOnTex;

            m_CloseStyle = new GUIStyle(GUI.skin.button);
            m_CloseStyle.normal.background = m_BtnTex;
            m_CloseStyle.hover.background = m_BtnHoverTex;
            m_CloseStyle.active.background = m_BtnHoverTex;
            m_CloseStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
            m_CloseStyle.hover.textColor = new Color(0.85f, 0.85f, 0.85f);
            m_CloseStyle.fontSize = 12;
            m_CloseStyle.border = new RectOffset(0, 0, 0, 0);
        }

        void OnGUI()
        {
            if (!m_IsVisible) return;
            InitStyles();

            float windowWidth = 440f;
            float windowHeight = 900f;
            float x = Screen.width - windowWidth - 10f;
            Rect scoreboardRect = NemesisOverlay.WindowRect;
            float y = scoreboardRect.height > 0 ? scoreboardRect.yMax + 6f : 700f;
            float bottomReserve = 500f; // leave space for the minimap
            if (y + windowHeight > Screen.height - bottomReserve) windowHeight = Screen.height - bottomReserve - y;
            Rect windowRect = new Rect(x, y, windowWidth, windowHeight);

            GUI.Window(m_WindowId, windowRect, DrawWindow, "", m_WindowStyle);

            // Consume mouse events inside the window so they don't pass to the game
            if (windowRect.Contains(UnityEngine.Event.current.mousePosition))
            {
                if (UnityEngine.Event.current.type == UnityEngine.EventType.ScrollWheel
                    || UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown
                    || UnityEngine.Event.current.type == UnityEngine.EventType.MouseUp
                    || UnityEngine.Event.current.type == UnityEngine.EventType.MouseDrag)
                {
                    UnityEngine.Event.current.Use();
                }
                Input.ResetInputAxes();
            }
        }

        void DrawSeparator()
        {
            GUILayout.Space(4f);
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUI.DrawTexture(r, m_SepTex);
            GUILayout.Space(4f);
        }

        void DrawWindow(int id)
        {
            GUILayout.Space(2f);
            GUILayout.Label("AI Overhaul Settings", m_TitleStyle);
            DrawSeparator();

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            Logic.Kingdom playerKingdom = GetPlayerKingdom();

            // Foldout: Auto-Refuse Offers
            string arrow = m_AutoRefuseExpanded ? "\u25BC" : "\u25B6";
            if (GUILayout.Button(arrow + "  Auto-Refuse Offers", m_SectionStyle))
                m_AutoRefuseExpanded = !m_AutoRefuseExpanded;

            if (m_AutoRefuseExpanded)
            {
                GUILayout.Space(4f);
                string currentCategory = "";

                foreach (var entry in s_OfferTypes)
                {
                    if (entry.category != currentCategory)
                    {
                        if (currentCategory != "") GUILayout.Space(6f);
                        currentCategory = entry.category;
                        GUILayout.Label(currentCategory.ToUpper(), m_CategoryStyle);
                        GUILayout.Space(1f);
                    }

                    bool wasRefused = s_AutoRefusedOffers.Contains(entry.key);
                    bool isRefused = GUILayout.Toggle(wasRefused, "  " + entry.displayName, m_ToggleStyle);

                    if (isRefused != wasRefused)
                    {
                        if (isRefused) s_AutoRefusedOffers.Add(entry.key);
                        else s_AutoRefusedOffers.Remove(entry.key);
                        SaveSetting(playerKingdom, entry.key, isRefused);
                        AIOverhaulPlugin.LogInfo($"Auto-refuse {entry.displayName}: {(isRefused ? "ON" : "OFF")}", LogCategory.Diplomacy, playerKingdom);
                    }
                }
            }

            GUILayout.Space(8f);
            DrawSeparator();
            GUILayout.Label("AI Behavior (Spectator)", m_SectionStyle);
            GUILayout.Space(4f);

            DrawAIToggle(playerKingdom, ref s_CanDeclareWar, CampaignVarNames.AIDeclareWar, "Can Declare War");
            GUILayout.Space(2f);

            GUILayout.Label("PERSONALITY", m_CategoryStyle);
            GUILayout.Space(1f);
            bool wasDefensive = s_IsDefensive;
            bool offensiveSelected = GUILayout.Toggle(!s_IsDefensive, "  Offensive", m_ToggleStyle);
            bool defensiveSelected = GUILayout.Toggle(s_IsDefensive, "  Defensive", m_ToggleStyle);
            if (offensiveSelected && s_IsDefensive) s_IsDefensive = false;
            else if (defensiveSelected && !s_IsDefensive) s_IsDefensive = true;
            if (s_IsDefensive != wasDefensive) SaveAISetting(playerKingdom, CampaignVarNames.AIDefensive, s_IsDefensive);
            GUILayout.Space(2f);

            DrawAIToggle(playerKingdom, ref s_RushCastle, CampaignVarNames.AIRushCastle, "Rush for Castle");
            DrawAIToggle(playerKingdom, ref s_CanNAP, CampaignVarNames.AICanNAP, "Can NAP");
            DrawAIToggle(playerKingdom, ref s_CanDemandSupport, CampaignVarNames.AIDemandSupport, "Can Demand Support In War");
            DrawAIToggle(playerKingdom, ref s_MercCastleOnly, CampaignVarNames.AIMercCastleOnly, "Mercs: Castle Only");

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            if (GUILayout.Button("Close", m_CloseStyle, GUILayout.Height(28)))
                m_IsVisible = false;
        }

        void DrawAIToggle(Logic.Kingdom k, ref bool setting, string varName, string label)
        {
            bool prev = setting;
            setting = GUILayout.Toggle(prev, "  " + label, m_ToggleStyle);
            if (setting != prev) SaveAISetting(k, varName, setting);
        }
    }
}
