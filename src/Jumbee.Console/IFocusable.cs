namespace Jumbee.Console;

using ConsoleGUI;
using System;

public delegate void FocusableEventHandler(); 

public interface IFocusable : IControl
{
    bool Focusable { get; set; }
    
    bool IsFocused { get; set; }

    IFocusable FocusableControl { get; }
    
    event FocusableEventHandler OnFocus;

    event FocusableEventHandler OnLostFocus;

    void Focus() => IsFocused = true;

    void UnFocus() => IsFocused = false;
}