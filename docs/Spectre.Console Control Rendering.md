# Spectre.Console Control Rendering Pipeline

This document details the rendering model of the **Spectre.Console** library, specifically focusing on how controls (widgets) are measured, laid out, and rendered into ANSI escape sequences. It analyzes the pipeline from the entry point (`AnsiConsole.Write`) down to the generation of `Segment`s, using `Panel` as a primary case study.

## 1. Core Concepts

Before understanding the pipeline, it is essential to understand the fundamental primitives used by Spectre.Console.

### 1.1 `IRenderable`
The interface `Spectre.Console.Rendering.IRenderable` is the building block of all widgets. Every control (Text, Panel, Table, Tree, etc.) implements this interface.

```csharp
public interface IRenderable
{
    // Calculates the required width (min/max) given a constraint (maxWidth)
    Measurement Measure(RenderOptions options, int maxWidth);

    // Generates the actual content segments given a constraint
    IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}
```

### 1.2 `Segment`
A `Segment` is the atomic unit of rendering. It represents a piece of text with a specific `Style` (foreground, background, attributes).
*   **Text**: The content string.
*   **Style**: Color and decoration (bold, italic, etc.).
*   **IsControlCode**: If true, it's a raw ANSI sequence (like cursor movement) and doesn't take up visual space.
*   **IsLineBreak**: Explicit newline.

### 1.3 `Measurement`
Returned by `Measure`, this struct tells the layout engine how flexible a control is.
*   **Min**: The absolute minimum width the control can collapse to without breaking (soft wrapping implies this can be the length of the longest word).
*   **Max**: The preferred width if space is unlimited.

### 1.4 `RenderOptions`
Context passed down the rendering tree. Contains:
*   **Capabilities**: (Unicode support, Color system, etc.)
*   **Size**: The size of the console.
*   **Height**: An optional height constraint override.

## 2. The Rendering Pipeline

When you call `AnsiConsole.Write(myPanel)`, the following high-level sequence occurs:

1.  **Entry Point**: `AnsiConsole.Write` delegates to the underlying `IAnsiConsole` implementation (usually `AnsiConsoleFacade` -> `AnsiConsoleBackend`).
2.  **AnsiBuilder**: The backend calls `AnsiBuilder.Build(console, renderable)`.
3.  **Pipeline Processing**:
    *   `AnsiBuilder` calls `renderable.GetSegments(console)`.
    *   `GetSegments` invokes the `RenderPipeline`. This is where `IRenderHook`s (like `LiveDisplay` or `Progress`) can intercept or modify the renderable before it's drawn.
4.  **Segment Generation**:
    *   The renderable's `Render` method is called with `RenderOptions` and the current console width.
    *   This returns an `IEnumerable<Segment>`.
5.  **Output Generation**:
    *   `AnsiBuilder` iterates over the segments.
    *   It converts `Segment` styles and text into a raw `string` containing ANSI escape codes (e.g., `\x1b[31mHello\x1b[0m`).
6.  **Writing**: The final string is written to the `TextWriter` (Stdout).

## 3. The Layout Phase (`Measure`)

Spectre.Console uses a **width-first** constraint system. There is no explicit "Layout" pass separate from Render, but `Measure` acts as the layout calculation step.

*   A parent control usually calls `Measure(options, availableWidth)` on its children to decide how to distribute space.
*   **Collapsing**: If `maxWidth` is smaller than a child's preferred width, the child typically soft-wraps text.

### Example: `Panel.Measure`
```csharp
protected override Measurement Measure(RenderOptions options, int maxWidth)
{
    // 1. Create a Padder to handle the panel's padding
    var child = new Padder(_child, Padding);
    
    // 2. Delegate to the helper method
    return Measure(options, maxWidth, child);
}

private Measurement Measure(..., IRenderable child)
{
    // 3. Account for border thickness (EdgeWidth is typically 2 for box borders)
    var edgeWidth = (options.GetSafeBorder(this) is not NoBoxBorder) ? EdgeWidth : 0;
    
    // 4. Measure the child with reduced width
    var childWidth = child.Measure(options, maxWidth - edgeWidth);

    // 5. Handle explicit Width override
    if (Width != null) { ... }

    // 6. Return total size (child + borders)
    return new Measurement(
        childWidth.Min + edgeWidth,
        childWidth.Max + edgeWidth);
}
```

## 4. The Rendering Phase (`Render`)

This is where the visual tree is traversed. Parents are responsible for rendering their children and decorating the result (e.g., drawing borders around them).

### Case Study: `Panel.Render`

The `Panel` control is an excellent example of a container. It wraps a child renderable, adds padding, and draws a box around it.

1.  **Determine Border Style**: Checks `options.Unicode` to decide whether to use ASCII (e.g., `+--+`) or Unicode (e.g., `╭──╮`) borders.
2.  **Measure Child**:
    ```csharp
    var width = Measure(options, maxWidth, child);
    ```
3.  **Calculate Dimensions**:
    *   `panelWidth`: Based on `Measure` result. If `Expand` is true, it tries to fill `maxWidth`.
    *   `innerWidth`: `panelWidth` minus the border edges.
    *   `height`: Explicit height or calculated based on content.
4.  **Render Top Border**:
    *   Adds segments for Top-Left corner, Top line (repeated), and Top-Right corner.
    *   Injects the `Header` (if any) into the top border line.
5.  **Render Child**:
    *   Crucially, the Panel **calls Render on the child**:
        ```csharp
        var childSegments = ((IRenderable)child).Render(
            options with { Height = height }, // Pass height constraint if any
            innerWidth // Pass constrained width
        );
        ```
6.  **Line Splitting & Assembly**:
    *   The child returns a flat list of segments. The Panel needs to wrap borders around *lines* of segments.
    *   It calls `Segment.SplitLines(childSegments, innerWidth)` to break the flat list into `List<SegmentLine>`.
    *   **Iteration**:
        ```csharp
        foreach (var line in lines) 
        {
            // Add Left Border
            result.Add(new Segment("│", borderStyle));
            
            // Add Line Content
            result.AddRange(line);
            
            // Add Right Padding (if the line is shorter than innerWidth)
            // This ensures the right border aligns perfectly.
            if (lineLength < innerWidth) 
                result.Add(Segment.Padding(innerWidth - lineLength));
            
            // Add Right Border
            result.Add(new Segment("│", borderStyle));
            
            // Add Newline
            result.Add(Segment.LineBreak);
        }
        ```
7.  **Render Bottom Border**: Adds Bottom-Left, Bottom line, Bottom-Right.

## 5. Composition and Recursion

Because `Panel` delegates to `child.Render`, and that child might be a `Table`, which might contain another `Panel`, the rendering recurses naturally.

*   **Coordinate Systems**: Unlike 2D pixel buffers, `Spectre.Console` is largely flow-based. Controls render "current line" segments. However, the `Segment.SplitLines` utility allows controls like `Panel` or `Columns` to treat the output of their children as 2D blocks of text that can be manipulated (wrapped with borders, placed side-by-side).
*   **State**: The `RenderOptions` object is immutable (a C# `record`). Modifications (like changing `Height` or `Justification`) are done via `with` expressions, ensuring isolation between branches of the render tree.

## Summary

1.  **Measure**: Parents ask children "How big do you want to be given width X?"
2.  **Render**: Parents ask children "Give me your segments for width Y."
3.  **Process**: Parents manipulate those segments (splitting into lines, padding, coloring) and wrap them with their own decoration (borders).
4.  **Flatten**: The final result is a flat stream of `Segment`s converted to ANSI text.
