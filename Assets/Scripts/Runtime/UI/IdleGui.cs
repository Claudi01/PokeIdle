using UnityEngine;

namespace PokeIdle
{
    public static class IdleGui
    {
        public static readonly Color Back = new Color32(14, 24, 31, 255);
        public static readonly Color Panel = new Color32(23, 37, 44, 255);
        public static readonly Color Card = new Color32(31, 48, 53, 255);
        public static readonly Color Edge = new Color32(66, 86, 86, 255);
        public static readonly Color Gold = new Color32(228, 187, 108, 255);
        public static readonly Color Text = new Color32(234, 237, 215, 255);
        public static readonly Color Muted = new Color32(150, 175, 174, 255);
        public static readonly Color Green = new Color32(123, 204, 131, 255);
        public static readonly Color Red = new Color32(224, 116, 100, 255);
        public static GUIStyle Small, Body, Title, ButtonStyle, Wrapped;
        private static Texture2D buttonTexture, hoverTexture, activeTexture;

        public static void Ensure()
        {
            if (Small != null) return;
            Small = Label(11, false, Muted);
            Body = Label(12, false, Text);
            Title = Label(16, true, Gold);
            Wrapped = new GUIStyle(Body) { wordWrap = true };
            buttonTexture = Texture(Card);
            hoverTexture = Texture(new Color32(52, 76, 77, 255));
            activeTexture = Texture(new Color32(78, 93, 73, 255));
            ButtonStyle = new GUIStyle(Body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 2, 2), clipping = TextClipping.Clip };
            ButtonStyle.normal.background = buttonTexture;
            ButtonStyle.hover.background = hoverTexture;
            ButtonStyle.active.background = activeTexture;
            ButtonStyle.onNormal.background = activeTexture;
        }

        private static GUIStyle Label(int size, bool bold, Color color)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0), alignment = TextAnchor.MiddleLeft };
            s.normal.textColor = color;
            return s;
        }
        private static Texture2D Texture(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply(); return t;
        }
        public static void Fill(Rect r, Color c)
        {
            Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }
        public static void Box(Rect r, Color c, bool active = false)
        {
            Fill(r, active ? Gold : Edge);
            Fill(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), c);
        }
        public static bool Button(Rect r, string text, bool enabled = true, bool active = false)
        {
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            Box(r, active ? Edge : Card, active);
            bool clicked = GUI.Button(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), text, ButtonStyle);
            GUI.enabled = previous; return clicked;
        }
        public static void Bar(Rect r, int hp, int maximum, Color color)
        {
            Fill(r, Back);
            Fill(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01((float)hp / Mathf.Max(1, maximum)), r.height - 2), color);
        }
        public static void Sprite(Rect r, Sprite sprite)
        {
            if (sprite == null) return;
            Rect source = sprite.rect;
            float ratio = source.width / source.height;
            float w = Mathf.Min(r.width, r.height * ratio), h = w / ratio;
            GUI.DrawTextureWithTexCoords(new Rect(r.center.x - w / 2, r.center.y - h / 2, w, h), sprite.texture,
                new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
                    source.width / sprite.texture.width, source.height / sprite.texture.height));
        }
        public static string TypeName(ElementalType type)
        {
            string[] names = { "Normal", "Fogo", "Água", "Planta", "Elétrico", "Gelo", "Lutador", "Veneno", "Terra",
                "Voador", "Psíquico", "Inseto", "Pedra", "Fantasma", "Dragão", "Sombrio", "Aço", "Fada" };
            return names[(int)type];
        }
    }
}
