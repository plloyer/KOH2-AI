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

        const float k_Duration = 10f;
        const float k_RingRadius = 8f;
        const int k_RingSegments = 48;
        const float k_BeaconHeight = 60f;
        const float k_PulseSpeed = 2f;

        readonly List<PingData> m_Pings = new List<PingData>();
        Material m_GlMaterial;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (m_GlMaterial != null) Destroy(m_GlMaterial);
        }

        void Update()
        {
            // Remove expired pings
            float now = UnityEngine.Time.time;
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

            // Try physics raycast first
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 5000f))
            {
                hitPos = hit.point;
                return true;
            }

            // Fallback: intersect with y=0 plane
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
            // Skip if this is our own ping (we already display it locally)
            Logic.Kingdom local = GetLocalKingdom();
            if (local != null && local.Name == senderName) return;

            // Find the sender's kingdom for color
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
                SpawnTime = UnityEngine.Time.time
            });
        }

        static Color ExtractKingdomColor(Logic.Kingdom kingdom)
        {
            // kingdom.map_color is a packed 0xRRGGBB int
            int packed = kingdom.map_color;
            float r = ((packed >> 16) & 0xFF) / 255f;
            float g = ((packed >> 8) & 0xFF) / 255f;
            float b = (packed & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }

        Logic.Kingdom GetLocalKingdom()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return null;

            var k = game.GetLocalPlayerKingdom();
            if (k != null) return k;

            // Fallback for MP client
            if (game.campaign == null) return null;
            string kingdomName = CampaignUtils.GetOwnKingdomName(game.campaign);
            if (string.IsNullOrEmpty(kingdomName)) return null;
            return game.GetKingdom(kingdomName);
        }

        void EnsureGlMaterial()
        {
            if (m_GlMaterial != null) return;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            m_GlMaterial = new Material(shader);
            m_GlMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_GlMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_GlMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m_GlMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            m_GlMaterial.SetInt("_ZWrite", 0);
            m_GlMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        void OnRenderObject()
        {
            if (m_Pings.Count == 0) return;

            EnsureGlMaterial();
            m_GlMaterial.SetPass(0);
            float now = UnityEngine.Time.time;

            foreach (var ping in m_Pings)
            {
                float age = now - ping.SpawnTime;
                float fade = 1f - Mathf.Clamp01(age / k_Duration);
                // Pulse effect: oscillate alpha
                float pulse = 0.5f + 0.5f * Mathf.Sin(age * k_PulseSpeed * Mathf.PI * 2f);
                float alpha = fade * (0.4f + 0.6f * pulse);

                Color c = ping.Color;
                c.a = alpha;

                // Draw ring
                GL.Begin(GL.LINES);
                for (int i = 0; i < k_RingSegments; i++)
                {
                    float a1 = (float)i / k_RingSegments * Mathf.PI * 2f;
                    float a2 = (float)(i + 1) / k_RingSegments * Mathf.PI * 2f;

                    Vector3 p1 = ping.Position + new Vector3(Mathf.Cos(a1) * k_RingRadius, 0.5f, Mathf.Sin(a1) * k_RingRadius);
                    Vector3 p2 = ping.Position + new Vector3(Mathf.Cos(a2) * k_RingRadius, 0.5f, Mathf.Sin(a2) * k_RingRadius);

                    GL.Color(c);
                    GL.Vertex(p1);
                    GL.Vertex(p2);
                }
                GL.End();

                // Draw vertical beacon line
                GL.Begin(GL.LINES);
                GL.Color(c);
                GL.Vertex(ping.Position + Vector3.up * 0.5f);
                GL.Color(new Color(c.r, c.g, c.b, 0f));
                GL.Vertex(ping.Position + Vector3.up * k_BeaconHeight);
                GL.End();
            }
        }

        void OnGUI()
        {
            if (m_Pings.Count == 0) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            float now = UnityEngine.Time.time;

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
                    DrawLabel(screenPos, ping.SenderName, ping.Color, fade);
                }
                else
                {
                    DrawEdgeIndicator(screenPos, ping.Color, fade, cam);
                }
            }
        }

        void DrawLabel(Vector3 screenPos, string name, Color color, float fade)
        {
            // Unity GUI y is inverted from screen y
            float guiY = Screen.height - screenPos.y;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.fontStyle = UnityEngine.FontStyle.Bold;
            style.alignment = UnityEngine.TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(color.r, color.g, color.b, fade);

            Vector2 size = style.CalcSize(new GUIContent(name));
            Rect rect = new Rect(screenPos.x - size.x / 2f, guiY - 30f - size.y, size.x, size.y);
            GUI.Label(rect, name, style);
        }

        void DrawEdgeIndicator(Vector3 screenPos, Color color, float fade, Camera cam)
        {
            // If behind camera, flip
            if (screenPos.z < 0)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            // Center of screen
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 dir = new Vector2(screenPos.x - center.x, screenPos.y - center.y).normalized;

            // Clamp to screen edge with margin
            float margin = 30f;
            float edgeX = Mathf.Clamp(screenPos.x, margin, Screen.width - margin);
            float edgeY = Mathf.Clamp(screenPos.y, margin, Screen.height - margin);

            // If clamping changed position, compute the edge point along the direction
            if (edgeX != screenPos.x || edgeY != screenPos.y)
            {
                // Walk from center along direction until hitting edge
                float tX = dir.x != 0 ? ((dir.x > 0 ? Screen.width - margin : margin) - center.x) / dir.x : float.MaxValue;
                float tY = dir.y != 0 ? ((dir.y > 0 ? Screen.height - margin : margin) - center.y) / dir.y : float.MaxValue;
                float t = Mathf.Min(Mathf.Abs(tX), Mathf.Abs(tY));

                edgeX = center.x + dir.x * t;
                edgeY = center.y + dir.y * t;
            }

            // Convert to GUI coordinates (flip Y)
            float guiY = Screen.height - edgeY;
            float guiX = edgeX;

            // Draw diamond
            float size = 12f;
            Color c = new Color(color.r, color.g, color.b, fade);
            DrawDiamond(guiX, guiY, size, c);
        }

        void DrawDiamond(float x, float y, float size, Color color)
        {
            Texture2D tex = Texture2D.whiteTexture;
            Color prev = GUI.color;
            GUI.color = color;

            // Approximate diamond with a small rotated square using a few rects
            float half = size / 2f;
            GUI.DrawTexture(new Rect(x - half, y - half, size, size), tex);

            GUI.color = prev;
        }
    }
}
