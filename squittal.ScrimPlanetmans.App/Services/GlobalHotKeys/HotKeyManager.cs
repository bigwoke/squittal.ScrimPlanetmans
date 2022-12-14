using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;

namespace squittal.ScrimPlanetmans.App.Services.GlobalHotKeys
{
    // Credit to https://stackoverflow.com/a/3654821 for much of this
    public static class HotKeyManager
    {
        private static readonly Dictionary<int, Action> _hotkeys = new Dictionary<int, Action>();
        private static readonly ManualResetEvent _windowReadyEvent = new ManualResetEvent(false);
        private static volatile MessageWindow _wnd;
        private static volatile IntPtr _hwnd;

        static HotKeyManager()
        {
            Thread messageLoop = new Thread(() => Application.Run(new MessageWindow()))
            {
                Name = "MessageLoopThread",
                IsBackground = true
            };

            messageLoop.Start();
        }

        private delegate void RegisterDelegate(IntPtr hwnd, int id, uint modifiers, uint key);
        private delegate void UnregisterDelegate(IntPtr hwnd, int id);

        public static int Add(Keys key, KeyModifiers modifiers, Action action)
        {
            int id = (int)key + (int)modifiers * 0x10000;

            if (!_hotkeys.ContainsKey(id))
            {
                _windowReadyEvent.WaitOne();
                _wnd.Invoke(new RegisterDelegate(Register), _hwnd, id, (uint)modifiers, (uint)key);
                _hotkeys.Add(id, action);

                return id;
            }

            return -1;
        }

        public static void Remove(int id)
        {
            _wnd.Invoke(new UnregisterDelegate(Unregister), _hwnd, id);
            _hotkeys.Remove(id);
        }

        private static void Register(IntPtr hwnd, int id, uint mod, uint key)
        {
            if (!RegisterHotKey(hwnd, id, mod, key))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static void Unregister(IntPtr hwnd, int id)
        {
            if (!UnregisterHotKey(hwnd, id))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static void RunHotKeyAction(int id) => _hotkeys[id]();

        [DllImport("user32", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class MessageWindow : Form
        {
            private const int WM_HOTKEY = 0x0312;

            public MessageWindow()
            {
                _wnd = this;
                _hwnd = Handle;
                _windowReadyEvent.Set();
            }

            protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    RunHotKeyAction(m.WParam.ToInt32());
                }

                base.WndProc(ref m);
            }
        }
    }

    [Flags]
    public enum KeyModifiers
    {
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Windows = 0x0008,
        NoRepeat = 0x4000
    }
}