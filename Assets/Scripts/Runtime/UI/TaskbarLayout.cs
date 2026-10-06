using UnityEngine;

namespace PokeIdle
{
    // Shared pixel contract: the arena never grows when the menu opens.
    public static class TaskbarLayout
    {
        public const int Width = 720;
        public const int StripHeight = 160;
        public const int MenuHeight = 440;
        public const int RailWidth = 176;
        public static Rect Strip(float width, float height)
        {
            float w = Mathf.Min(Width, width);
            float h = Mathf.Min(StripHeight, height);
            return new Rect((width - w) * 0.5f, height - h, w, h);
        }
        public static Rect Arena(float width, float height)
        {
            Rect strip = Strip(width, height);
            return new Rect(strip.x + 3, strip.y + 3, Mathf.Max(1, strip.width - RailWidth - 6), strip.height - 6);
        }
        public static Rect DragHandle(float width, float height, float handleHeight)
        {
            Rect arena = Arena(width, height);
            return new Rect(arena.x + 8, arena.y, Mathf.Max(1, arena.width - 16), Mathf.Clamp(handleHeight, 8, 24));
        }
        public static Rect Menu(float width, float height)
        {
            Rect strip = Strip(width, height);
            float h = Mathf.Min(MenuHeight, Mathf.Max(0, strip.y - 4));
            return new Rect(strip.x, strip.y - h - 4, strip.width, h);
        }
        public static Rect CameraViewport(float width, float height)
        {
            Rect a = Arena(width, height);
            return new Rect(a.x / width, (height - a.yMax) / height, a.width / width, a.height / height);
        }
    }
}
