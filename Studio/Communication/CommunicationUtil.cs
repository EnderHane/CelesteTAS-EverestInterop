using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CelesteStudio.RichText;
using TasCommunication;

namespace CelesteStudio.Communication;

internal static class CommunicationUtil {
    private static Dictionary<HotkeyID, List<Keys>> bindings;
    private static bool fastForwarding;
    private static bool slowForwarding;
    public static bool Forwarding => fastForwarding || slowForwarding;

    [DllImport("User32.dll")]
    private static extern short GetAsyncKeyState(Keys key);

    private static bool IsKeyDown(Keys keys) {
        return (GetAsyncKeyState(keys) & 0x8000) == 0x8000;
    }

    public static void SetBindings(Dictionary<HotkeyID, List<Keys>> newBindings) {
        if (bindings is null) {
            bindings = newBindings;
            return;
        }
        lock (bindings) {
            bindings = newBindings;
        }
    }

    public static bool CheckControls(ref Message msg) {
        if (!Settings.Instance.SendInputsToCeleste
            || Environment.OSVersion.Platform == PlatformID.Unix
            || bindings == null
            // check if key is repeated
            || ((int) msg.LParam & 0x40000000) == 0x40000000) {
            return false;
        }

        bool anyPressed = false;
        lock (bindings) {
            foreach (HotkeyID hotkeyID in bindings.Keys) {
                List<Keys> keys = bindings[hotkeyID];

                bool pressed = keys.Count > 0 && keys.All(IsKeyDown);

                if (pressed && keys.Count == 1) {
                    if (!keys.Contains(Keys.LShiftKey) && IsKeyDown(Keys.LShiftKey)) {
                        pressed = false;
                    }

                    if (!keys.Contains(Keys.RShiftKey) && IsKeyDown(Keys.RShiftKey)) {
                        pressed = false;
                    }

                    if (!keys.Contains(Keys.LControlKey) && IsKeyDown(Keys.LControlKey)) {
                        pressed = false;
                    }

                    if (!keys.Contains(Keys.RControlKey) && IsKeyDown(Keys.RControlKey)) {
                        pressed = false;
                    }
                }

                if (pressed) {
                    if (hotkeyID == HotkeyID.FastForward) {
                        fastForwarding = true;
                    } else if (hotkeyID == HotkeyID.SlowForward) {
                        slowForwarding = true;
                    }

                    CommunicationServer.Instance?.SendHotkeyPressed(hotkeyID);
                    anyPressed = true;
                }
            }
        }

        return anyPressed;
    }

    public static void CheckForward() {
        if (fastForwarding) {
            CheckForward(HotkeyID.FastForward, ref fastForwarding);
        } else if (slowForwarding) {
            CheckForward(HotkeyID.SlowForward, ref slowForwarding);
        }
    }

    private static void CheckForward(HotkeyID hotkeyId, ref bool forwarding) {
        if (Environment.OSVersion.Platform == PlatformID.Unix || bindings == null) {
            throw new InvalidOperationException();
        }

        bool pressed;
        lock (bindings) {
            if (bindings.ContainsKey(hotkeyId)) {
                List<Keys> keys = bindings[hotkeyId];
                pressed = keys.Count > 0 && keys.All(IsKeyDown);
            } else {
                pressed = false;
            } 
        }

        if (!pressed) {
            CommunicationServer.Instance.SendHotkeyPressed(hotkeyId, true);
            forwarding = false;
        }
    }

    public static void UpdateLines(Dictionary<int, string> updateLines) {
        StudioTextEdit tasText = Studio.Instance.richText;
        tasText.Invoke(() => {
            foreach (int lineNumber in updateLines.Keys) {
                string lineText = updateLines[lineNumber];
                if (tasText.Lines.Count > lineNumber) {
                    Line line = tasText.TextSource[lineNumber];
                    line.Clear();
                    if (lineText.Length > 0) {
                        line.AddRange(lineText.ToCharArray().Select(c => new StudioChar(c)));
                        Range range = new(tasText, 0, lineNumber, line.Count, lineNumber);
                        range.SetStyle(SyntaxHighlighter.CommandStyle);
                    }
                }
            }

            if (updateLines.Count > 0) {
                tasText.NeedRecalc();
            }
        });
    }
}