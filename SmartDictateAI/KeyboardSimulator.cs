using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // For Keys enum
using System.Text.RegularExpressions;

namespace WhisperNetConsoleDemo;

public static class KeyboardSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private static readonly Regex PlaceholderRegex = new Regex(
            @"(\[[A-Za-z _\-]+\]|\([A-Za-z _\-]+\)|\.\.\.)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

    // Define a set of known placeholders for more precise matching if needed,
    // in addition to or instead of a general regex.
    private static readonly HashSet<string> KnownPlaceholdersForDictation = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "[BLANK_AUDIO]", "[INAUDIBLE]", "[MUSIC PLAYING]", "[SOUND]", "[CLICK]",
        "(silence)", "[ Silence ]", "..."
    };

    public static void SendText(string text, bool filterPlaceholders = true, Action<string>? logger = null)
    {
        logger?.Invoke($"KeyboardSimulator.SendText called with: '{text}'");
        if (string.IsNullOrEmpty(text)) return;
        string textToSend = text;
        if (filterPlaceholders)
        {
            string tempText = textToSend.Trim();
            foreach (string placeholder in KnownPlaceholdersForDictation)
            {
                if (tempText.Equals(placeholder, StringComparison.OrdinalIgnoreCase))
                {
                    tempText = string.Empty;
                    break;
                }
            }
            textToSend = tempText.Trim();
        }

        if (string.IsNullOrWhiteSpace(textToSend)) return; // Don't send if filtering resulted in empty string

        // If text is long, prefer clipboard paste to avoid slow character-by-character SendInput
        const int CLIPBOARD_PASTE_THRESHOLD = 20;
        if (textToSend.Length > CLIPBOARD_PASTE_THRESHOLD)
        {
            logger?.Invoke($"Using clipboard paste for long text (len={textToSend.Length}).");
            var t = new Thread(() =>
            {
                IDataObject? backup = null;
                try
                {
                    backup = Clipboard.GetDataObject();
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            Clipboard.SetText(textToSend);
                            break;
                        }
                        catch (ExternalException)
                        {
                            Thread.Sleep(50); // Wait and retry if locked by another process
                        }
                    }
                    Thread.Sleep(60); // Allow clipboard to update
                    SendCtrlVWrapper();
                    Thread.Sleep(60); // Allow target app to process paste
                }
                catch (Exception ex)
                {
                    logger?.Invoke($"Clipboard paste path failed: {ex.Message}");
                }
                finally
                {
                    if (backup != null)
                    {
                        try { Clipboard.SetDataObject(backup, true); }
                        catch { /* ignore */ }
                    }
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            return;
        }

        logger?.Invoke($"Preparing to SendInput for: '{textToSend}' (Length: {textToSend.Length})");
        List<INPUT> inputs = new List<INPUT>();
        foreach (char c in textToSend)
        {
            inputs.Add(CreateKeyInput(c, false)); // KeyDown
            inputs.Add(CreateKeyInput(c, true));  // KeyUp
        }
        SendInput((uint)inputs.Count, inputs.ToArray(), INPUT.Size);
    }

    // Helper for characters that need Shift (uppercase, symbols)
    // This is a simplified version and might not handle all keyboard layouts or complex symbols perfectly.
    // For full robustness, one might need to map characters to virtual key codes + shift states.
    private static INPUT CreateKeyInput(char c, bool keyUp)
    {
        ushort scanCode = c; // For KEYEVENTF_UNICODE, wScan is the character itself

        KEYBDINPUT ki = new KEYBDINPUT
        {
            wVk = 0, // Not using virtual key codes when sending Unicode
            wScan = scanCode,
            dwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0),
            time = 0,
            dwExtraInfo = GetMessageExtraInfo()
        };

        return new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = ki } };
    }

    // Create an INPUT for a virtual-key code (used for ctrl/c/v, etc.)
    private static INPUT CreateVirtualKeyInput(ushort virtualKey, bool keyUp)
    {
        KEYBDINPUT ki = new KEYBDINPUT
        {
            wVk = virtualKey,
            wScan = 0,
            dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
            time = 0,
            dwExtraInfo = GetMessageExtraInfo()
        };
        return new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = ki } };
    }

    // Send a modifier + key sequence using SendInput (modifier down, key down, key up, modifier up)
    public static void SendModifiedKey(Keys modifier, Keys key)
    {
        var inputs = new List<INPUT>
        {
            CreateVirtualKeyInput((ushort)modifier, false), // modifier down
            CreateVirtualKeyInput((ushort)key, false),      // key down
            CreateVirtualKeyInput((ushort)key, true),       // key up
            CreateVirtualKeyInput((ushort)modifier, true)   // modifier up
        };

        SendInput((uint)inputs.Count, inputs.ToArray(), INPUT.Size);
    }

    // Convenience methods for common combos
    public static void SendCtrlC() => SendModifiedKey(Keys.ControlKey, Keys.C);
    public static void SendCtrlV() => SendModifiedKey(Keys.ControlKey, Keys.V);

    // Low-level paste wrapper used above
    private static void SendCtrlVWrapper()
    {
        SendCtrlV();
    }

    // Alternative using System.Windows.Forms.SendKeys (simpler but less reliable with some apps)
    public static void SendTextAlternative(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        System.Windows.Forms.SendKeys.SendWait(text);
    }
}
