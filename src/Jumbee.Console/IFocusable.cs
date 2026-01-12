namespace Jumbee.Console;

using ConsoleGUI;
using System;

public delegate void FocusableEventHandler(); 

public interface IFocusable : ConsoleGUI.IControl
{
    bool IsFocused { get; set; }

    IFocusable FocusableControl { get; }

    public void Focus() => IsFocused = true;

    void UnFocus() => IsFocused = false;

    event FocusableEventHandler OnFocus;

    event FocusableEventHandler OnLostFocus;
}