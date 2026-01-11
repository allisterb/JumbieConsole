namespace Jumbee.Console;

using System;

public delegate void FocusableEventHandler(); 

public interface IFocusable
{
    bool IsFocused { get; set; }

    public void Focus() => IsFocused = true;

    void UnFocus() => IsFocused = false;

    event FocusableEventHandler OnFocus;

    event FocusableEventHandler OnLostFocus;
}