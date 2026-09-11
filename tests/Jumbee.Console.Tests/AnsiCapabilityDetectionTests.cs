namespace Jumbee.Console.Tests;

using System;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

/// <summary>
/// <see cref="UI.Start"/> asks the console whether it can render ANSI before assuming it can.
/// </summary>
/// <remarks>
/// <para>
/// A Windows console only interprets escape sequences once an application enables VT processing, and one with the
/// machine-wide <b>Use legacy console</b> option ticked refuses to enable it at all. Before the probe existed,
/// <c>isAnsiTerminal</c> defaulted to true and was believed: on such a console every frame arrived as literal text
/// and the UI was unreadable from the first paint.
/// </para>
/// <para>
/// These tests pin the <em>gate</em> rather than the probe's verdict. The verdict depends on the machine the suite
/// runs on — that is the whole point of probing — so asserting it would make the suite pass or fail with a Windows
/// setting. What must hold everywhere is that the probe is consulted only when it could possibly apply, and never
/// reaches around a caller who supplied their own console. Dropping that gate would silently push every headless
/// render and snapshot onto the legacy path on a legacy-console machine, which is exactly the kind of breakage that
/// shows up as inexplicably wrong test output rather than as an error.
/// </para>
/// </remarks>
public class AnsiCapabilityDetectionTests
{
    public AnsiCapabilityDetectionTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public void SuppliedConsole_IsNeverDowngraded_WhateverTheRealTerminalSupports()
    {
        Assert.True(RenderPathFor(isAnsiTerminal: true),
            "a caller-supplied console is a stub or a headless render — its ANSI choice is not the real " +
            "terminal's to veto, and probing on its behalf would break snapshots on a legacy-console machine");
    }

    [Fact]
    public void SuppliedConsole_StillHonoursAnExplicitLegacyRequest()
    {
        Assert.False(RenderPathFor(isAnsiTerminal: false),
            "the probe may only ever downgrade; it must not turn the legacy path back on");
    }

    #region Harness
    // Starts a UI against a stub console and reports which render path it chose.
    private static bool RenderPathFor(bool isAnsiTerminal)
    {
        UiTestHarness.EnsureStopped();
        var previousOut = System.Console.Out;
        System.Console.SetOut(System.IO.TextWriter.Null);
        try
        {
            var run = UI.Start(
                new VerticalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "x")),
                40, 8, fps: 66, isAnsiTerminal: isAnsiTerminal,
                console: new StubConsole(40, 8), input: new NoInput());

            Assert.True(SpinWait.SpinUntil(() => UI.IsRunning, 2000), "the UI should have started");
            return ConsoleManager.AnsiEnabled;
        }
        finally
        {
            UI.Stop();
            UI.RestoreBuiltInHotKeys();
            System.Console.SetOut(previousOut);
            // Wait for the UI thread AND the dispatcher to actually be down. Stop() returns before they are, and
            // the next test's Start then races a session that is still tearing down -- which surfaces as an
            // unrelated test failing somewhere later in the run.
            UiTestHarness.EnsureStopped();
        }
    }

    private sealed class NoInput : IInputSource
    {
        public bool TryRead(out TerminalInputEvent? evt) { evt = null; return false; }
    }

    private sealed class StubConsole(int w, int h) : IConsole
    {
        public Size Size { get; set; } = new Size(w, h);
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in Character character) { }
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    }
    #endregion
}
