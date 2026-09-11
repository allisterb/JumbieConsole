# Input Routing

How a key press or mouse action reaches the right control — across plain controls, layouts, composite controls,
and nested layouts (e.g. a list inside a tab inside a grid).

There are **two independent pipelines**, and the single most important thing to understand is that they work
completely differently:

| | Keyboard / paste | Mouse (move / click / wheel) |
|---|---|---|
| Travels through | the **logical** layout tree, from the root | nothing — direct **spatial** cell hit-testing |
| Finds its target via | the **focused** control (`FocusedControl` chain) | the **cell under the cursor** and its tagged listener |
| Cares about nesting | yes (each layer must forward) | no (a composited cell carries its listener up as-is) |
| Reaches inactive/hidden controls | no (not focused, not in the routing path) | no (not rendered, so no cells on screen) |

Everything here runs on the single UI thread (the input reader posts decoded events onto it); see
[Multithreading](Multithreading.md). This document is about *routing/dispatch*; for the mouse **gesture** surface
(hover/press/click/double-click synthesis), the wheel hook, and the overlay/modal layer, see
[Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md).

## 1. The single entry point

Decoded terminal events are dispatched by `UI.OnInput` ([UI.cs](../../src/Jumbee.Console/UI.cs)):

```csharp
case KeyInputEvent k:
    var inputEvent = new InputEvent(k.ToConsoleKeyInfo());
    globalInputListener.OnInput(inputEvent);      // 1. global hotkeys, before any control
    if (!inputEvent.Handled)
        layout?.OnInput(inputEventArgs);          // 2. into the ROOT layout's routing
    break;
case MouseInputEvent m:
    ConsoleManager.MousePosition = new Position(m.X, m.Y);   // mouse: just update ConsoleManager
    switch (m.Kind)
    {
        case TerminalMouseKind.Down: ConsoleManager.MouseDown = true; break;
        case TerminalMouseKind.Up:   ConsoleManager.MouseDown = false; break;
        case TerminalMouseKind.Wheel: ConsoleManager.MouseWheel(±WheelLines); break;
    }
    break;
case PasteInputEvent p: layout?.OnPaste(p.Text); break;     // paste follows the keyboard path
```

So **keyboard and paste enter at the root layout only**; **mouse never touches the layout tree** — it just feeds
`ConsoleManager`, which dispatches by hit-testing.

### What the legacy input source cannot deliver

Which events exist at all depends on the source `UI.DefaultInputSource` picked. The ANSI path uses `VtInputSource` +
`AnsiInputDecoder` and sees everything the terminal encodes. The legacy path (`isAnsiTerminal: false`, or any
redirected/CI run) falls back to `ConsoleInputSource`, which is a `Console.ReadKey` loop.

**Measured on cmd.exe / Win10 19045** — `Ctrl`+letter, `Ctrl+←`, `Ctrl+→` and `Ctrl+↓` all arrive intact. A bare
`Console.ReadKey` probe reports `Key=RightArrow Char=0 Mods=Control`, exactly what `HotKeys.Ctrl(ConsoleKey.RightArrow)`
builds, and a live `--legacy` session records `Ctrl+↓` reaching the hotkey table. So a dead `Ctrl`+arrow on legacy is
worth checking against the leaf ring (§ global focus navigation) before suspecting the decoder.

One known gap, from .NET rather than the console: `Console.ReadKey` on Windows filters `Alt` combined with
`PageUp`…`DownArrow` as an Alt+NumPad sequence, which would cost the whole **Alt tier** (§ modifier-key convention),
including `TabPanel` tab switching. *Not yet confirmed by measurement here* — read it as a thing to test, not a
finding.

### The VT input source can fail silently, and `Start` now catches it

`ConsoleManager.AnsiEnabled` (output) and the input source are **independent knobs**, and they can disagree. `Start`
probes the output handle and downgrades `isAnsiTerminal` on refusal, but the source is `input ?? DefaultInputSource(…)`
— so a **caller-supplied** source bypasses that decision entirely. Worse, a caller writing
`UI.Start(root, input: new VtInputSource(…))` constructs it *in the argument list*, so it has already reconfigured
the terminal before `Start` runs its probe.

That matters because `VtInputSource` does not fail loudly. Its `SetConsoleMode` can be refused — a legacy console
refuses VT on both handles — and the reader carries on regardless. The console only encodes arrows, function keys,
mouse and paste as VT sequences **when that mode is set**, so those keys then produce *no bytes at all*, while plain
characters and `Ctrl`+letter (real control characters) still arrive. The app comes up looking perfectly healthy and
navigates for nothing. Measured on a legacy cmd.exe: six seconds of `Ctrl+→` yielded zero bytes; the only byte the
reader ever saw was the `0x11` of `Ctrl+Q`.

`Start` now detects exactly that state via `VtInputSource.VtModeUnavailable` (a *real* terminal that refused — as
opposed to the test-seam source, which has no `TerminalInputMode` because it deliberately never touches a terminal)
and swaps in `ConsoleInputSource`, which is what works there. Mouse and hover are then unavailable, which is
inherent: such a console cannot report them.

**Swapping the source is not enough on its own, and the reason is worth remembering: `Dispose` cannot take back a
read.** `ReaderLoop` keeps one outstanding `_stdin.ReadAsync`, and a console read cannot be cancelled — `Dispose`
ends the thread, but the read stays pending on the console handle for the life of the process. A replacement
`ConsoleInputSource` (which uses `ReadConsoleInput`) then shares one input queue with that orphan, and keys
**alternate between the two readers**: roughly every other press disappears. The giveaway in the field is a hotkey
that works on the second press — `Ctrl+Q` needing two goes. On top of that the console is still in cooked mode (there
was nothing to restore, the `SetConsoleMode` having failed), so conhost's line editor consumes arrows as
line-editing commands before `ReadConsoleInput` ever sees them.

So the constructor **does not start the reader at all** when `VtModeUnavailable` — never issuing the read leaves
nothing to orphan. It also skips the mouse/paste/focus enable sequences in that state: with VT processing off they
are printed rather than obeyed, spraying `[?1003h[?1006h…` across the screen and enabling nothing. `Dispose` is
symmetric and skips the disable sequence it never sent.

To debug this layer, set `JUMBEE_INPUT_LOG` to a file: `VtInputSource` then logs its startup state (`raw=` is whether
VT input mode applied) and the raw stdin bytes of every read. Zero bytes for a keypress that the terminal *should*
encode is the signature of the failure described below.

It is not free — every read is a separate open/append/close — so `UI.Start` and `UI.Stop` announce it on **stderr**,
the shutdown notice reporting how many bytes the run wrote. Two notices rather than one because neither alone is
reliable: the startup one is the only warning that precedes the cost, but the UI paints over it immediately (and a
console with no alternate screen to restore just clears it), while the shutdown one is written after the terminal has
been handed back and therefore always survives. Nothing is printed when the variable is unset.

## 2. The focus model

Focus is **single and global**. Every `Control` self-registers in `UI`'s control list via its `UI.Paint`
subscription in the constructor, so `UI` can address any control that has ever been painted regardless of where it
sits in the tree.

- `UI.SetFocus(target)` clears `IsFocused` on every other registered control and sets it on the target (and its
  `FocusableControl`). Called by click-to-focus, and by `Control.Focus()` — so **always focus through these**, never
  by assigning `IsFocused = true` directly, which would leave the previous control focused too (multiple focused
  controls → a key delivered to all of them).
- `UI.Focused` is the one control with `IsFocused == true` (or none).

At any instant exactly one leaf is focused — a button, a list, a text editor, a tab header, … — and that is where
keyboard input is steered.

## 3. The `FocusedControl` contract

Keyboard routing hinges on one member of `IFocusable`:

```csharp
IFocusable? FocusedControl { get; }   // "the input target inside me, right now — or null"
```

Each type answers it differently. This table is the heart of keyboard routing:

| Type | `FocusedControl` returns | Why |
|---|---|---|
| `IFocusable` (default) | `Focusable && IsFocused ? FocusableControl : null` | a leaf is its own target when focused |
| `Control` | `Focusable && IsFocused ? this : null` | **`this`, not `FocusableControl`** — see note below |
| `ControlFrame` | `_control.FocusedControl is not null ? this : null` | a frame stays the routing node (for scroll keys) but reports inner focus |
| `Layout<T>` (default) | `Focusable && IsFocused ? FocusableControl : null` | a plain container isn't itself a focus target |
| `CompositeControl` | the first focused descendant in its children, else `base` | delegates into the child that owns focus |
| `TabPanel` | a focused tab header, else the **active** content's `FocusedControl` | delegates into the bar or the visible tab |

> **`Control.FocusedControl` returns `this`, not `FocusableControl`.** `FocusableControl` is `Frame ?? this` — for a
> *framed* control it's the `ControlFrame`. Returning the frame here would make a frame forwarding input inward route
> back to itself → infinite recursion (a real StackOverflow we hit). `FocusedControl` = "the input target inside me";
> `FocusableControl` = "how a parent layout addresses me". Don't conflate them.

Note that `Layout<T>.FocusedControl` is **`virtual`**: the default behaves like the `IFocusable` default (a plain
container forwards nothing unless it is itself focused), but interactive layouts override it so input reaches their
descendants even when nested — that override is what makes `TabPanel` work (§7).

## 4. Keyboard routing

`Layout.OnInput` is the router ([Layout.cs](../../src/Jumbee.Console/Layout.cs)):

```csharp
public void OnInput(UI.InputEventArgs args)
{
    // tunnel phase: this layout's own nav keys (Alt), Overlay Esc-close, … — consumed => mark handled
    if (InterceptInput(args)) { if (args.InputEvent is { } e) e.Handled = true; return; }

    foreach (var f in Controls)
    {
        if (f is ILayout nested) { if (nested.FocusedControl is not null) nested.OnInput(args); } // recurse w/ tunnel
        else f?.FocusedControl?.OnInput(args);        // leaf / frame / composite (null-safe)
        if (args.InputEvent?.Handled == true) return; // stop once consumed — input belongs to one control
    }
}
```

- **Hotkeys first.** `globalInputListener` checks `GlobalHotKeys` (e.g. `Ctrl+Q → Stop`, and anything from
  `UI.RegisterHotKey`) and can mark the event handled *before* any control sees it — so hotkeys work regardless of
  focus.
- **Tunnel (`InterceptInput`).** A layout may consume a key before routing it down (e.g. `Overlay` closing on its
  `CloseKey`). Default returns false.
- **Bubble to the focused leaf.** `Controls` are the layout's children; for each, `FocusedControl?.OnInput(args)`
  delivers only to the one whose focused descendant is non-null. Because focus is single, exactly one child resolves
  to the focused leaf.

A leaf control's `Control.OnInput(UI.InputEventArgs)` runs `OnInput(InputEvent)` only when `HandlesInput` is true,
so display-only controls ignore keys.

### The modifier-key navigation convention

Navigation keys are tiered by modifier so the **unmodified** key is always free for the focused control:

| Modifier | Scope | Owner | Examples |
|---|---|---|---|
| *(none)* | the **focused control** | the control | a text editor indents on `Tab`, moves its caret on the arrows |
| `Ctrl` | **global** | `UI` (global hotkeys) | `Ctrl+arrows` move between regions, `Ctrl+N/P` within a region, `Ctrl+Q` quit |
| `Alt` | **layout** | the **layout** (its `InterceptInput` tunnel) | `TabPanel` switches tabs on `Alt+Left/Right` |
| `Shift` | **frame** | the `ControlFrame` | (intended) scroll the focused control's frame |

There is **one global hotkey set** (`Ctrl`-modified). Each *layout* defines its own navigation keys by overriding
`InterceptInput` — e.g. `TabPanel` claims `Alt+arrows`. Plain `Tab` is deliberately left for the focused control:
`Ctrl+Tab` can't be encoded by terminals without the `modifyOtherKeys`/kitty protocol (not enabled), whereas
`Ctrl`+*letter* (a C0 control char) and `Ctrl`/`Alt`+*arrow* (`CSI 1;5/3{ABCD}`) decode reliably. Leaving `Tab`
unbound is what lets `TextEditor` receive it (it inserts `TabWidth` spaces).

### Global focus navigation (Ctrl+arrows between regions, Ctrl+N/P within)

Focus navigation is **spatial**, driven off the **root layout's 2-D cell grid** (`Rows`/`Columns`/`this[r,c]`) rather
than a flat registration-order ring (construction order isn't the user's mental order, and screen positions aren't
cheaply available — but the layout structure already encodes the spatial arrangement). Two tiers, both default
`Ctrl`-tier hotkeys:

- **`Ctrl+arrows` → move between regions.** `UI.FocusLeft/Right/Up/Down` step one cell in the root layout's grid,
  **wrapping per axis** and **skipping cells with no focusable**; landing on a cell focuses its first focusable leaf
  (`FirstLeaf`, which descends nested layouts and `ControlFrame`s). A `CompositeControl` is an *opaque* leaf (it
  reports `HandlesInput`): arrowing onto a `CodeEditor` cell focuses the composite, which then delegates focus to its
  editor child via `Control_OnFocus` (focus resolves to the composite — the navigable unit — through `Control.Owner`/
  `FocusRoot`, so click-to-focus and keyboard navigation agree).
- **`Ctrl+N/P` → cycle within the current region.** `UI.FocusNext/Previous` cycle the focusable leaves of the cell
  that currently holds focus — **but only when that cell is a multi-focusable nested layout**; a single-control or
  composite cell is a **no-op** (enter/leave those with the arrows).

A leaf is an interactive, laid-out control (`Focusable && HandlesInput && HasLayout`) — so display-only controls,
adornments, and frames/composite *wrappers* are skipped or descended-through. An **interactive** adornment is not
skipped, though, and that surprises people: `SplitDivider` is `Focusable` + `HandlesInput` (that is how you resize a
split from the keyboard), so it is a leaf like any other. A `SplitPanel`'s ring is therefore
`Tree · divider · host · divider · editor` — **`Ctrl+arrow` is leaf-to-leaf, not pane-to-pane, and crossing a split
takes two presses.** Worth knowing when it looks like the key did nothing: the first press landed on a 1-cell divider
whose only focus cue is a recolour to `IStyleTheme.Hover`, a background-only style that flattens to plain black on a
16-colour legacy console. Remap by re-registering
`HotKeys.CtrlLeft/Right/Up/Down`/`CtrlN`/`CtrlP`. Caveat: a couple of layouts (`TabPanel`, `Overlay`) **flatten**
their indexer for routing, so as a *root* their `Ctrl+arrows` follow that flattened order rather than a spatial grid
(uncommon — the root is normally a `Grid`/stack/dock, whose indexer is spatial). Sparse `Grid` cells are tolerated
(`CellAt` swallows the empty-slot indexer throw).

```
key ─▶ UI.OnInput ─▶ Ctrl hotkeys ─▶ ROOT layout.OnInput
                                       ├─ InterceptInput?  (this layout's own tunnel — Alt nav, Esc-close, …)
                                       └─ for each child:
                                            ├─ nested ILayout w/ focus → child.OnInput  (its OWN tunnel, then recurse)
                                            └─ leaf / frame / composite → child.FocusedControl?.OnInput
                                                                             └─▶ focused leaf.OnInput(InputEvent)
```

## 5. Mouse routing

Mouse dispatch is entirely separate and **spatial**. A control tags the cells it owns with itself, in its indexer
([Control.cs](../../src/Jumbee.Console/Control.cs)):

```csharp
var cell = consoleBuffer[position];
return Focusable || WantsMouse ? cell.WithMouseListener(this, position) : cell;   // position in THIS control's coords
```

That tag rides the composited cell up the tree — through layouts, frames, composites — unchanged, because each layer
returns the child's cell as-is. `ConsoleManager` then dispatches by hit-testing the cell under the cursor
([ConsoleManager.cs](../../ext/C-sharp-console-gui-framework/ConsoleGUI/ConsoleManager.cs)):

```csharp
MouseContext?.MouseListener?.OnMouseDown(MouseContext.Value.RelativePosition);   // on MouseDown flip
// MousePosition transitions -> OnMouseEnter / Leave / Move
public static void MouseWheel(int delta)
{
    if (MouseContext is { } ctx && ctx.MouseListener is IMouseWheelListener w)
        w.OnMouseWheel(ctx.RelativePosition, delta);                              // wheel, if the listener wants it
}
```

So a click/hover/wheel lands **directly** on the control whose cell is under the cursor — nesting is irrelevant,
because hit-testing happens on the final composited buffer. Key consequences:

- **Click-to-focus** is the bridge to the keyboard pipeline: `Control`'s `OnMouseDown` calls `UI.SetFocus(this)`, so
  clicking a control makes subsequent keys route to it.
- **A non-focusable control that still wants mouse** (e.g. a clickable `Link`) opts in with `WantsMouse => true` so
  its cells get tagged.
- **Only rendered cells are hit-testable.** A control in a hidden tab or an unshown popup has no cells on screen, so
  it cannot receive mouse — no special suppression needed.

(For how `OnMouseDown`/`Up` become hover/press/click/double-click, and how the wheel default scrolls the frame, see
[Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md).)

## 6. Composite controls

A `CompositeControl` ([CompositeControl.cs](../../src/Jumbee.Console/CompositeControl.cs)) is one `Control` made of
several child controls via an internal layout. It is a single `IFocusable` to its parent, so it overrides
`FocusedControl` to return the focused descendant:

```csharp
public override IFocusable? FocusedControl
{
    get
    {
        foreach (var c in _content.Controls)
            if (c?.FocusedControl is { } focused) return focused;
        return base.FocusedControl;
    }
}
```

That single override is what lets keyboard input route *through* the composite to the focused child (e.g. the
`CodeEditor`'s text editor). Mouse needs nothing extra: the composite's indexer returns each child's cell as-is, so
the child's listener (in child coordinates) reaches `ConsoleManager` directly. The composite wires inter-child
behaviour itself (e.g. the editor's `Changed`/`MouseWheeled` events) — there is no event bus.

### Which child gets the focus

Focusing a child resolves *up* to the composite (`UI.SetFocus` → `Control.FocusRoot`), which delegates back down to
one child via `Control_OnFocus` → `FocusChild`. So the composite decides, and it must follow what was asked for:
`SetFocus` first tells the owning composite (`OnChildFocusRequest`) which child was requested, and that child becomes
the default `FocusChild`. Without that step a composite always re-focuses the child it happened to claim first —
clicking any *other* field of a form would do nothing, and a focusable-but-passive sibling (a description label) would
swallow the keys meant for the content. Override `FocusChild` to pin a specific child regardless (e.g. `Dialog`).

### A composite's key tunnel

A composite gets a first look at keys headed for its focused child via `InterceptInput` — the same tunnel shape a
layout has (§7), so it can define its own navigation keys. **Both** routing paths run it: `Layout.OnInput`'s composite
branch, and `ControlFrame.OnInput` (the routing layer hands the key to the *frame* for a framed composite, which would
otherwise dispatch straight to the focused descendant and skip the tunnel).

The base implementation handles Tab / Shift+Tab when a subclass opts in with `TabNavigatesChildren` — the form case,
where several fields are tabbed between. It is **opt-in** because Tab otherwise belongs to the focused control
(§5: a `TextEditor` indents with it), and because `Ctrl+N`/`Ctrl+P` region cycling deliberately does not descend into a
composite.

## 7. Layouts and nesting

`Layout.Controls` enumerates the layout's cells (`this[row, column]` over `Rows × Columns`). For a plain container
those are its direct children. Two patterns let routing reach deeper:

- **Flattening.** A layout can override `Rows`/the indexer to surface *leaf* focusables rather than sub-containers,
  so the root's `Controls.ForEach` reaches them. `TabPanel` does this for its headers + active content. (`Overlay`
  instead *delegates* its grid to the bottom layer when no popup is shown, and presents only the popup while one is —
  it does not flatten.)
- **`FocusedControl` override.** When the layout is itself *nested* (a child of another layout), the parent only
  sees the one `IFocusable`, and routes through `parent → thisLayout.FocusedControl`. The default returns null unless
  the layout is itself focused — which is exactly why a nested interactive layout would dead-end. Overriding
  `FocusedControl` to return the focused descendant fixes it.

`TabPanel` ([TabPanel.cs](../../src/Jumbee.Console/Layouts/TabPanel/TabPanel.cs)) needs **both**, so it works as root *and*
nested:

```csharp
// flattened: all headers, then the active tab's content  (so the root's OnInput walks them)
public override int Rows => _tabs.Count + (_selectedIndex >= 0 ? 1 : 0);
public override int Columns => 1;
public override IFocusable this[int row, int column] =>
    row < _tabs.Count ? _tabs[row].Header : _tabs[_selectedIndex].Content;

// delegated: focused header, else into the active content  (so a parent layout routes through here)
public override IFocusable? FocusedControl
{
    get
    {
        foreach (var t in _tabs) if (t.Header.FocusedControl is { } h) return h;
        return _selectedIndex >= 0 ? _tabs[_selectedIndex].Content.FocusedControl : null;
    }
}
```

Only the **active** tab's content is in either path, so controls in inactive tabs receive no keyboard (and, being
unrendered, no mouse).

## 8. End-to-end: a list inside a tab inside a grid

Tree: `Grid (root) → TabPanel → "Files" tab → ListBox`.

**Mouse — click the list:** the `ListBox` cells carry its listener (composited up through the tab/grid unchanged);
`ConsoleManager` hit-tests and calls `ListBox.OnMouseDown` → `UI.SetFocus(listBox)`. Direct; the grid/tab nesting is
irrelevant.

**Keyboard — press ↓ now that the list is focused:**

```
↓ ─▶ UI.OnInput ─▶ (no hotkey) ─▶ Grid.OnInput
       └─ Controls = [TabPanel, …]
            └─ TabPanel.FocusedControl  ─▶ active content.FocusedControl ─▶ ListBox   (it's focused)
                 └─ ListBox.OnInput(↓)   → moves the selection
```

If instead a **tab header** is focused (e.g. right after start, or after clicking a label), `TabPanel.FocusedControl`
resolves to that header, and the **plain arrows** along the bar's axis switch tabs (Left/Right on a top/bottom bar,
Up/Down on a left/right bar), keeping focus on the header for continued arrowing — the standard tab-strip behaviour,
handled in `TabPanel.InterceptInput` gated on "a header has focus". (`Alt`+arrows switch tabs from *anywhere* in the
panel, including while the content is focused.) Clicking back into the list moves focus there again — the single
global focus is the switch between "drive the tabs" and "drive the content".

## 9. Invariants & pitfalls

- **One focused leaf at a time.** Don't assume a container "has" focus; focus lives on the leaf, and containers only
  forward to it via `FocusedControl`.
- **Nested interactive layout?** It must override `FocusedControl` (and usually flatten `Controls`), or keyboard
  dead-ends at it. A plain container needs neither.
- **`FocusedControl` ≠ `FocusableControl`.** Target-inside-me vs how-my-parent-addresses-me. Returning the wrong one
  around a `ControlFrame` recurses infinitely.
- **Mouse follows pixels, not the tree.** If a control receives unexpected clicks (or none), reason about *which
  cell's listener* is on screen there, not about the layout hierarchy.
- **Hidden = unreachable, for free.** Inactive tabs / closed popups are neither rendered (no mouse) nor in the
  focus/routing path (no keyboard); no explicit gating required.

## The tunnel runs at every layout on the focus path

`InterceptInput` (the tunnel that lets a layout consume a key before its children — e.g. `Overlay` closing on its
`CloseKey`, or `TabPanel` switching tabs on `Alt+arrows`) runs for **every layout on the focus path**, not just the
root. `Layout.OnInput` recurses into a nested layout via *its* `OnInput` (so it gets its own tunnel pass) rather than
jumping straight to the focused leaf — which is what lets a nested layout define its own navigation keys. A layout's
`InterceptInput` only fires when the layout is actually on the focus path: the root always (keys enter at
`root.OnInput`), and a nested layout only when it contains the focus (`FocusedControl != null`) — so a sibling's keys
never reach it. A consumed key (`InterceptInput` returns `true`) is marked `Handled`, so ancestor layouts stop.

## See also
- [Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md) — mouse gesture synthesis, the wheel hook, overlays, modal routing.
- [Multithreading](Multithreading.md) — the single UI thread that all input dispatch runs on.
- [ConsoleGUI Control Rendering](ConsoleGUI%20Control%20Rendering.md) — the cell-composition model the mouse listeners ride on.
