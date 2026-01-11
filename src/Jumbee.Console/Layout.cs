namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System;
using System.Collections.Generic;

public enum LayoutKeyboardNavigation
{
    Up,
    Down,
    Left,
    Right
}

public interface ILayout : IControl, IDrawingContextListener, IInputListener, IFocusable
{
    int Rows { get; }
    
    int Columns { get; }    
    
    IControl LayoutControl { get; }

    IControl this[int row, int column] { get; }

    IEnumerable<IControl> Controls { get; }

    Dictionary <ConsoleKeyInfo, LayoutKeyboardNavigation> NavigationKeys { get; }
}   

public abstract class Layout<T> : ILayout where T:CControl, IDrawingContextListener
{
    #region Constructors
    protected Layout(T control)
    {
        this.control = control;
    }
    #endregion

    #region Indexers
    public abstract IControl this[int row, int column] { get; }
    #endregion

    #region Properties
    public abstract int Rows { get; }

    public abstract int Columns { get; }    
        
    public Dictionary<ConsoleKeyInfo, LayoutKeyboardNavigation> NavigationKeys { get; } = new Dictionary<ConsoleKeyInfo, LayoutKeyboardNavigation>();
   
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

    public bool IsFocused
    {
        get => field;
        set
        {
            field = value;
            if (value)
                OnFocus?.Invoke();
            else
                OnLostFocus?.Invoke();           
        }
    }
    #endregion

    #region Events
    public event FocusableEventHandler? OnFocus;

    public event FocusableEventHandler? OnLostFocus;
    #endregion

    #region Methods
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
    #endregion

    #region Fields
    public readonly T control;
    #endregion
}
