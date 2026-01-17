# ConsoleGUI Control Rendering Pipeline

This document details how controls in the `ConsoleGUI` library are rendered, focusing on the `Control` base class and how container controls like `Border` manage their children.

## Overview

ConsoleGUI uses a **pull-based rendering system** combined with a **constraint-based layout system**.

1.  **Layout (Initialization):** A top-down propagation of constraints (`MinSize`, `MaxSize`) followed by a bottom-up determination of actual `Size`. This happens in the `Initialize()` method.
2.  **Drawing (Rendering):** A top-down request for character cells via the `this[Position]` indexer. The root (ConsoleManager) iterates over the screen coordinates and asks the root control "What character is at (x, y)?". This request propagates down the visual tree, transforming coordinates as it goes.

## 1. The `Control` Base Class

The `ConsoleGUI.Common.Control` class is the foundation for all widgets. It defines the contract for participation in the layout and rendering pipeline.

### Key Properties & Methods

*   **`IDrawingContext Context`**: This is the link to the parent control.
    *   When a parent sets this property on a child, it triggers `UpdateSizeLimits()`.
    *   If the limits (`MinSize`, `MaxSize`) have changed, `Initialize()` is called.
*   **`Initialize()` (Abstract)**:
    *   **Role:** Performs layout calculations.
    *   **Responsibility:** It must determine the control's own `Size` based on the constraints provided by the `Context` and the requirements of its children (if any).
    *   **Action:** Must call `Resize(Size)` to finalize the control's dimensions.
*   **`this[Position position]` (Abstract Indexer)**:
    *   **Role:** Returns the `Cell` (character + color) at the specified **local** coordinate.
    *   **Coordinate System:** The `position` is relative to the control's top-left corner (0,0).
*   **`Resize(Size newSize)`**:
    *   Updates the `Size` property.
    *   Manages "freezing" (batching updates) to prevent flickering or inconsistent states during resize operations.

## 2. The Layout Phase (`Initialize`)

The layout phase ensures that every control knows its size and position (relative to its parent) before drawing occurs.

### Example: How `Border` Implements Layout

The `ConsoleGUI.Controls.Border` class demonstrates how a parent controls a child's layout.

1.  **Context Assignment**: When the `Border` is added to a parent (or the root), its `Context` is set. The `Border` receives its `MinSize` and `MaxSize`.
2.  **`Initialize()` Execution**:
    ```csharp
    protected override void Initialize()
    {
        using (Freeze()) // Prevents redraws until calculation is done
        {
            // 1. Calculate space occupied by the border itself
            var borderOffset = BorderPlacement.AsOffset(); // e.g., Left=1, Top=1, Right=1, Bottom=1

            // 2. Set Offset for the Child
            // This tells the DrawingContext that the child is shifted by (1,1) relative to the Border
            ContentContext?.SetOffset(BorderPlacement.AsVector());

            // 3. Constrain the Child
            // The child gets whatever space is left after subtracting the border frame
            ContentContext?.SetLimits(
                MinSize.AsRect().Remove(borderOffset).Size,
                MaxSize.AsRect().Remove(borderOffset).Size);

            // 4. Determine Border's Final Size
            // The Border's size is the Child's size + the border frame size
            var contentSize = Content?.Size ?? Size.Empty;
            Resize(contentSize.AsRect().Add(borderOffset).Size);
        }
    }
    ```
3.  **Recursion**: Setting limits on `ContentContext` (in step 3) triggers the child's `Context` setter, causing the child's `Initialize()` to run. This propagates the layout pass down the tree.

## 3. The Rendering Phase (`this[Position]`)

Rendering is triggered by the `ConsoleManager`. It does not "push" drawing commands. Instead, it iterates over the screen buffer and "pulls" the correct cell for every coordinate.

### The Pipeline Flow

1.  **ConsoleManager Loop**:
    The `ConsoleManager` iterates through screen coordinates `(x, y)`:
    ```csharp
    var cell = ContentContext[new Position(x, y)];
    ```
2.  **DrawingContext Translation**:
    The `DrawingContext` wrapping the root control receives the call. It translates the global coordinate to a local coordinate if necessary (though the root usually has 0 offset) and calls the root control's indexer.

3.  **Control Indexer (e.g., `Border`)**:
    The `Border` receives a request for a cell at local position `pos`.
    ```csharp
    public override Cell this[Position position]
    {
        get
        {
            // 1. Check if the position is within the child's area
            if (ContentContext.Contains(position))
                // Delegate to the child (via the context)
                return ContentContext[position];

            // 2. If not in child area, draw the border frame
            if (position.X == 0 && position.Y == 0) return _borderStyle.TopLeft;
            // ... checks for other border segments ...

            return Character.Empty; // Default fallback
        }
    }
    ```

4.  **DrawingContext Propagation**:
    When `Border` calls `ContentContext[position]`, the `DrawingContext`:
    *   Subtracts its **Offset** from the position (`position - Offset`).
    *   Calls `Child[shiftedPosition]`.

    *Example*: If `Border` is at (0,0) and has a 1px frame, and the `ConsoleManager` asks for (5, 5):
    *   `Border` sees (5,5). It is inside the content area.
    *   `Border` calls `ContentContext[(5,5)]`.
    *   `ContentContext` has an offset of (1,1). It calls `Child[(4,4)]`.
    *   The `Child` returns the cell at its local (4,4).

5.  **Termination**:
    The call chain ends at a leaf control (like `TextBlock`), which returns a specific character (e.g., 'A') or `Character.Empty` (transparent).

6.  **Composition**:
    The `Cell` travels back up the stack. Intermediate controls can modify it.
    *   *Example*: A `Background` control might receive the cell from its child and replace the `BackgroundColor` if the child's background was null.
    *   *Example*: A `Button` might change the background color if it is currently hovered or clicked.

## Summary: The Life Cycle of a Pixel

1.  **Creation**: `ConsoleManager.Update(Rect)` decides which part of the screen needs refreshing.
2.  **Request**: It asks the Root Context for the `Cell` at `(x, y)`.
3.  **Translation**: The request travels down through `DrawingContext`s (adjusting xy for offsets) and `Control`s (determining if the child or the control itself handles that pixel).
4.  **Generation**: A leaf control generates the `Cell`.
5.  **Decoration**: The `Cell` bubbles up, potentially picking up background colors or styles from parent containers.
6.  **Buffering**: `ConsoleManager` compares the result with its internal `ConsoleBuffer`.
7.  **Output**: If different, the character is written to the real `System.Console`.
