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

    public static T WithWidth<T>(this T control, int width) where T : Control
    {
        control.Width = width;
        return control;
    }

    public static T WithHeight<T>(this T control, int height) where T : Control
    {
        control.Height = height;
        return control;
    }

    public static T WithSize<T>(this T control, int? width = null, int? height = null) where T : Control
    {
        if (width is null && height is null) throw new ArgumentNullException("You must specify either a width or height.");

        if (width.HasValue)
        {
            control.Width = width.Value;
        }
        if (height.HasValue)
        {
            control.Height = height.Value;
        }
        return control;
    }

    public static T WithFrame<T>(this T control, ControlFrame frame) where T : Control
    {
        control.Frame = frame;
        return control;
    }
    public static T WithFrame<T>(this T control, BorderStyle? borderStyle = null, Offset? margin = null, Color? fgColor = null, Color? bgColor = null, string? title = null, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
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

    public static T WithMargin<T>(this T control, int left, int top, int right, int bottom) where T : Control
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

    public static T WithMargin<T>(this T control, int offset) where T : Control => control.WithMargin(offset, offset, offset, offset);

    public static T WithBorder<T>(this T control, BorderStyle? style, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
    {
        var frame = control.Frame ??= new ControlFrame(control);
        frame.BorderStyle = style ?? frame.BorderStyle;
        frame.BorderFgColor = borderFgColor ?? frame.BorderFgColor;
        frame.BorderBgColor = borderBgColor ?? frame.BorderBgColor;
        return control;
    }

    public static T WithTitle<T>(this T control, string title) where T : Control
    {
        var frame = control.Frame ??= new ControlFrame(control);
        frame.Title = title;
        return control;
    }

    public static T WithNoBorder<T>(this T control) where T : Control =>
        control.WithBorder(BorderStyle.None);

    public static T WithAsciiBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control =>
        control.WithBorder(BorderStyle.Ascii, borderFgColor, borderBgColor);

    public static T WithHeavyBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control =>
         control.WithBorder(BorderStyle.Heavy, borderFgColor, borderBgColor);

    public static T WithDoubleBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control =>
        control.WithBorder(BorderStyle.Double, borderFgColor, borderBgColor);

    public static T WithRoundedBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control =>
        control.WithBorder(BorderStyle.Rounded, borderFgColor, borderBgColor);

    public static T WithSquareBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control =>
        control.WithBorder(BorderStyle.Square, borderFgColor, borderBgColor);

    public static Spectre.Console.Markup WithStyle(this string s, Style style) => style[s];
}
