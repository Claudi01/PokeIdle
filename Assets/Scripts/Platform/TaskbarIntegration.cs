using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace PokeIdle
{
    public sealed class TaskbarIntegration : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;
        [SerializeField, Min(320)] private int fallbackWidth = TaskbarLayout.Width;
        [SerializeField, Min(160)] private int fallbackHeight = TaskbarLayout.StripHeight;
        [SerializeField, Min(600)] private int expandedHeight = TaskbarLayout.StripHeight + TaskbarLayout.MenuHeight;
        [SerializeField] private bool allowWindowDrag = true;
        [SerializeField, Min(8)] private int dragHandleHeight = 22;
        public static TaskbarIntegration Instance { get; private set; }
        public int CompactHeight { get { return Mathf.Max(TaskbarLayout.StripHeight, fallbackHeight); } }
        public int ExpandedHeight { get { return Mathf.Max(expandedHeight, CompactHeight + TaskbarLayout.MenuHeight + 4); } }
        public bool MenuExpanded { get; private set; }
        public bool IsResizePending { get; private set; }

        private void Awake() { Instance = this; }
        private void Start() { if (applyOnStart) Apply(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public Rect GetGameplayViewport() { return TaskbarLayout.CameraViewport(Screen.width, Screen.height); }

        public bool IsWindowDragPoint(Vector2 point)
        {
            if (!allowWindowDrag || IsResizePending) return false;
            return TaskbarLayout.DragHandle(Screen.width, Screen.height, dragHandleHeight).Contains(point);
        }

        private void Update()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Vector2 mousePosition;
            if (TryGetLeftMousePress(out mousePosition) && IsWindowDragPoint(mousePosition))
            {
                IntPtr handle = GetOwnedWindow();
                if (handle == IntPtr.Zero) return;
                ReleaseCapture();
                SendMessage(handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static bool TryGetLeftMousePress(out Vector2 screenPointFromTopLeft)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                screenPointFromTopLeft = Vector2.zero;
                return false;
            }

            Vector2 position = mouse.position.ReadValue();
            screenPointFromTopLeft = new Vector2(position.x, Screen.height - position.y);
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (!Input.GetMouseButtonDown(0))
            {
                screenPointFromTopLeft = Vector2.zero;
                return false;
            }

            Vector3 position = Input.mousePosition;
            screenPointFromTopLeft = new Vector2(position.x, Screen.height - position.y);
            return true;
#else
            screenPointFromTopLeft = Vector2.zero;
            return false;
#endif
        }
#endif

        public void Apply()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            StartLayout(true);
#endif
        }

        public void SetMenuExpanded(bool expanded)
        {
            MenuExpanded = expanded;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            StartLayout(false);
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private Coroutine resizeRoutine;
        private IntPtr ownedWindow;
        private static readonly IntPtr Topmost = new IntPtr(-1);
        private const uint FrameFlags = 0x0010 | 0x0040 | 0x0020;
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
        private delegate bool WindowCallback(IntPtr handle, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessId();
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, StringBuilder name, int max);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int w, int h, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetLong64(IntPtr handle, int index);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetLong32(IntPtr handle, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetLong64(IntPtr handle, int index, IntPtr value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetLong32(IntPtr handle, int index, int value);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr w, IntPtr l);

        private IntPtr GetOwnedWindow()
        {
            uint process = GetCurrentProcessId();
            uint owner;
            if (ownedWindow != IntPtr.Zero && IsWindow(ownedWindow))
            {
                GetWindowThreadProcessId(ownedWindow, out owner);
                if (owner == process) return ownedWindow;
            }
            ownedWindow = IntPtr.Zero;
            EnumWindows((handle, parameter) =>
            {
                uint candidateProcess;
                GetWindowThreadProcessId(handle, out candidateProcess);
                if (candidateProcess != process) return true;
                var name = new StringBuilder(128);
                GetClassName(handle, name, name.Capacity);
                if (name.ToString() != "UnityWndClass") return true;
                ownedWindow = handle;
                return false;
            }, IntPtr.Zero);
            return ownedWindow;
        }

        private void StartLayout(bool initial)
        {
            if (resizeRoutine != null) StopCoroutine(resizeRoutine);
            resizeRoutine = StartCoroutine(ResizeWindow(initial));
        }

        private IEnumerator ResizeWindow(bool initial)
        {
            IsResizePending = true;
            IntPtr handle = GetOwnedWindow();
            for (int i = 0; handle == IntPtr.Zero && i < 90; i++)
            {
                yield return null;
                handle = GetOwnedWindow();
            }
            if (handle == IntPtr.Zero)
            {
                Debug.LogWarning("PokeIdle: janela do proprio processo nao encontrada.");
                IsResizePending = false;
                yield break;
            }
            var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            NativeRect work = new NativeRect { Right = Screen.currentResolution.width, Bottom = Screen.currentResolution.height };
            if (GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) work = info.Work;
            NativeRect before;
            GetWindowRect(handle, out before);
            int width = Mathf.Min(Mathf.Max(320, fallbackWidth), work.Right - work.Left);
            int height = Mathf.Min(MenuExpanded ? ExpandedHeight : CompactHeight, work.Bottom - work.Top);
            int x = initial ? work.Left + (work.Right - work.Left - width) / 2 : before.Left;
            int bottom = initial ? work.Bottom : before.Bottom;
            x = Mathf.Clamp(x, work.Left, work.Right - width);
            bottom = Mathf.Clamp(bottom, work.Top + height, work.Bottom);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            // Unity applies resolution at frame end and may restore the window style then.
            for (int i = 0; i < 3; i++) yield return null;
            for (int pass = 0; pass < 2; pass++)
            {
                handle = GetOwnedWindow();
                if (handle == IntPtr.Zero) break;
                long style = IntPtr.Size == 8 ? GetLong64(handle, -16).ToInt64() : GetLong32(handle, -16);
                style = (style & ~0x00CF0000L) | 0x80000000L;
                if (IntPtr.Size == 8) SetLong64(handle, -16, new IntPtr(style));
                else SetLong32(handle, -16, unchecked((int)style));
                if (!SetWindowPos(handle, Topmost, x, bottom - height, width, height, FrameFlags))
                    Debug.LogWarning("PokeIdle: falha ao posicionar janela: " + Marshal.GetLastWin32Error());
                yield return null;
            }
            IsResizePending = false;
            resizeRoutine = null;
        }
#endif
    }
}
