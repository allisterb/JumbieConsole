namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

public enum LayoutKeyboardNavigation
{
    Up,
    Down,
    Left,
    Right
}

public interface ILayout : IFocusable, IDrawingContextListener, IInputListener
{
    int Rows { get; }
    
    int Columns { get; }    
    
    IControl CControl { get; }

    IFocusable this[int row, int column] { get; }

    IEnumerable<IFocusable> Controls { get; }

    IFocusable[] InputListeners { get; }

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
    public abstract IFocusable this[int row, int column] { get; }
    #endregion

    #region Properties
    public abstract int Rows { get; }

    public abstract int Columns { get; }    
        
    public IFocusable[] InputListeners => inputListeners;

    public Dictionary<ConsoleKeyInfo, LayoutKeyboardNavigation> NavigationKeys { get; } = new Dictionary<ConsoleKeyInfo, LayoutKeyboardNavigation>();
   
    public Cell this[Position position] => control[position];   

    public Size Size => control.Size;   

    public IControl CControl => control;
    
    public IDrawingContext Context
    {
        get => ((IControl) control).Context;
        set => ((IControl)control).Context = value;
    }

    public IEnumerable<IFocusable> Controls
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

    public bool Focusable { get; set; } = true;

    public IFocusable FocusableControl => this;

    public bool IsFocused
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                if (value)
                    OnFocus?.Invoke();
                else
                    OnLostFocus?.Invoke();
            }
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

    public void OnInput(InputEvent inputEvent) => Array.ForEach(inputListeners, il => il.FocusedControl?.OnInput(inputEvent));

    protected void UpdateInputListeners()
    {        
        inputListeners = 
            Controls            
            .Where(c => c is not null) // Must handle case where controls might not be fully initialized when called from constructor             
            .ToArray();
    }
    #endregion

    #region Fields
    public readonly T control;
    protected IFocusable[] inputListeners = Array.Empty<IFocusable>();
    #endregion
}
