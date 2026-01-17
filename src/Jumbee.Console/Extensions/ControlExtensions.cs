namespace Jumbee.Console;

using ConsoleGUI.Common;
using ConsoleGUI.Space;
using System;

public static class ControlExtensions
{    
    public static void Deconstruct(this Position position, out int X, out int Y)
    { 
        X = position.X;
        Y = position.Y;
    }

    public static Position SubtractClamp(this Position position1, Position position2)
    {
        var x = position1.X - position2.X;  
        var y = position1.Y - position2.Y;  
        return new Position(Math.Max(0, x), Math.Max(0, y));
    }

    public static Position Add(this Position position, int x, int y) => new Position(position.X + x, position.Y + y);

    public static Control WithFrame(this Control control, ControlFrame frame)
    {
        control.Frame = frame;
        return control;
    }
    public static Control WithFrame(this Control control, BorderStyle? borderStyle = null, Offset? margin = null, Color? fgColor = null, Color? bgColor = null, string? title = null, Color? borderFgColor = null, Color? borderBgColor = null)
    {
        var frame = control.Frame ??= new ControlFrame(control);         
        frame.BorderStyle = borderStyle ?? frame.BorderStyle;   
        frame.Margin = margin ?? frame.Margin;
        frame.Foreground = fgColor ?? frame.Foreground;
        frame.Background = bgColor ?? frame.Background;
        frame.Title = title ?? frame.Title;
        frame.BorderFgColor = borderFgColor ?? frame.BorderFgColor;
        frame.BorderBgColor = borderBgColor ?? frame.BorderBgColor; 
        return control;
    }

    public static Control WithMargin(this Control control, int left, int top, int right, int bottom)
    {
        if (control.Frame != null)
        {
            control.Frame.Margin = new Offset(left, top, right, bottom);
            return control;
        }
        else
        {
            control.Frame = new ControlFrame(control, margin: new Offset(left, top, right, bottom));
            return control; 
        }
    }
    
    public static Control WithMargin(this Control control, int offset) => control.WithMargin(offset, offset, offset, offset);
    
    public static Control WithBorder(this Control control, BorderStyle? style, Color? borderFgColor = null, Color? borderBgColor = null)
    {
        var frame = control.Frame ??= new ControlFrame(control);
        frame.BorderStyle = style ?? frame.BorderStyle;
        frame.BorderFgColor = borderFgColor ?? frame.BorderFgColor;
        frame.BorderBgColor = borderBgColor ?? frame.BorderBgColor;
        return control;
    }

    public static Control WithTitle(this Control control, string title)
    {
        var frame = control.Frame ??= new ControlFrame(control);
        frame.Title = title;
        return control;
    }

    public static Control WithNoBorder(this Control control) =>
        control.WithBorder(BorderStyle.None);

    public static Control WithAsciiBorder(this Control control, Color? borderFgColor = null, Color? borderBgColor = null) =>
        control.WithBorder(BorderStyle.Ascii, borderFgColor, borderBgColor);

    public static Control WithHeavyBorder(this Control control, Color? borderFgColor = null, Color? borderBgColor = null) =>
         control.WithBorder(BorderStyle.Heavy, borderFgColor, borderBgColor);

    public static Control WithDoubleBorder(this Control control, Color? borderFgColor = null, Color? borderBgColor = null) =>
        control.WithBorder(BorderStyle.Double, borderFgColor, borderBgColor);

    public static Control WithRoundedBorder(this Control control, Color? borderFgColor = null, Color? borderBgColor = null) =>
        control.WithBorder(BorderStyle.Rounded, borderFgColor, borderBgColor);

    public static Control WithSquareBorder(this Control control, Color? borderFgColor = null, Color? borderBgColor = null) =>
        control.WithBorder(BorderStyle.Square, borderFgColor, borderBgColor);

    public static string WithStyle(this string s, Style style) => style[s];
}
