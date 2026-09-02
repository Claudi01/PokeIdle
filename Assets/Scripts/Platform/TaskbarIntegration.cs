using System.Collections;
using UnityEngine;

namespace PokeIdle
{
    public sealed class TaskbarIntegration : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;
        [SerializeField, Min(1)] private int fallbackHeight = 48;

        private void Start()
        {
            if (applyOnStart)
            {
                StartCoroutine(ApplyOnNextFrame());
            }
        }

        public void Apply()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ApplyWindowsTaskbarLayout();
#else
            Debug.Log("TaskbarIntegration: a integração com a taskbar será aplicada apenas no build Windows.");
#endif
        }

        private IEnumerator ApplyOnNextFrame()
        {
            yield return null;
            Apply();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_POPUP = 0x80000000L;
        private const long WS_OVERLAPPEDWINDOW = 0x00CF0000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const long WS_EX_APPWINDOW = 0x00040000L;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_SHOWNOACTIVATE = 4;

        private static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
        private static readonly System.IntPtr HWND_TOP = new System.IntPtr(0);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern System.IntPtr GetWindowLongPtr64(System.IntPtr hWnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        private static extern int GetWindowLong32(System.IntPtr hWnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern System.IntPtr SetWindowLongPtr64(System.IntPtr hWnd, int index, System.IntPtr value);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern int SetWindowLong32(System.IntPtr hWnd, int index, int value);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern System.IntPtr FindWindow(string className, string windowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(System.IntPtr hWnd, out Rect rect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(System.IntPtr hWnd, int command);

        private void ApplyWindowsTaskbarLayout()
        {
            System.IntPtr windowHandle = GetActiveWindow();
            if (windowHandle == System.IntPtr.Zero)
            {
                Debug.LogWarning("TaskbarIntegration: não foi possível obter a janela do jogo.");
                return;
            }

            System.IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            Rect taskbarRect;
            bool hasTaskbarRect = taskbarHandle != System.IntPtr.Zero && GetWindowRect(taskbarHandle, out taskbarRect);

            int x = hasTaskbarRect ? taskbarRect.Left : 0;
            int y = hasTaskbarRect ? taskbarRect.Top : Screen.currentResolution.height - fallbackHeight;
            int width = hasTaskbarRect ? taskbarRect.Right - taskbarRect.Left : Screen.currentResolution.width;
            int height = hasTaskbarRect ? taskbarRect.Bottom - taskbarRect.Top : fallbackHeight;

            long style = GetWindowLong(windowHandle, GWL_STYLE).ToInt64();
            style = (style & ~WS_OVERLAPPEDWINDOW) | WS_POPUP;
            SetWindowLong(windowHandle, GWL_STYLE, new System.IntPtr(style));

            long extendedStyle = GetWindowLong(windowHandle, GWL_EXSTYLE).ToInt64();
            extendedStyle = (extendedStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
            SetWindowLong(windowHandle, GWL_EXSTYLE, new System.IntPtr(extendedStyle));

            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            SetWindowPos(windowHandle, HWND_TOPMOST, x, y, width, height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
            ShowWindow(windowHandle, SW_SHOWNOACTIVATE);
        }

        private static System.IntPtr GetWindowLong(System.IntPtr handle, int index)
        {
            return System.IntPtr.Size == 8 ? GetWindowLongPtr64(handle, index) : new System.IntPtr(GetWindowLong32(handle, index));
        }

        private static System.IntPtr SetWindowLong(System.IntPtr handle, int index, System.IntPtr value)
        {
            return System.IntPtr.Size == 8 ? SetWindowLongPtr64(handle, index, value) : new System.IntPtr(SetWindowLong32(handle, index, value.ToInt32()));
        }
#endif
    }
}
