using System.Collections.Generic;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    public class PingSystem : MonoBehaviour
    {
        public static PingSystem Instance { get; private set; }

        struct PingData
        {
            public Vector3 Position;
            public Color Color;
            public string SenderName;
            public float SpawnTime;
        }

        // --- Constants ---
        const float k_Duration = 12f;
        const float k_RingRadius = 22f;
        const int k_RingSegments = 64;
        const float k_RingThickness = 1.5f;
        const float k_BeaconHeight = 140f;
        const float k_BeaconWidthBase = 2.0f;
        const float k_BeaconWidthTop = 0.4f;
        const float k_PulseSpeed = 4f;
        const float k_CrossLength = 18f;
        const float k_CrossWidthBase = 1.8f;
        const float k_CrossWidthTip = 0.5f;
        const float k_ShockwaveInterval = 1.5f;
        const float k_ShockwaveLifetime = 2f;
        const float k_ShockwaveStartRadius = 22f;
        const float k_ShockwaveEndRadius = 50f;
        const float k_ShockwaveThickness = 1.2f;
        const float k_SpawnFlashDuration = 0.4f;
        const int k_LabelFontSize = 26;
        const float k_EdgeIndicatorSize = 24f;
        const float k_MinimapSearchInterval = 5f;
        const float k_MinimapMarkerSize = 10f;
        const float k_MinimapPulseSpeed = 3f;

        readonly List<PingData> m_Pings = new List<PingData>();
        Material m_GlMaterial;
        Material m_GlowMaterial;

        // Minimap discovery
        Camera m_MinimapCamera;
        bool m_MinimapCameraSearched;
        float m_LastMinimapSearchTime;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (m_GlMaterial != null) Destroy(m_GlMaterial);
            if (m_GlowMaterial != null) Destroy(m_GlowMaterial);
        }

        void Update()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;

            // Remove expired pings
            for (int i = m_Pings.Count - 1; i >= 0; i--)
            {
                if (now - m_Pings[i].SpawnTime > k_Duration)
                    m_Pings.RemoveAt(i);
            }

            // Detect Ctrl+LClick
            if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                TryPlacePing();
            }

            // Lazy minimap camera discovery
            TryFindMinimapCamera(now);
        }

        void TryPlacePing()
        {
            Logic.Kingdom localKingdom = GetLocalKingdom();
            if (localKingdom == null) return;

            Vector3 hitPos;
            if (!RaycastMousePosition(out hitPos)) return;

            Color color = ExtractKingdomColor(localKingdom);
            AddPing(hitPos, color, localKingdom.Name);

            // Send over network if in multiplayer
            var game = AIOverhaulPlugin.CurrentGame;
            if (game?.multiplayer != null)
            {
                PingNetworkHelper.SendPing(hitPos, localKingdom);
            }
        }

        bool RaycastMousePosition(out Vector3 hitPos)
        {
            hitPos = Vector3.zero;
            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 5000f))
            {
                hitPos = hit.point;
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            float enter;
            if (ground.Raycast(ray, out enter))
            {
                hitPos = ray.GetPoint(enter);
                return true;
            }

            return false;
        }

        public void ReceiveNetworkPing(Vector3 position, string senderName)
        {
            Logic.Kingdom local = GetLocalKingdom();
            if (local != null && local.Name == senderName) return;

            var game = AIOverhaulPlugin.CurrentGame;
            Color color = Color.white;
            if (game != null)
            {
                Logic.Kingdom senderK = game.GetKingdom(senderName);
                if (senderK != null)
                    color = ExtractKingdomColor(senderK);
            }

            AddPing(position, color, senderName);
        }

        void AddPing(Vector3 position, Color color, string senderName)
        {
            m_Pings.Add(new PingData
            {
                Position = position,
                Color = color,
                SenderName = senderName,
                SpawnTime = UnityEngine.Time.realtimeSinceStartup
            });
        }

        static Color ExtractKingdomColor(Logic.Kingdom kingdom)
        {
            // In multiplayer, use fixed colors per player slot
            var playerData = Logic.Multiplayer.CurrentPlayers.GetByKingdom(kingdom.Name);
            if (playerData != null)
            {
                return GetPlayerSlotColor(playerData.pid);
            }

            // Singleplayer fallback: use kingdom map_color
            int packed = kingdom.map_color;
            float r = ((packed >> 16) & 0xFF) / 255f;
            float g = ((packed >> 8) & 0xFF) / 255f;
            float b = (packed & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }

        static Color GetPlayerSlotColor(int pid)
        {
            switch (pid)
            {
                case 1: return new Color(1f, 0.2f, 0.2f, 1f);   // Red
                case 2: return new Color(0.3f, 0.5f, 1f, 1f);   // Blue
                case 3: return new Color(0.2f, 0.9f, 0.3f, 1f); // Green
                default: return new Color(1f, 0.9f, 0.2f, 1f);  // Yellow
            }
        }

        Logic.Kingdom GetLocalKingdom()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return null;

            var k = game.GetLocalPlayerKingdom();
            if (k != null) return k;

            if (game.campaign == null) return null;
            string kingdomName = CampaignUtils.GetOwnKingdomName(game.campaign);
            if (string.IsNullOrEmpty(kingdomName)) return null;
            return game.GetKingdom(kingdomName);
        }

        // ---------------------------------------------------------------
        // Color computation: HSV boost + spawn flash + pulsing alpha
        // ---------------------------------------------------------------

        Color ComputePingColor(Color baseColor, float age, float fade)
        {
            // Boost saturation and brightness via HSV
            float h, s, v;
            Color.RGBToHSV(baseColor, out h, out s, out v);
            s = Mathf.Clamp01(s + 0.3f);
            v = Mathf.Clamp01(v + 0.4f);
            Color boosted = Color.HSVToRGB(h, s, v);

            // Spawn flash: lerp from white to boosted over first 0.4s
            if (age < k_SpawnFlashDuration)
            {
                float t = age / k_SpawnFlashDuration;
                boosted = Color.Lerp(Color.white, boosted, t);
            }

            // Pulsing alpha
            float pulse = 0.5f + 0.5f * Mathf.Sin(age * k_PulseSpeed * Mathf.PI * 2f);
            boosted.a = fade * (0.4f + 0.6f * pulse);

            return boosted;
        }

        // ---------------------------------------------------------------
        // Material setup
        // ---------------------------------------------------------------

        void EnsureGlMaterials()
        {
            if (m_GlMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                m_GlMaterial = new Material(shader);
                m_GlMaterial.hideFlags = HideFlags.HideAndDontSave;
                m_GlMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m_GlMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m_GlMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                m_GlMaterial.SetInt("_ZWrite", 0);
                m_GlMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }

            if (m_GlowMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                m_GlowMaterial = new Material(shader);
                m_GlowMaterial.hideFlags = HideFlags.HideAndDontSave;
                m_GlowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m_GlowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
                m_GlowMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                m_GlowMaterial.SetInt("_ZWrite", 0);
                m_GlowMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        // ---------------------------------------------------------------
        // GL Drawing helpers
        // ---------------------------------------------------------------

        void DrawThickRing(Vector3 center, float radius, float thickness, Color color)
        {
            float halfThick = thickness * 0.5f;
            float innerR = radius - halfThick;
            float outerR = radius + halfThick;

            GL.Begin(GL.QUADS);
            GL.Color(color);

            for (int i = 0; i < k_RingSegments; i++)
            {
                float a1 = (float)i / k_RingSegments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / k_RingSegments * Mathf.PI * 2f;

                float cos1 = Mathf.Cos(a1), sin1 = Mathf.Sin(a1);
                float cos2 = Mathf.Cos(a2), sin2 = Mathf.Sin(a2);

                Vector3 inner1 = center + new Vector3(cos1 * innerR, 0.5f, sin1 * innerR);
                Vector3 outer1 = center + new Vector3(cos1 * outerR, 0.5f, sin1 * outerR);
                Vector3 inner2 = center + new Vector3(cos2 * innerR, 0.5f, sin2 * innerR);
                Vector3 outer2 = center + new Vector3(cos2 * outerR, 0.5f, sin2 * outerR);

                GL.Vertex(inner1);
                GL.Vertex(outer1);
                GL.Vertex(outer2);
                GL.Vertex(inner2);
            }

            GL.End();
        }

        void DrawShockwaves(Vector3 center, Color baseColor, float age, float fade)
        {
            // Determine which shockwave rings are currently active
            // A new ring spawns every k_ShockwaveInterval seconds
            int maxRing = Mathf.FloorToInt(age / k_ShockwaveInterval);
            // Check last few rings that could still be alive
            for (int ri = Mathf.Max(0, maxRing - 2); ri <= maxRing; ri++)
            {
                float ringBirth = ri * k_ShockwaveInterval;
                float ringAge = age - ringBirth;
                if (ringAge < 0f || ringAge > k_ShockwaveLifetime) continue;

                float t = ringAge / k_ShockwaveLifetime;
                float radius = Mathf.Lerp(k_ShockwaveStartRadius, k_ShockwaveEndRadius, t);
                float ringFade = (1f - t) * fade;

                Color c = baseColor;
                c.a = ringFade * 0.6f;

                // Draw with reduced segments for perf (shockwave is thinner)
                float halfThick = k_ShockwaveThickness * 0.5f;
                float innerR = radius - halfThick;
                float outerR = radius + halfThick;
                int segments = 48;

                GL.Begin(GL.QUADS);
                GL.Color(c);

                for (int i = 0; i < segments; i++)
                {
                    float a1 = (float)i / segments * Mathf.PI * 2f;
                    float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                    float cos1 = Mathf.Cos(a1), sin1 = Mathf.Sin(a1);
                    float cos2 = Mathf.Cos(a2), sin2 = Mathf.Sin(a2);

                    Vector3 inner1 = center + new Vector3(cos1 * innerR, 0.5f, sin1 * innerR);
                    Vector3 outer1 = center + new Vector3(cos1 * outerR, 0.5f, sin1 * outerR);
                    Vector3 inner2 = center + new Vector3(cos2 * innerR, 0.5f, sin2 * innerR);
                    Vector3 outer2 = center + new Vector3(cos2 * outerR, 0.5f, sin2 * outerR);

                    GL.Vertex(inner1);
                    GL.Vertex(outer1);
                    GL.Vertex(outer2);
                    GL.Vertex(inner2);
                }

                GL.End();
            }
        }

        void DrawGroundCross(Vector3 center, Color color)
        {
            // Four arms at 45-degree angles (X shape)
            float[] angles = { 45f, 135f, 225f, 315f };

            GL.Begin(GL.QUADS);
            GL.Color(color);

            foreach (float angleDeg in angles)
            {
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x); // perpendicular on ground

                Vector3 basePos = center + Vector3.up * 0.5f;
                Vector3 tipPos = basePos + dir * k_CrossLength;

                Vector3 baseLeft = basePos - perp * (k_CrossWidthBase * 0.5f);
                Vector3 baseRight = basePos + perp * (k_CrossWidthBase * 0.5f);
                Vector3 tipLeft = tipPos - perp * (k_CrossWidthTip * 0.5f);
                Vector3 tipRight = tipPos + perp * (k_CrossWidthTip * 0.5f);

                GL.Vertex(baseLeft);
                GL.Vertex(baseRight);
                GL.Vertex(tipRight);
                GL.Vertex(tipLeft);
            }

            GL.End();
        }

        void DrawBeacon(Vector3 center, Color color)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Billboard: face camera using camera's right vector
            Vector3 right = cam.transform.right;

            // Draw as a quad strip from bottom to top, tapering
            int segments = 8;
            Color topColor = new Color(color.r, color.g, color.b, 0f);

            GL.Begin(GL.QUADS);

            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;

                float y0 = 0.5f + t0 * k_BeaconHeight;
                float y1 = 0.5f + t1 * k_BeaconHeight;

                float w0 = Mathf.Lerp(k_BeaconWidthBase, k_BeaconWidthTop, t0) * 0.5f;
                float w1 = Mathf.Lerp(k_BeaconWidthBase, k_BeaconWidthTop, t1) * 0.5f;

                Vector3 base0 = center + Vector3.up * y0;
                Vector3 base1 = center + Vector3.up * y1;

                Color c0 = Color.Lerp(color, topColor, t0);
                Color c1 = Color.Lerp(color, topColor, t1);

                GL.Color(c0);
                GL.Vertex(base0 - right * w0);
                GL.Vertex(base0 + right * w0);
                GL.Color(c1);
                GL.Vertex(base1 + right * w1);
                GL.Vertex(base1 - right * w1);
            }

            GL.End();
        }

        // ---------------------------------------------------------------
        // OnRenderObject: two-pass rendering
        // ---------------------------------------------------------------

        void OnRenderObject()
        {
            if (m_Pings.Count == 0) return;

            EnsureGlMaterials();
            float now = UnityEngine.Time.realtimeSinceStartup;

            // Pass 1: Glow material (additive) for shockwaves
            m_GlowMaterial.SetPass(0);
            foreach (var ping in m_Pings)
            {
                float age = now - ping.SpawnTime;
                float fade = 1f - Mathf.Clamp01(age / k_Duration);
                Color c = ComputePingColor(ping.Color, age, fade);
                DrawShockwaves(ping.Position, c, age, fade);
            }

            // Pass 2: Alpha material for ring, cross, beacon
            m_GlMaterial.SetPass(0);
            foreach (var ping in m_Pings)
            {
                float age = now - ping.SpawnTime;
                float fade = 1f - Mathf.Clamp01(age / k_Duration);
                Color c = ComputePingColor(ping.Color, age, fade);

                DrawThickRing(ping.Position, k_RingRadius, k_RingThickness, c);
                DrawGroundCross(ping.Position, c);
                DrawBeacon(ping.Position, c);
            }
        }

        // ---------------------------------------------------------------
        // OnGUI: labels, edge indicators, minimap markers
        // ---------------------------------------------------------------

        void OnGUI()
        {
            if (m_Pings.Count == 0) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            float now = UnityEngine.Time.realtimeSinceStartup;

            foreach (var ping in m_Pings)
            {
                float age = now - ping.SpawnTime;
                float fade = 1f - Mathf.Clamp01(age / k_Duration);

                Vector3 screenPos = cam.WorldToScreenPoint(ping.Position);
                bool onScreen = screenPos.z > 0 &&
                                screenPos.x >= 0 && screenPos.x <= Screen.width &&
                                screenPos.y >= 0 && screenPos.y <= Screen.height;

                if (onScreen)
                {
                    DrawLabel(screenPos, ping.SenderName, ping.Color, fade, age);
                }
                else
                {
                    DrawEdgeIndicator(screenPos, ping.Color, fade, age);
                }
            }

            DrawMinimapMarkers(now);
        }

        void DrawLabel(Vector3 screenPos, string name, Color color, float fade, float age)
        {
            float guiY = Screen.height - screenPos.y;

            Color boosted = Color.white;
            boosted.a = fade;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = k_LabelFontSize;
            style.fontStyle = UnityEngine.FontStyle.Bold;
            style.alignment = UnityEngine.TextAnchor.MiddleCenter;
            style.normal.textColor = boosted;

            GUIContent content = new GUIContent(name);
            Vector2 size = style.CalcSize(content);

            // Dark background rect for readability
            float pad = 6f;
            Rect bgRect = new Rect(screenPos.x - size.x / 2f - pad, guiY - 35f - size.y - pad, size.x + pad * 2f, size.y + pad * 2f);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.7f * fade);
            GUI.Box(bgRect, GUIContent.none);
            GUI.backgroundColor = prevBg;

            Rect textRect = new Rect(screenPos.x - size.x / 2f, guiY - 35f - size.y, size.x, size.y);
            GUI.Label(textRect, content, style);
        }

        void DrawEdgeIndicator(Vector3 screenPos, Color color, float fade, float age)
        {
            // If behind camera, flip
            if (screenPos.z < 0)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 dir = new Vector2(screenPos.x - center.x, screenPos.y - center.y).normalized;

            float margin = 40f;

            // Walk from center along direction until hitting edge
            float tX = dir.x != 0 ? ((dir.x > 0 ? Screen.width - margin : margin) - center.x) / dir.x : float.MaxValue;
            float tY = dir.y != 0 ? ((dir.y > 0 ? Screen.height - margin : margin) - center.y) / dir.y : float.MaxValue;
            float t = Mathf.Min(Mathf.Abs(tX), Mathf.Abs(tY));

            float edgeX = center.x + dir.x * t;
            float edgeY = center.y + dir.y * t;

            // Convert to GUI coordinates (flip Y)
            float guiY = Screen.height - edgeY;
            float guiX = edgeX;

            // Pulsing size
            float pulse = 0.8f + 0.2f * Mathf.Sin(age * k_PulseSpeed * Mathf.PI * 2f);
            float size = k_EdgeIndicatorSize * pulse;

            Color boosted = BoostColor(color, 0.3f, 0.4f);
            boosted.a = fade;

            // Black border square
            Color borderColor = new Color(0f, 0f, 0f, fade * 0.9f);
            DrawSquare(guiX, guiY, size + 4f, borderColor);
            // Colored fill square
            DrawSquare(guiX, guiY, size, boosted);

            // Directional dot: small square offset in the direction of the ping
            Vector2 guiDir = new Vector2(dir.x, -dir.y); // flip Y for GUI
            float dotOffset = size * 0.8f;
            float dotX = guiX + guiDir.x * dotOffset;
            float dotY = guiY + guiDir.y * dotOffset;
            DrawSquare(dotX, dotY, 6f, boosted);
        }

        void DrawSquare(float x, float y, float size, Color color)
        {
            Texture2D tex = Texture2D.whiteTexture;
            Color prev = GUI.color;
            GUI.color = color;
            float half = size / 2f;
            GUI.DrawTexture(new Rect(x - half, y - half, size, size), tex);
            GUI.color = prev;
        }

        // ---------------------------------------------------------------
        // Minimap
        // ---------------------------------------------------------------

        void TryFindMinimapCamera(float now)
        {
            if (m_MinimapCameraSearched && now - m_LastMinimapSearchTime < k_MinimapSearchInterval)
                return;

            m_LastMinimapSearchTime = now;
            m_MinimapCameraSearched = true;
            m_MinimapCamera = null;

            foreach (Camera cam in Camera.allCameras)
            {
                if (cam == Camera.main) continue;
                // Look for orthographic camera or one targeting a RenderTexture
                if (cam.orthographic || cam.targetTexture != null)
                {
                    m_MinimapCamera = cam;
                    return;
                }
            }
        }

        bool TryGetMinimapScreenPos(Vector3 worldPos, out Vector2 screenPos)
        {
            screenPos = Vector2.zero;

            // Tier 1: Use discovered minimap camera
            if (m_MinimapCamera != null)
            {
                Vector3 vp = m_MinimapCamera.WorldToViewportPoint(worldPos);
                if (vp.x >= 0f && vp.x <= 1f && vp.z > 0f)
                {
                    Rect pixelRect = m_MinimapCamera.pixelRect;
                    screenPos = new Vector2(
                        pixelRect.x + vp.x * pixelRect.width,
                        pixelRect.y + vp.y * pixelRect.height
                    );
                    return true;
                }
            }

            // Tier 2: Fallback using world_size
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return false;

            Logic.Point ws = game.world_size;
            if (ws.x <= 0f || ws.y <= 0f) return false;

            float normX = worldPos.x / ws.x;
            float normZ = worldPos.z / ws.y;

            if (normX < 0f || normX > 1f || normZ < 0f || normZ > 1f) return false;

            Rect minimapRect = GetEstimatedMinimapRect();
            screenPos = new Vector2(
                minimapRect.x + normX * minimapRect.width,
                minimapRect.y + (1f - normZ) * minimapRect.height // flip Z for screen
            );
            return true;
        }

        Rect GetEstimatedMinimapRect()
        {
            // Estimate minimap as bottom-left, ~18% of shorter screen dimension
            float dim = Mathf.Min(Screen.width, Screen.height) * 0.18f;
            float margin = 10f;
            return new Rect(margin, Screen.height - dim - margin, dim, dim);
        }

        void DrawMinimapMarkers(float now)
        {
            foreach (var ping in m_Pings)
            {
                float age = now - ping.SpawnTime;
                float fade = 1f - Mathf.Clamp01(age / k_Duration);

                Vector2 mmPos;
                if (TryGetMinimapScreenPos(ping.Position, out mmPos))
                {
                    DrawMinimapPingMarker(mmPos, ping.Color, fade, age);
                }
            }
        }

        void DrawMinimapPingMarker(Vector2 screenPos, Color color, float fade, float age)
        {
            // Convert screen pos to GUI coordinates (flip Y)
            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // Boost brightness extra for minimap visibility
            Color boosted = BoostColor(color, 0.3f, 0.5f);

            // Pulsing size
            float pulse = 0.7f + 0.3f * Mathf.Sin(age * k_MinimapPulseSpeed * Mathf.PI * 2f);
            float size = k_MinimapMarkerSize * pulse;

            boosted.a = fade;

            // Black border for contrast
            DrawSquare(guiX, guiY, size + 4f, new Color(0f, 0f, 0f, fade * 0.9f));
            // Bright fill
            DrawSquare(guiX, guiY, size, boosted);
        }

        // ---------------------------------------------------------------
        // Utility
        // ---------------------------------------------------------------

        static Color BoostColor(Color color, float satBoost, float valBoost)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            s = Mathf.Clamp01(s + satBoost);
            v = Mathf.Clamp01(v + valBoost);
            return Color.HSVToRGB(h, s, v);
        }
    }
}
