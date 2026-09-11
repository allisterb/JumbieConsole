namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// A focused control nested several layouts deep must still be reachable by keyboard.
/// </summary>
/// <remarks>
/// <para>
/// Reported against the examples browser on the legacy render path: Tab, the arrows and Ctrl+arrows all do nothing,
/// while Ctrl+B / Ctrl+E / Ctrl+F12 keep working. That split is the signature of <b>nothing being routed to a
/// focused control</b> — global hotkeys are dispatched before the layout and never consult focus, everything else
/// goes through <c>layout.OnInput</c>, which walks for <c>FocusedControl</c> and delivers to whatever it finds.
/// </para>
/// <para>
/// The browser's root is a <c>DockPanel</c> holding a status line and a <c>SplitPanel</c>, whose panes are
/// themselves split — so its tree is three layouts deep. The walk is per-layout over <c>Rows × Columns</c> via
/// <c>CellAt</c>, so a layout that reports nothing enumerable at a given size hides everything beneath it. Size is
/// the variable that tracks the render path in practice: the legacy console is whatever its Properties dialog says
/// (often 80×25) while the ANSI path gets the real, larger window.
/// </para>
/// </remarks>
public class NestedLayoutFocusRoutingTests
{
    public NestedLayoutFocusRoutingTests() => UiTestHarness.EnsureStopped();

    [Theory]
    [InlineData(150, 42)]   // a roomy terminal, as the ANSI path usually gets
    [InlineData(80, 25)]    // the legacy console default
    [InlineData(60, 20)]
    [InlineData(40, 12)]
    public void ADeeplyNestedFocusedControl_IsStillFoundByTheInputWalk(int width, int height)
    {
        var tree = new ListBox(["alpha", "beta", "gamma"]);
        var editor = new TextLabel(TextLabelOrientation.Horizontal, "editor");
        var status = new TextLabel(TextLabelOrientation.Horizontal, "status");

        // The browser's shape: status docked at the bottom, a split of tree | editor filling the rest.
        var inner = new SplitPanel(SplitOrientation.Vertical, tree, editor, splitPosition: 30);
        var root = new DockPanel(DockedControlPlacement.Bottom, status, inner);

        _ = ConsoleSnapshot.ToText(root, width, height);   // lay it out at this size
        UI.SetFocus(tree);

        Assert.True(tree.IsFocused, $"SetFocus did not take at {width}x{height}");
        Assert.Same(tree, root.FocusedControl);
    }
}
