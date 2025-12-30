namespace Jumbee.Console;

using System;

using ConsoleGUI.Space;

public static class ControlExtensions
{
    public static T[][] Transpose<T>(this T[][] source)
    {
        if (source == null || source.Length == 0)
        {
            return Array.Empty<T[]>();
        }

        // Determine the number of rows (source.Length) and columns (source[0].Length)
        int rowCount = source.Length;
        for (int i = 1; i < rowCount; i++)
        {
            if (source[i].Length != source[0].Length)
            {
                throw new ArgumentException("All inner arrays must have the same length to transpose.");
            }
        }   
        // This assumes all inner arrays have the same length for a successful transpose
        int columnCount = source[0].Length;

        // Create the new jagged array with dimensions swapped
        T[][] result = new T[columnCount][];

        for (int i = 0; i < columnCount; i++)
        {
            // Initialize each inner array of the result with the new row count
            result[i] = new T[rowCount];
            for (int j = 0; j < rowCount; j++)
            {
                // Swap the indices (i, j) to (j, i)
                result[i][j] = source[j][i];
            }
        }

        return result;
    }

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

    public static ControlFrame WithFrame(this Control control, BorderStyle? borderStyle = null, Offset? margin = null, Color? fgColor = null, Color? bgColor = null, string? title = null) 
        => new ControlFrame(control, borderStyle, margin, fgColor, bgColor, title); 

    public static ControlFrame WithMargin(this Control control, int left, int top, int right, int bottom) => new ControlFrame(
        control,
        margin: new Offset(left, top, right, bottom)
    );
    
    public static ControlFrame WithMargin(this Control control, int offset) => control.WithMargin(offset, offset, offset, offset);
    
    public static ControlFrame WithMargin(this ControlFrame frame, int left, int top, int right, int bottom)
    {
        frame.Margin = new Offset(left, top, right, bottom);
        return frame;
    }

    public static ControlFrame WithMargin(this ControlFrame frame, int offset) => frame.WithMargin(offset, offset, offset, offset);

    public static ControlFrame WithBorder(this Control control, BorderStyle style) => new ControlFrame(control, style);
        
    public static ControlFrame WithBorder(this ControlFrame frame, BorderStyle style)
    {
        frame.BorderStyle = style;
        return frame;
    }
    public static ControlFrame WithBorderColor(this ControlFrame border, Color? fgColor = null, Color? bgColor = null)
    {
        border.Foreground = fgColor ?? border.Foreground;
        border.Background = bgColor ?? border.Background;
        return border;
    }

    public static ControlFrame WithTitle(this ControlFrame frame, string title)
    {
        frame.Title = title;
        return frame;
    }

    public static ControlFrame WithTitle(this Control control, string title) => new ControlFrame(control) { Title = title };

    public static ControlFrame WithAsciiBorder(this Control control) => new ControlFrame(control, BorderStyle.Ascii);  

    public static ControlFrame WithAsciiBorder(this ControlFrame frame)
    {
        frame.BorderStyle = BorderStyle.Ascii;
        return frame;
    }
    public static ControlFrame WithDoubleBorder(this Control control) => new ControlFrame(control, BorderStyle.Double);

    public static ControlFrame WithDoubleBorder(this ControlFrame frame)
    {
        frame.BorderStyle = BorderStyle.Double;
        return frame;
    }   

    public static ControlFrame WithHeavyBorder(this Control control) => new ControlFrame(control, BorderStyle.Heavy);

    public static ControlFrame WithHeavyBorder(this ControlFrame frame)
    {
        frame.BorderStyle = BorderStyle.Heavy;
        return frame;
    }
    
    public static ControlFrame WithRoundedBorder(this Control control) => new ControlFrame(control, BorderStyle.Rounded);

    public static ControlFrame WithRoundedBorder(this ControlFrame frame)
    {
        frame.BorderStyle = BorderStyle.Rounded;
        return frame;
    }
    
    public static ControlFrame WithSquareBorder(this Control control) => new ControlFrame(control, BorderStyle.Square);

    public static ControlFrame WithSquareBorder(this ControlFrame frame)
    {
        frame.BorderStyle = BorderStyle.Square;
        return frame;
    }          
}
