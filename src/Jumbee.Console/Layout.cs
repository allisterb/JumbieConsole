namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System.Collections.Generic;

public interface ILayout : IControl, IDrawingContextListener, IInputListener
{
    int Rows { get; }
    
    int Columns { get; }    
    
    IControl LayoutControl { get; }

    IControl this[int row, int column] { get; }

    IEnumerable<IControl> Controls { get; }
}   

public abstract class Layout<T> : ILayout where T:ConsoleGUI.Common.Control, IDrawingContextListener
{
    protected Layout(T control)
    {
        this.control = control;
    }

    public abstract int Rows { get; }

    public abstract int Columns { get; }    
    
    public abstract IControl this[int row, int column] { get; }  
    
    public readonly T control;

    public Cell this[Position position] => control[position];   

    public Size Size => control.Size;   

    public IControl LayoutControl => control;

    public IDrawingContext Context
    {
        get => ((IControl) control).Context;
        set => ((IControl)control).Context = value;
    }

    public IEnumerable<IControl> Controls
    {
        get
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    yield return this[r, c];
                }
            }
        }
    }

    public void OnRedraw(DrawingContext drawingContext) => control.OnRedraw(drawingContext);

    public void OnUpdate(DrawingContext drawingContext, Rect rect) => control.OnUpdate(drawingContext, rect);   

    public void OnInput(InputEvent inputEvent)
    {
        foreach(var c in Controls)
        {
            if (c is IInputListener listener)
            {
                listener.OnInput(inputEvent);
                if (inputEvent.Handled)
                {
                    break;
                }
            }
        }
    }
}
