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
/// The UI follows the terminal's size; it never pins itself to the size it was started with.
/// </summary>
/// <remarks>
/// <para>
/// <c>UI.Start(width, height)</c> reads as a fixed geometry, and on a terminal that cannot be resized it looks like
/// one. It is not: <c>ConsoleManager.AdjustBufferSize()</c> runs every frame, compares the console's reported size
/// against the internal buffer, and adopts the console's whenever they differ. The start size is only the opening
/// bid — one frame later the terminal's own size wins.
/// </para>
/// <para>
/// The distinction matters because the two are easy to confuse when a terminal <em>cannot</em> be resized live. A
/// Windows console in legacy mode caps its window at the screen-buffer width set in its Properties dialog, so
/// dragging the frame does nothing and the app appears frozen at its start size. That is the host refusing to
/// resize, not the app declining to follow.
/// </para>
/// </remarks>
public class TerminalResizeFollowTests
{
    public TerminalResizeFollowTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public void BufferFollowsTheConsole_WhenItGrows() => AssertFollows(new Size(40, 8), new Size(96, 30));

    [Fact]
    public void BufferFollowsTheConsole_WhenItShrinks() => AssertFollows(new Size(96, 30), new Size(40, 8));

    // The start size is an opening bid, not a lease: a console that reports something else from the first frame
    // wins immediately, which is what makes the hardcoded default in an app's Start call harmless.
    [Fact]
    public void ConsoleSizeWins_OverTheSizePassedToStart()
    {
        var console = new ResizableConsole(72, 20);
        Run(console, startWidth: 150, startHeight: 42, () =>
        {
            Assert.True(SpinWait.SpinUntil(() => ConsoleManager.BufferSize == new Size(72, 20), 3000),
                $"the buffer settled at {ConsoleManager.BufferSize}, not the console's 72x20");
        });
    }

    #region Harness
    private static void AssertFollows(Size from, Size to)
    {
        var console = new ResizableConsole(from.Width, from.Height);
        Run(console, from.Width, from.Height, () =>
        {
            Assert.True(SpinWait.SpinUntil(() => ConsoleManager.BufferSize == from, 3000),
                $"never settled at the initial {from}; saw {ConsoleManager.BufferSize}");

            console.Observed = to;   // the terminal was resized under us

            Assert.True(SpinWait.SpinUntil(() => ConsoleManager.BufferSize == to, 3000),
                $"the buffer stayed at {ConsoleManager.BufferSize} after the console became {to}");
        });
    }

    private static void Run(ResizableConsole console, int startWidth, int startHeight, Action body)
    {
        UiTestHarness.EnsureStopped();
        var previousOut = System.Console.Out;
        System.Console.SetOut(System.IO.TextWriter.Null);
        try
        {
            UI.Start(new VerticalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "x")),
                startWidth, startHeight, fps: 66, isAnsiTerminal: true, console: console, input: new NoInput());
            body();
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

    // A terminal the app cannot steer: it reports the size the TEST gives it and ignores the app's attempts to set
    // one, which is how a real terminal behaves on the ANSI path (AnsiTerminalConsole only records a fallback).
    // AdoptSize is the "follow, don't steer" hook and must not write back either, or the two fight every frame.
    private sealed class ResizableConsole(int w, int h) : IConsole
    {
        private Size _size = new(w, h);
        private readonly Lock _gate = new();

        /// <summary>Simulates the user resizing the terminal window.</summary>
        public Size Observed
        {
            get { lock (_gate) return _size; }
            set { lock (_gate) _size = value; }
        }

        Size IConsole.Size
        {
            get => Observed;
            set { }
        }

        public void AdoptSize(in Size size) { }

        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in Character character) { }
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    }
    #endregion
}
