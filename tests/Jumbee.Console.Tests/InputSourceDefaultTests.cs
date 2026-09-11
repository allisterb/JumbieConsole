namespace Jumbee.Console.Tests;

using System.IO;
using System.Reflection;

using Jumbee.Console;

using Xunit;

/// <summary>Covers the auto-selected default input source: an interactive ANSI terminal gets the mouse-capable
/// <see cref="VtInputSource"/>; everything else (non-ANSI, or redirected/piped like this test host) keeps the safe
/// keyboard-only <see cref="ConsoleInputSource"/>.</summary>
public class InputSourceDefaultTests
{
    private static object? DefaultInputSource(bool isAnsiTerminal) =>
        typeof(UI).GetMethod("DefaultInputSource", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [isAnsiTerminal]);

    [Fact]
    public void NonAnsiTerminal_AlwaysKeyboardOnly()
    {
        // A non-ANSI terminal never gets the VT source (no mouse/paste sequences to speak) — regardless of tty state.
        Assert.IsType<ConsoleInputSource>(DefaultInputSource(false));
    }

    [Fact]
    public void RedirectedHost_KeepsKeyboardOnly_EvenWhenAnsi()
    {
        // The test host redirects stdin/stdout, so it is not an interactive terminal and must not be flipped into raw
        // VT input mode — the default stays the keyboard-only source even for an "ANSI" run.
        Assert.False(UI.IsInteractiveTerminal());
        Assert.IsType<ConsoleInputSource>(DefaultInputSource(true));
    }

    [Fact]
    public void SourceThatNeverTouchedTheTerminal_IsNotTreatedAsUnusable()
    {
        // Start swaps out a VtInputSource the terminal refused VT input mode (VtModeUnavailable), because such a
        // source silently eats every arrow. The guard must key off a REAL terminal that said no — not off "no
        // TerminalInputMode at all", which is the injected/test-seam source that deliberately never touches the
        // terminal and is perfectly usable. Getting that wrong would silently replace an injected source with one
        // reading the real console, so every scripted-input test would hang instead of failing.
        using var source = new VtInputSource(new MemoryStream(), idleFlushMs: 1);
        Assert.False(source.VtModeUnavailable);
    }
}
