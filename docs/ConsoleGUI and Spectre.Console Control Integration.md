# ConsoleGUI and Spectre.Console Control Integration

This document outlines the architectural pattern for integrating `Spectre.Console` widgets into the ConsoleGUI framework. It specifically addresses how the layout and rendering pipelines of the two libraries bridge via the Jumbee.Console `SpectreControl` and `AnsiConsoleBuffer` classes.

## 1. The Integration Challenge

*   **ConsoleGUI** uses a **pull-based** layout system. Controls are initialized with constraints (`MinSize`, `MaxSize`) and must determine their own `Size` during an `Initialize()` pass.
*   **Spectre.Console** uses a **rendering pipeline** where objects are typically `Measure`d and then `Render`ed into segments.

To host a Spectre widget (like a `Panel`, `Table`, or `BarChart`) inside the ConsoleGUI layout system, we must translate ConsoleGUI's layout constraints into a Spectre `Measure` call, and then capture the Spectre `Render` output into a buffer that ConsoleGUI can display.

## 2. The Bridge Components

### 2.1 `SpectreControl<T>`
This generic class wraps a `Spectre.Console.IRenderable`. It inherits from `Jumbee.Console.Control` and acts as the adapter.

### 2.2 `AnsiConsoleBuffer`
This class implements `Spectre.Console.IAnsiConsole` but directs output to a `Jumbee.Console.ConsoleBuffer` (a 2D character array) instead of the standard output.

## 3. The Layout Process: `Initialize()`

The critical integration point is the `Initialize()` method of `Jumbee.Console.Control`.

### The Base Implementation (`Control`)
The base `Control` class provides a default implementation that essentially fills the available space or defaults to a safe maximum if unconstrained.

```csharp
protected override void Initialize()
{
    // Default logic: Set size to MaxSize (clamped)
    var size = ...; 
    Resize(size);
    // ...
}
```

### The `SpectreControl` Implementation
For proper layout, `SpectreControl<T>` **must override** `Initialize()`. It should use the `Measure` capability of the wrapped Spectre widget to determine the optimal size.
> **Note:** Spectre's `Measure` method primarily returns width. Calculating the exact height often requires a dry-run `Render` or specific knowledge of the widget. `SpectreControl` should ideally perform a layout-only render if height is unknown and critical.

## 4. The Rendering Process: `Render()`

The `Render()` method in `SpectreControl` bridges the drawing phase.

1.  **Clear**: The `AnsiConsoleBuffer` is cleared to ensure no artifacts remain from the previous frame.
2.  **Write**: The `Content` (Spectre renderable) is written to the `ansiConsole`.
3.  **Pipeline**:
    *   `ansiConsole.Write(Content)` -> `RenderPipeline` -> `Content.Render(...)` -> `Segments`
    *   The `AnsiConsoleBuffer` interprets the resulting ANSI sequences/Segments and plots `Cell`s (Char + Color) onto the `ConsoleBuffer`.

```csharp
protected sealed override void Render()
{  
    // 1. Clear the virtual console buffer
    ansiConsole.Clear(true); 
    
    // 2. Render the Spectre widget into the buffer
    ansiConsole.Write(_content);        
}
```

## 5. State Management and Threading

*   **Invalidate**: When a property on the wrapped Spectre control changes (e.g., `Panel.Header = ...`), the `SpectreControl` wrapper must call `Invalidate()`. This schedules a repaint on the UI thread.
*   **Cloning**: Since `Spectre.Console` widgets are often modified in place, but ConsoleGUI rendering happens on a background thread, developers should be careful about thread safety. Using `CloneContent()` patterns (Copy-On-Write) for immutable properties or locking is recommended.

## 6. Summary of Responsibilities

| Component | Responsibility |
| :--- | :--- |
| **ConsoleGUI Parent** | Sets `MinSize`/`MaxSize` on the `SpectreControl` via `Context`. Calls `Initialize()`. |
| **SpectreControl** | Overrides `Initialize()` to `Measure()` the content. Calls `Resize()` to set bounds. Overrides `Render()` to draw content to buffer. |
| **AnsiConsoleBuffer** | Intercepts Spectre output and maps (X,Y) coordinates to the `ConsoleBuffer`. |
| **Spectre Widget** | Provides `Measure()` logic and generates `Segment`s during `Render`. |
