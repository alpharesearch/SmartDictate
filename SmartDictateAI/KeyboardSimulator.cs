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
            // Option 1: Regex Replace (more general but might be too broad)
            // textToSend = PlaceholderRegex.Replace(textToSend.Trim(), string.Empty).Trim();

            // Option 2: Replace known placeholders (more precise)
            // This might be better to avoid accidentally removing legitimate bracketed text.
            // We can combine it with a regex for any remaining general bracketed forms if desired.
            string tempText = textToSend.Trim();
            foreach (string placeholder in KnownPlaceholdersForDictation)
            {
                // Use Regex.Replace for case-insensitive whole-word match if placeholders can vary slightly
                // For exact match (case-insensitive due to HashSet comparer):
                if (tempText.Equals(placeholder, StringComparison.OrdinalIgnoreCase)) // If the whole segment is a placeholder
                {
                    tempText = string.Empty;
                    break;
                }
                // If placeholders can be part of a larger string and need replacing:
                // tempText = Regex.Replace(tempText, Regex.Escape(placeholder), string.Empty, RegexOptions.IgnoreCase);
            }
            textToSend = tempText.Trim();


            // Optional: Apply a more general regex for any remaining simple bracketed items
            // if they weren't caught by KnownPlaceholders and you want to be aggressive.
            // Be cautious with this.
            // if (!string.IsNullOrWhiteSpace(textToSend) && textToSend.StartsWith("[") && textToSend.EndsWith("]") && textToSend.Length <= 25) {
            //     textToSend = string.Empty;
            // }
        }

        if (string.IsNullOrWhiteSpace(textToSend)) return; // Don't send if filtering resulted in empty string
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
        // For characters requiring Shift (e.g., uppercase, or symbols like '!', '@', '#')
        // a more complex solution would be needed to simulate Shift key press/release
        // around the character. System.Windows.Forms.SendKeys handles this internally.
        // For simplicity, this example sends Unicode characters directly.
        // Most applications handle direct Unicode input well.

        return new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = ki } };
    }

    // Alternative using System.Windows.Forms.SendKeys (simpler but less reliable with some apps)
    public static void SendTextAlternative(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        // SendKeys can have issues with focus and modifier keys in some applications.
        // It also sends keys to the *active* window, which is what we want.
        System.Windows.Forms.SendKeys.SendWait(text);
    }
}
