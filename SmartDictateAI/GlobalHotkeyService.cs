using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WhisperNetConsoleDemo
{
    public class GlobalHotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _hWnd;

        // BACK-COMPAT: keep your original default ID for "the" hotkey
        private const int DEFAULT_HOTKEY_ID = 9000;

        public enum FsModifiers : uint
        {
            None = 0,
            Alt = 0x0001,
            Control = 0x0002,
            Shift = 0x0004,
            Window = 0x0008
        }

        //BACK-COMPAT: bring back event expected by MainForm
        public event Action? HotKeyPressed;

        // Optional: ID-aware event if you ever want it
        public event Action<int>? HotKeyPressedWithId;

        // hotkeyId -> callback
        private readonly Dictionary<int, Action> _handlers = new();

        public GlobalHotkeyService(IntPtr? windowHandle = null)
        {
            _hWnd = windowHandle ?? IntPtr.Zero;
        }

        //BACK-COMPAT: old signature used by your MainForm today
        // Registers DEFAULT_HOTKEY_ID and triggers HotKeyPressed when pressed.
        public bool Register(FsModifiers modifier, Keys key)
        {
            return Register(DEFAULT_HOTKEY_ID, modifier, key, () =>
            {
                HotKeyPressed?.Invoke();                 
            });
        }

        // New: multi-hotkey registration
        public bool Register(int hotkeyId, FsModifiers modifier, Keys key, Action handler)
        {
            if (_hWnd == IntPtr.Zero && Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm != null)
                    _hWnd = mainForm.Handle;
            }

            if (_hWnd == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: Cannot register global hotkey without a window handle.");
                return false;
            }

            _handlers[hotkeyId] = handler;

            var ok = RegisterHotKey(_hWnd, hotkeyId, (uint)modifier, (uint)key);
            if (!ok)
            {
                _handlers.Remove(hotkeyId);
            }
            return ok;
        }

        public void Unregister(int hotkeyId)
        {
            if (_hWnd != IntPtr.Zero)
                UnregisterHotKey(_hWnd, hotkeyId);

            _handlers.Remove(hotkeyId);
        }

        public void UnregisterAll()
        {
            foreach (var id in new List<int>(_handlers.Keys))
                Unregister(id);
        }

        // Call from MainForm.WndProc when WM_HOTKEY arrives

        public void ProcessHotKeyMessage(int hotkeyId)
        {
            // Raise ID-aware event once (optional)
            HotKeyPressedWithId?.Invoke(hotkeyId);

            // Invoke registered handler (single source of truth)
            if (_handlers.TryGetValue(hotkeyId, out var handler))
                handler?.Invoke();
        }


        public void Dispose()
        {
            UnregisterAll();
        }
    }
}
