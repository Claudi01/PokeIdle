using System.Collections;
using UnityEngine;

namespace PokeIdle
{
    public sealed class TaskbarIntegration : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;
        [SerializeField, Min(320)] private int fallbackWidth = 720;
        [SerializeField, Min(48)] private int fallbackHeight = 96;
        [SerializeField, Min(160)] private int expandedHeight = 360;
        [SerializeField] private bool allowWindowDrag = true;
        [SerializeField, Min(1)] private int dragHandleHeight = 22;

        public static TaskbarIntegration Instance { get; private set; }
        private int collapsedWindowHeight;

        private void Start()
        {
            Instance = this;
            if (applyOnStart)
            {
                StartCoroutine(ApplyOnNextFrame());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!allowWindowDrag || !Input.GetMouseButtonDown(0) || Screen.height <= dragHandleHeight)
            {
                return;
            }

            // A faixa superior esquerda fica livre para arrastar a janela sem bloquear os botoes.
            bool isDragHandle = Input.mousePosition.y >= Screen.height - dragHandleHeight && Input.mousePosition.x < Screen.width - 280f;
            if (isDragHandle)
            {
                BeginWindowDrag();
            }
#endif
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
        private const uint WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 2;

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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr SendMessage(System.IntPtr hWnd, uint message, System.IntPtr wParam, System.IntPtr lParam);

        private void ApplyWindowsTaskbarLayout()
        {
            System.IntPtr windowHandle = GetActiveWindow();
            if (windowHandle == System.IntPtr.Zero)
            {
                Debug.LogWarning("TaskbarIntegration: não foi possível obter a janela do jogo.");
                return;
            }

            System.IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            Rect taskbarRect = new Rect();
            bool hasTaskbarRect = taskbarHandle != System.IntPtr.Zero && GetWindowRect(taskbarHandle, out taskbarRect);

            int availableWidth = hasTaskbarRect ? taskbarRect.Right - taskbarRect.Left : Screen.currentResolution.width;
            int width = Mathf.Min(fallbackWidth, Mathf.Max(320, availableWidth));
            int height = Mathf.Max(48, fallbackHeight);
            int x = hasTaskbarRect
                ? taskbarRect.Left + Mathf.Max(0, (availableWidth - width) / 2)
                : Mathf.Max(0, (Screen.currentResolution.width - width) / 2);
            int y = hasTaskbarRect
                ? taskbarRect.Top - height
                : Screen.currentResolution.height - height;
            collapsedWindowHeight = height;

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

        private void BeginWindowDrag()
        {
            System.IntPtr windowHandle = GetActiveWindow();
            if (windowHandle == System.IntPtr.Zero)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(windowHandle, WM_NCLBUTTONDOWN, new System.IntPtr(HTCAPTION), System.IntPtr.Zero);
        }

        private void ResizeWindowsTaskbarWindow(bool expanded)
        {
            System.IntPtr windowHandle = GetActiveWindow();
            Rect currentRect;
            if (windowHandle == System.IntPtr.Zero || !GetWindowRect(windowHandle, out currentRect))
            {
                return;
            }

            int width = currentRect.Right - currentRect.Left;
            int bottom = currentRect.Bottom;
            int targetHeight = expanded
                ? Mathf.Max(expandedHeight, collapsedWindowHeight)
                : Mathf.Max(48, collapsedWindowHeight);

            Screen.SetResolution(width, targetHeight, FullScreenMode.Windowed);
            SetWindowPos(windowHandle, HWND_TOPMOST, currentRect.Left, bottom - targetHeight, width, targetHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
#endif

        public void SetMenuExpanded(bool expanded)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ResizeWindowsTaskbarWindow(expanded);
#endif
        }
    }
}
