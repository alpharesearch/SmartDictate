using System;
using System.Runtime.InteropServices;
using System.Windows.Forms; // For Keys enum and potentially message handling if in a Form
namespace WhisperNetConsoleDemo;

public class GlobalHotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000; // Unique ID for our hotkey
    private IntPtr _hWnd; // Handle of the window to receive hotkey messages

    // Modifiers for hotkey (e.g., Alt, Ctrl, Shift, Win)
    public enum FsModifiers : uint
    {
        None = 0,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Window = 0x0008
    }

    public event Action? HotKeyPressed;

    // If integrating into MainForm, _hWnd can be this.Handle
    // If a separate class, it needs a window handle (can be a hidden message-only window)
    public GlobalHotkeyService(IntPtr? windowHandle = null)
    {
        // If no handle is provided, we might need to create a hidden message window.
        // For now, let's assume it will be integrated or used with MainForm's handle.
        _hWnd = windowHandle ?? IntPtr.Zero;
        // If windowHandle is null, RegisterHotKey might register a thread-specific hotkey
        // or fail if a window handle is strictly required.
        // A robust solution often involves a NativeWindow for message handling.
    }

    public bool Register(FsModifiers modifier, Keys key)
    {
        if (_hWnd == IntPtr.Zero && Application.OpenForms.Count > 0)
        {
            _hWnd = Application.OpenForms[0].Handle; // Try to get main form's handle if not provided
        }
        if (_hWnd == IntPtr.Zero)
        {
            // Cannot register without a window handle for application-wide hotkey
            // Or create a NativeWindow to listen for messages.
            Console.WriteLine("ERROR: Cannot register global hotkey without a window handle.");
            return false;
        }
        return RegisterHotKey(_hWnd, HOTKEY_ID, (uint)modifier, (uint)key);
    }

    public void Unregister()
    {
        if (_hWnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hWnd, HOTKEY_ID);
        }
    }

    // This method would be called from the window's WndProc that handles WM_HOTKEY
    public void ProcessHotKeyMessage(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_ID)
        {
            HotKeyPressed?.Invoke();
        }
    }

    public void Dispose()
    {
        Unregister();
    }
}
