namespace Jumbee.Console;

using ConsoleGUIColor = ConsoleGUI.Data.Color;
using SpectreColor = Spectre.Console.Color;
using SystemDrawingColor = System.Drawing.Color;
public readonly struct Color 
{
    #region Constructors
    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
        SystemColor = SystemDrawingColor.FromArgb(r, g, b);
    }
    #endregion

    #region Properties
    
    public SystemDrawingColor SystemColor { get; }
    #endregion
    
    #region Methods
    public SpectreColor ToSpectreColor() => new SpectreColor(R, G, B);
    

    public static Color FromSpectreColor(SpectreColor color) => new Color(color.R, color.G, color.B);
    

    public static ConsoleGUIColor? ToConsoleGUIColor(SpectreColor color)
    {
        if (color == SpectreColor.Default)
        {
            return null;
        }

        return new ConsoleGUIColor(color.R, color.G, color.B);
    }

    public ConsoleGUIColor ToConsoleGUIColor() => new ConsoleGUIColor(R, G, B);    

    public static Color FromConsoleGUIColor(ConsoleGUIColor color) => new Color(color.Red, color.Green, color.Blue);
    
    #endregion

    #region Operators
    public static implicit operator SpectreColor(Color color) => color.ToSpectreColor();

    public static implicit operator Color(SpectreColor color) => FromSpectreColor(color);

    public static implicit operator ConsoleGUIColor(Color color) => color.ToConsoleGUIColor();

    public static implicit operator Color(ConsoleGUIColor color) => FromConsoleGUIColor(color);

    public static implicit operator SystemDrawingColor(Color color) => color.SystemColor;
    #endregion

    #region Fields
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;

    // The following color names are imported from the Spectre.Console definitions.

    public static readonly Color Black = FromSpectreColor(SpectreColor.Black);
    /// <summary>
    /// Gets the color "Maroon" (RGB 128,0,0).
    /// </summary>
    public static readonly Color Maroon = FromSpectreColor(SpectreColor.Maroon);

    /// <summary>
    /// Gets the color "Green" (RGB 0,128,0).
    /// </summary>
    public static readonly Color Green = FromSpectreColor(SpectreColor.Green);

    /// <summary>
    /// Gets the color "Olive" (RGB 128,128,0).
    /// </summary>
    public static readonly Color Olive = FromSpectreColor(SpectreColor.Olive);

    /// <summary>
    /// Gets the color "Navy" (RGB 0,0,128).
    /// </summary>
    public static readonly Color Navy = FromSpectreColor(SpectreColor.Navy);

    /// <summary>
    /// Gets the color "Purple" (RGB 128,0,128).
    /// </summary>
    public static readonly Color Purple = FromSpectreColor(SpectreColor.Purple);

    /// <summary>
    /// Gets the color "Teal" (RGB 0,128,128).
    /// </summary>
    public static readonly Color Teal = FromSpectreColor(SpectreColor.Teal);

    /// <summary>
    /// Gets the color "Silver" (RGB 192,192,192).
    /// </summary>
    public static readonly Color Silver = FromSpectreColor(SpectreColor.Silver);

    /// <summary>
    /// Gets the color "Grey" (RGB 128,128,128).
    /// </summary>
    public static readonly Color Grey = FromSpectreColor(SpectreColor.Grey);

    /// <summary>
    /// Gets the color "Red" (RGB 255,0,0).
    /// </summary>
    public static readonly Color Red = FromSpectreColor(SpectreColor.Red);

    /// <summary>
    /// Gets the color "Lime" (RGB 0,255,0).
    /// </summary>
    public static readonly Color Lime = FromSpectreColor(SpectreColor.Lime);

    /// <summary>
    /// Gets the color "Yellow" (RGB 255,255,0).
    /// </summary>
    public static readonly Color Yellow = FromSpectreColor(SpectreColor.Yellow);

    /// <summary>
    /// Gets the color "Blue" (RGB 0,0,255).
    /// </summary>
    public static readonly Color Blue = FromSpectreColor(SpectreColor.Blue);

    /// <summary>
    /// Gets the color "Fuchsia" (RGB 255,0,255).
    /// </summary>
    public static readonly Color Fuchsia = FromSpectreColor(SpectreColor.Fuchsia);

    /// <summary>
    /// Gets the color "Aqua" (RGB 0,255,255).
    /// </summary>
    public static readonly Color Aqua = FromSpectreColor(SpectreColor.Aqua);

    /// <summary>
    /// Gets the color "White" (RGB 255,255,255).
    /// </summary>
    public static readonly Color White = FromSpectreColor(SpectreColor.White);

    /// <summary>
    /// Gets the color "Grey0" (RGB 0,0,0).
    /// </summary>
    public static readonly Color Grey0 = FromSpectreColor(SpectreColor.Grey0);

    /// <summary>
    /// Gets the color "NavyBlue" (RGB 0,0,95).
    /// </summary>
    public static readonly Color NavyBlue = FromSpectreColor(SpectreColor.NavyBlue);

    /// <summary>
    /// Gets the color "DarkBlue" (RGB 0,0,135).
    /// </summary>
    public static readonly Color DarkBlue = FromSpectreColor(SpectreColor.DarkBlue);

    /// <summary>
    /// Gets the color "Blue3" (RGB 0,0,175).
    /// </summary>
    public static readonly Color Blue3 = FromSpectreColor(SpectreColor.Blue3);

    /// <summary>
    /// Gets the color "Blue3_1" (RGB 0,0,215).
    /// </summary>
    public static readonly Color Blue3_1 = FromSpectreColor(SpectreColor.Blue3_1);

    /// <summary>
    /// Gets the color "Blue1" (RGB 0,0,255).
    /// </summary>
    public static readonly Color Blue1 = FromSpectreColor(SpectreColor.Blue1);

    /// <summary>
    /// Gets the color "DarkGreen" (RGB 0,95,0).
    /// </summary>
    public static readonly Color DarkGreen = FromSpectreColor(SpectreColor.DarkGreen);

    /// <summary>
    /// Gets the color "DeepSkyBlue4" (RGB 0,95,95).
    /// </summary>
    public static readonly Color DeepSkyBlue4 = FromSpectreColor(SpectreColor.DeepSkyBlue4);

    /// <summary>
    /// Gets the color "DeepSkyBlue4_1" (RGB 0,95,135).
    /// </summary>
    public static readonly Color DeepSkyBlue4_1 = FromSpectreColor(SpectreColor.DeepSkyBlue4_1);

    /// <summary>
    /// Gets the color "DeepSkyBlue4_2" (RGB 0,95,175).
    /// </summary>
    public static readonly Color DeepSkyBlue4_2 = FromSpectreColor(SpectreColor.DeepSkyBlue4_2);

    /// <summary>
    /// Gets the color "DodgerBlue3" (RGB 0,95,215).
    /// </summary>
    public static readonly Color DodgerBlue3 = FromSpectreColor(SpectreColor.DodgerBlue3);

    /// <summary>
    /// Gets the color "DodgerBlue2" (RGB 0,95,255).
    /// </summary>
    public static readonly Color DodgerBlue2 = FromSpectreColor(SpectreColor.DodgerBlue2);

    /// <summary>
    /// Gets the color "Green4" (RGB 0,135,0).
    /// </summary>
    public static readonly Color Green4 = FromSpectreColor(SpectreColor.Green4);

    /// <summary>
    /// Gets the color "SpringGreen4" (RGB 0,135,95).
    /// </summary>
    public static readonly Color SpringGreen4 = FromSpectreColor(SpectreColor.SpringGreen4);

    /// <summary>
    /// Gets the color "Turquoise4" (RGB 0,135,135).
    /// </summary>
    public static readonly Color Turquoise4 = FromSpectreColor(SpectreColor.Turquoise4);

    /// <summary>
    /// Gets the color "DeepSkyBlue3" (RGB 0,135,175).
    /// </summary>
    public static readonly Color DeepSkyBlue3 = FromSpectreColor(SpectreColor.DeepSkyBlue3);

    /// <summary>
    /// Gets the color "DeepSkyBlue3_1" (RGB 0,135,215).
    /// </summary>
    public static readonly Color DeepSkyBlue3_1 = FromSpectreColor(SpectreColor.DeepSkyBlue3_1);

    /// <summary>
    /// Gets the color "DodgerBlue1" (RGB 0,135,255).
    /// </summary>
    public static readonly Color DodgerBlue1 = FromSpectreColor(SpectreColor.DodgerBlue1);

    /// <summary>
    /// Gets the color "Green3" (RGB 0,175,0).
    /// </summary>
    public static readonly Color Green3 = FromSpectreColor(SpectreColor.Green3);

    /// <summary>
    /// Gets the color "SpringGreen3" (RGB 0,175,95).
    /// </summary>
    public static readonly Color SpringGreen3 = FromSpectreColor(SpectreColor.SpringGreen3);

    /// <summary>
    /// Gets the color "DarkCyan" (RGB 0,175,135).
    /// </summary>
    public static readonly Color DarkCyan = FromSpectreColor(SpectreColor.DarkCyan);

    /// <summary>
    /// Gets the color "LightSeaGreen" (RGB 0,175,175).
    /// </summary>
    public static readonly Color LightSeaGreen = FromSpectreColor(SpectreColor.LightSeaGreen);

    /// <summary>
    /// Gets the color "DeepSkyBlue2" (RGB 0,175,215).
    /// </summary>
    public static readonly Color DeepSkyBlue2 = FromSpectreColor(SpectreColor.DeepSkyBlue2);

    /// <summary>
    /// Gets the color "DeepSkyBlue1" (RGB 0,175,255).
    /// </summary>
    public static readonly Color DeepSkyBlue1 = FromSpectreColor(SpectreColor.DeepSkyBlue1);

    /// <summary>
    /// Gets the color "Green3_1" (RGB 0,215,0).
    /// </summary>
    public static readonly Color Green3_1 = FromSpectreColor(SpectreColor.Green3_1);

    /// <summary>
    /// Gets the color "SpringGreen3_1" (RGB 0,215,95).
    /// </summary>
    public static readonly Color SpringGreen3_1 = FromSpectreColor(SpectreColor.SpringGreen3_1);

    /// <summary>
    /// Gets the color "SpringGreen2" (RGB 0,215,135).
    /// </summary>
    public static readonly Color SpringGreen2 = FromSpectreColor(SpectreColor.SpringGreen2);

    /// <summary>
    /// Gets the color "Cyan3" (RGB 0,215,175).
    /// </summary>
    public static readonly Color Cyan3 = FromSpectreColor(SpectreColor.Cyan3);

    /// <summary>
    /// Gets the color "DarkTurquoise" (RGB 0,215,215).
    /// </summary>
    public static readonly Color DarkTurquoise = FromSpectreColor(SpectreColor.DarkTurquoise);

    /// <summary>
    /// Gets the color "Turquoise2" (RGB 0,215,255).
    /// </summary>
    public static readonly Color Turquoise2 = FromSpectreColor(SpectreColor.Turquoise2);

    /// <summary>
    /// Gets the color "Green1" (RGB 0,255,0).
    /// </summary>
    public static readonly Color Green1 = FromSpectreColor(SpectreColor.Green1);

    /// <summary>
    /// Gets the color "SpringGreen2_1" (RGB 0,255,95).
    /// </summary>
    public static readonly Color SpringGreen2_1 = FromSpectreColor(SpectreColor.SpringGreen2_1);

    /// <summary>
    /// Gets the color "SpringGreen1" (RGB 0,255,135).
    /// </summary>
    public static readonly Color SpringGreen1 = FromSpectreColor(SpectreColor.SpringGreen1);

    /// <summary>
    /// Gets the color "MediumSpringGreen" (RGB 0,255,175).
    /// </summary>
    public static readonly Color MediumSpringGreen = FromSpectreColor(SpectreColor.MediumSpringGreen);

    /// <summary>
    /// Gets the color "Cyan2" (RGB 0,255,215).
    /// </summary>
    public static readonly Color Cyan2 = FromSpectreColor(SpectreColor.Cyan2);

    /// <summary>
    /// Gets the color "Cyan1" (RGB 0,255,255).
    /// </summary>
    public static readonly Color Cyan1 = FromSpectreColor(SpectreColor.Cyan1);

    /// <summary>
    /// Gets the color "DarkRed" (RGB 95,0,0).
    /// </summary>
    public static readonly Color DarkRed = FromSpectreColor(SpectreColor.DarkRed);

    /// <summary>
    /// Gets the color "DeepPink4" (RGB 95,0,95).
    /// </summary>
    public static readonly Color DeepPink4 = FromSpectreColor(SpectreColor.DeepPink4);

    /// <summary>
    /// Gets the color "Purple4" (RGB 95,0,135).
    /// </summary>
    public static readonly Color Purple4 = FromSpectreColor(SpectreColor.Purple4);

    /// <summary>
    /// Gets the color "Purple4_1" (RGB 95,0,175).
    /// </summary>
    public static readonly Color Purple4_1 = FromSpectreColor(SpectreColor.Purple4_1);

    /// <summary>
    /// Gets the color "Purple3" (RGB 95,0,215).
    /// </summary>
    public static readonly Color Purple3 = FromSpectreColor(SpectreColor.Purple3);

    /// <summary>
    /// Gets the color "BlueViolet" (RGB 95,0,255).
    /// </summary>
    public static readonly Color BlueViolet = FromSpectreColor(SpectreColor.BlueViolet);

    /// <summary>
    /// Gets the color "Orange4" (RGB 95,95,0).
    /// </summary>
    public static readonly Color Orange4 = FromSpectreColor(SpectreColor.Orange4);

    /// <summary>
    /// Gets the color "Grey37" (RGB 95,95,95).
    /// </summary>
    public static readonly Color Grey37 = FromSpectreColor(SpectreColor.Grey37);

    /// <summary>
    /// Gets the color "MediumPurple4" (RGB 95,95,135).
    /// </summary>
    public static readonly Color MediumPurple4 = FromSpectreColor(SpectreColor.MediumPurple4);

    /// <summary>
    /// Gets the color "SlateBlue3" (RGB 95,95,175).
    /// </summary>
    public static readonly Color SlateBlue3 = FromSpectreColor(SpectreColor.SlateBlue3);

    /// <summary>
    /// Gets the color "SlateBlue3_1" (RGB 95,95,215).
    /// </summary>
    public static readonly Color SlateBlue3_1 = FromSpectreColor(SpectreColor.SlateBlue3_1);

    /// <summary>
    /// Gets the color "RoyalBlue1" (RGB 95,95,255).
    /// </summary>
    public static readonly Color RoyalBlue1 = FromSpectreColor(SpectreColor.RoyalBlue1);

    /// <summary>
    /// Gets the color "Chartreuse4" (RGB 95,135,0).
    /// </summary>
    public static readonly Color Chartreuse4 = FromSpectreColor(SpectreColor.Chartreuse4);

    /// <summary>
    /// Gets the color "DarkSeaGreen4" (RGB 95,135,95).
    /// </summary>
    public static readonly Color DarkSeaGreen4 = FromSpectreColor(SpectreColor.DarkSeaGreen4);

    /// <summary>
    /// Gets the color "PaleTurquoise4" (RGB 95,135,135).
    /// </summary>
    public static readonly Color PaleTurquoise4 = FromSpectreColor(SpectreColor.PaleTurquoise4);

    /// <summary>
    /// Gets the color "SteelBlue" (RGB 95,135,175).
    /// </summary>
    public static readonly Color SteelBlue = FromSpectreColor(SpectreColor.SteelBlue);

    /// <summary>
    /// Gets the color "SteelBlue3" (RGB 95,135,215).
    /// </summary>
    public static readonly Color SteelBlue3 = FromSpectreColor(SpectreColor.SteelBlue3);

    /// <summary>
    /// Gets the color "CornflowerBlue" (RGB 95,135,255).
    /// </summary>
    public static readonly Color CornflowerBlue = FromSpectreColor(SpectreColor.CornflowerBlue);

    /// <summary>
    /// Gets the color "Chartreuse3" (RGB 95,175,0).
    /// </summary>
    public static readonly Color Chartreuse3 = FromSpectreColor(SpectreColor.Chartreuse3);

    /// <summary>
    /// Gets the color "DarkSeaGreen4_1" (RGB 95,175,95).
    /// </summary>
    public static readonly Color DarkSeaGreen4_1 = FromSpectreColor(SpectreColor.DarkSeaGreen4_1);

    /// <summary>
    /// Gets the color "CadetBlue" (RGB 95,175,135).
    /// </summary>
    public static readonly Color CadetBlue = FromSpectreColor(SpectreColor.CadetBlue);

    /// <summary>
    /// Gets the color "CadetBlue_1" (RGB 95,175,175).
    /// </summary>
    public static readonly Color CadetBlue_1 = FromSpectreColor(SpectreColor.CadetBlue_1);

    /// <summary>
    /// Gets the color "SkyBlue3" (RGB 95,175,215).
    /// </summary>
    public static readonly Color SkyBlue3 = FromSpectreColor(SpectreColor.SkyBlue3);

    /// <summary>
    /// Gets the color "SteelBlue1" (RGB 95,175,255).
    /// </summary>
    public static readonly Color SteelBlue1 = FromSpectreColor(SpectreColor.SteelBlue1);

    /// <summary>
    /// Gets the color "Chartreuse3_1" (RGB 95,215,0).
    /// </summary>
    public static readonly Color Chartreuse3_1 = FromSpectreColor(SpectreColor.Chartreuse3_1);

    /// <summary>
    /// Gets the color "PaleGreen3" (RGB 95,215,95).
    /// </summary>
    public static readonly Color PaleGreen3 = FromSpectreColor(SpectreColor.PaleGreen3);

    /// <summary>
    /// Gets the color "SeaGreen3" (RGB 95,215,135).
    /// </summary>
    public static readonly Color SeaGreen3 = FromSpectreColor(SpectreColor.SeaGreen3);

    /// <summary>
    /// Gets the color "Aquamarine3" (RGB 95,215,175).
    /// </summary>
    public static readonly Color Aquamarine3 = FromSpectreColor(SpectreColor.Aquamarine3);

    /// <summary>
    /// Gets the color "MediumTurquoise" (RGB 95,215,215).
    /// </summary>
    public static readonly Color MediumTurquoise = FromSpectreColor(SpectreColor.MediumTurquoise);

    /// <summary>
    /// Gets the color "SteelBlue1_1" (RGB 95,215,255).
    /// </summary>
    public static readonly Color SteelBlue1_1 = FromSpectreColor(SpectreColor.SteelBlue1_1);

    /// <summary>
    /// Gets the color "Chartreuse2" (RGB 95,255,0).
    /// </summary>
    public static readonly Color Chartreuse2 = FromSpectreColor(SpectreColor.Chartreuse2);

    /// <summary>
    /// Gets the color "SeaGreen2" (RGB 95,255,95).
    /// </summary>
    public static readonly Color SeaGreen2 = FromSpectreColor(SpectreColor.SeaGreen2);

    /// <summary>
    /// Gets the color "SeaGreen1" (RGB 95,255,135).
    /// </summary>
    public static readonly Color SeaGreen1 = FromSpectreColor(SpectreColor.SeaGreen1);

    /// <summary>
    /// Gets the color "SeaGreen1_1" (RGB 95,255,175).
    /// </summary>
    public static readonly Color SeaGreen1_1 = FromSpectreColor(SpectreColor.SeaGreen1_1);

    /// <summary>
    /// Gets the color "Aquamarine1" (RGB 95,255,215).
    /// </summary>
    public static readonly Color Aquamarine1 = FromSpectreColor(SpectreColor.Aquamarine1);

    /// <summary>
    /// Gets the color "DarkSlateGray2" (RGB 95,255,255).
    /// </summary>
    public static readonly Color DarkSlateGray2 = FromSpectreColor(SpectreColor.DarkSlateGray2);

    /// <summary>
    /// Gets the color "DarkRed_1" (RGB 135,0,0).
    /// </summary>
    public static readonly Color DarkRed_1 = FromSpectreColor(SpectreColor.DarkRed_1);

    /// <summary>
    /// Gets the color "DeepPink4_1" (RGB 135,0,95).
    /// </summary>
    public static readonly Color DeepPink4_1 = FromSpectreColor(SpectreColor.DeepPink4_1);

    /// <summary>
    /// Gets the color "DarkMagenta" (RGB 135,0,135).
    /// </summary>
    public static readonly Color DarkMagenta = FromSpectreColor(SpectreColor.DarkMagenta);

    /// <summary>
    /// Gets the color "DarkMagenta_1" (RGB 135,0,175).
    /// </summary>
    public static readonly Color DarkMagenta_1 = FromSpectreColor(SpectreColor.DarkMagenta_1);

    /// <summary>
    /// Gets the color "DarkViolet" (RGB 135,0,215).
    /// </summary>
    public static readonly Color DarkViolet = FromSpectreColor(SpectreColor.DarkViolet);

    /// <summary>
    /// Gets the color "Purple_1" (RGB 135,0,255).
    /// </summary>
    public static readonly Color Purple_1 = FromSpectreColor(SpectreColor.Purple_1);

    /// <summary>
    /// Gets the color "Orange4_1" (RGB 135,95,0).
    /// </summary>
    public static readonly Color Orange4_1 = FromSpectreColor(SpectreColor.Orange4_1);

    /// <summary>
    /// Gets the color "LightPink4" (RGB 135,95,95).
    /// </summary>
    public static readonly Color LightPink4 = FromSpectreColor(SpectreColor.LightPink4);

    /// <summary>
    /// Gets the color "Plum4" (RGB 135,95,135).
    /// </summary>
    public static readonly Color Plum4 = FromSpectreColor(SpectreColor.Plum4);

    /// <summary>
    /// Gets the color "MediumPurple3" (RGB 135,95,175).
    /// </summary>
    public static readonly Color MediumPurple3 = FromSpectreColor(SpectreColor.MediumPurple3);

    /// <summary>
    /// Gets the color "MediumPurple3_1" (RGB 135,95,215).
    /// </summary>
    public static readonly Color MediumPurple3_1 = FromSpectreColor(SpectreColor.MediumPurple3_1);

    /// <summary>
    /// Gets the color "SlateBlue1" (RGB 135,95,255).
    /// </summary>
    public static readonly Color SlateBlue1 = FromSpectreColor(SpectreColor.SlateBlue1);

    /// <summary>
    /// Gets the color "Yellow4" (RGB 135,135,0).
    /// </summary>
    public static readonly Color Yellow4 = FromSpectreColor(SpectreColor.Yellow4);

    /// <summary>
    /// Gets the color "Wheat4" (RGB 135,135,95).
    /// </summary>
    public static readonly Color Wheat4 = FromSpectreColor(SpectreColor.Wheat4);

    /// <summary>
    /// Gets the color "Grey53" (RGB 135,135,135).
    /// </summary>
    public static readonly Color Grey53 = FromSpectreColor(SpectreColor.Grey53);

    /// <summary>
    /// Gets the color "LightSlateGrey" (RGB 135,135,175).
    /// </summary>
    public static readonly Color LightSlateGrey = FromSpectreColor(SpectreColor.LightSlateGrey);

    /// <summary>
    /// Gets the color "MediumPurple" (RGB 135,135,215).
    /// </summary>
    public static readonly Color MediumPurple = FromSpectreColor(SpectreColor.MediumPurple);

    /// <summary>
    /// Gets the color "LightSlateBlue" (RGB 135,135,255).
    /// </summary>
    public static readonly Color LightSlateBlue = FromSpectreColor(SpectreColor.LightSlateBlue);

    /// <summary>
    /// Gets the color "Yellow4_1" (RGB 135,175,0).
    /// </summary>
    public static readonly Color Yellow4_1 = FromSpectreColor(SpectreColor.Yellow4_1);

    /// <summary>
    /// Gets the color "DarkOliveGreen3" (RGB 135,175,95).
    /// </summary>
    public static readonly Color DarkOliveGreen3 = FromSpectreColor(SpectreColor.DarkOliveGreen3);

    /// <summary>
    /// Gets the color "DarkSeaGreen" (RGB 135,175,135).
    /// </summary>
    public static readonly Color DarkSeaGreen = FromSpectreColor(SpectreColor.DarkSeaGreen);

    /// <summary>
    /// Gets the color "LightSkyBlue3" (RGB 135,175,175).
    /// </summary>
    public static readonly Color LightSkyBlue3 = FromSpectreColor(SpectreColor.LightSkyBlue3);

    /// <summary>
    /// Gets the color "LightSkyBlue3_1" (RGB 135,175,215).
    /// </summary>
    public static readonly Color LightSkyBlue3_1 = FromSpectreColor(SpectreColor.LightSkyBlue3_1);

    /// <summary>
    /// Gets the color "SkyBlue2" (RGB 135,175,255).
    /// </summary>
    public static readonly Color SkyBlue2 = FromSpectreColor(SpectreColor.SkyBlue2);

    /// <summary>
    /// Gets the color "Chartreuse2_1" (RGB 135,215,0).
    /// </summary>
    public static readonly Color Chartreuse2_1 = FromSpectreColor(SpectreColor.Chartreuse2_1);

    /// <summary>
    /// Gets the color "DarkOliveGreen3_1" (RGB 135,215,95).
    /// </summary>
    public static readonly Color DarkOliveGreen3_1 = FromSpectreColor(SpectreColor.DarkOliveGreen3_1);

    /// <summary>
    /// Gets the color "PaleGreen3_1" (RGB 135,215,135).
    /// </summary>
    public static readonly Color PaleGreen3_1 = FromSpectreColor(SpectreColor.PaleGreen3_1);

    /// <summary>
    /// Gets the color "DarkSeaGreen3" (RGB 135,215,175).
    /// </summary>
    public static readonly Color DarkSeaGreen3 = FromSpectreColor(SpectreColor.DarkSeaGreen3);

    /// <summary>
    /// Gets the color "DarkSlateGray3" (RGB 135,215,215).
    /// </summary>
    public static readonly Color DarkSlateGray3 = FromSpectreColor(SpectreColor.DarkSlateGray3);

    /// <summary>
    /// Gets the color "SkyBlue1" (RGB 135,215,255).
    /// </summary>
    public static readonly Color SkyBlue1 = FromSpectreColor(SpectreColor.SkyBlue1);

    /// <summary>
    /// Gets the color "Chartreuse1" (RGB 135,255,0).
    /// </summary>
    public static readonly Color Chartreuse1 = FromSpectreColor(SpectreColor.Chartreuse1);

    /// <summary>
    /// Gets the color "LightGreen" (RGB 135,255,95).
    /// </summary>
    public static readonly Color LightGreen = FromSpectreColor(SpectreColor.LightGreen);

    /// <summary>
    /// Gets the color "LightGreen_1" (RGB 135,255,135).
    /// </summary>
    public static readonly Color LightGreen_1 = FromSpectreColor(SpectreColor.LightGreen_1);

    /// <summary>
    /// Gets the color "PaleGreen1" (RGB 135,255,175).
    /// </summary>
    public static readonly Color PaleGreen1 = FromSpectreColor(SpectreColor.PaleGreen1);

    /// <summary>
    /// Gets the color "Aquamarine1_1" (RGB 135,255,215).
    /// </summary>
    public static readonly Color Aquamarine1_1 = FromSpectreColor(SpectreColor.Aquamarine1_1);

    /// <summary>
    /// Gets the color "DarkSlateGray1" (RGB 135,255,255).
    /// </summary>
    public static readonly Color DarkSlateGray1 = FromSpectreColor(SpectreColor.DarkSlateGray1);

    /// <summary>
    /// Gets the color "Red3" (RGB 175,0,0).
    /// </summary>
    public static readonly Color Red3 = FromSpectreColor(SpectreColor.Red3);

    /// <summary>
    /// Gets the color "DeepPink4_2" (RGB 175,0,95).
    /// </summary>
    public static readonly Color DeepPink4_2 = FromSpectreColor(SpectreColor.DeepPink4_2);

    /// <summary>
    /// Gets the color "MediumVioletRed" (RGB 175,0,135).
    /// </summary>
    public static readonly Color MediumVioletRed = FromSpectreColor(SpectreColor.MediumVioletRed);

    /// <summary>
    /// Gets the color "Magenta3" (RGB 175,0,175).
    /// </summary>
    public static readonly Color Magenta3 = FromSpectreColor(SpectreColor.Magenta3);

    /// <summary>
    /// Gets the color "DarkViolet_1" (RGB 175,0,215).
    /// </summary>
    public static readonly Color DarkViolet_1 = FromSpectreColor(SpectreColor.DarkViolet_1);

    /// <summary>
    /// Gets the color "Purple_2" (RGB 175,0,255).
    /// </summary>
    public static readonly Color Purple_2 = FromSpectreColor(SpectreColor.Purple_2);

    /// <summary>
    /// Gets the color "DarkOrange3" (RGB 175,95,0).
    /// </summary>
    public static readonly Color DarkOrange3 = FromSpectreColor(SpectreColor.DarkOrange3);

    /// <summary>
    /// Gets the color "IndianRed" (RGB 175,95,95).
    /// </summary>
    public static readonly Color IndianRed = FromSpectreColor(SpectreColor.IndianRed);

    /// <summary>
    /// Gets the color "HotPink3" (RGB 175,95,135).
    /// </summary>
    public static readonly Color HotPink3 = FromSpectreColor(SpectreColor.HotPink3);

    /// <summary>
    /// Gets the color "MediumOrchid3" (RGB 175,95,175).
    /// </summary>
    public static readonly Color MediumOrchid3 = FromSpectreColor(SpectreColor.MediumOrchid3);

    /// <summary>
    /// Gets the color "MediumOrchid" (RGB 175,95,215).
    /// </summary>
    public static readonly Color MediumOrchid = FromSpectreColor(SpectreColor.MediumOrchid);

    /// <summary>
    /// Gets the color "MediumPurple2" (RGB 175,95,255).
    /// </summary>
    public static readonly Color MediumPurple2 = FromSpectreColor(SpectreColor.MediumPurple2);

    /// <summary>
    /// Gets the color "DarkGoldenrod" (RGB 175,135,0).
    /// </summary>
    public static readonly Color DarkGoldenrod = FromSpectreColor(SpectreColor.DarkGoldenrod);

    /// <summary>
    /// Gets the color "LightSalmon3" (RGB 175,135,95).
    /// </summary>
    public static readonly Color LightSalmon3 = FromSpectreColor(SpectreColor.LightSalmon3);

    /// <summary>
    /// Gets the color "RosyBrown" (RGB 175,135,135).
    /// </summary>
    public static readonly Color RosyBrown = FromSpectreColor(SpectreColor.RosyBrown);

    /// <summary>
    /// Gets the color "Grey63" (RGB 175,135,175).
    /// </summary>
    public static readonly Color Grey63 = FromSpectreColor(SpectreColor.Grey63);

    /// <summary>
    /// Gets the color "MediumPurple2_1" (RGB 175,135,215).
    /// </summary>
    public static readonly Color MediumPurple2_1 = FromSpectreColor(SpectreColor.MediumPurple2_1);

    /// <summary>
    /// Gets the color "MediumPurple1" (RGB 175,135,255).
    /// </summary>
    public static readonly Color MediumPurple1 = FromSpectreColor(SpectreColor.MediumPurple1);

    /// <summary>
    /// Gets the color "Gold3" (RGB 175,175,0).
    /// </summary>
    public static readonly Color Gold3 = FromSpectreColor(SpectreColor.Gold3);

    /// <summary>
    /// Gets the color "DarkKhaki" (RGB 175,175,95).
    /// </summary>
    public static readonly Color DarkKhaki = FromSpectreColor(SpectreColor.DarkKhaki);

    /// <summary>
    /// Gets the color "NavajoWhite3" (RGB 175,175,135).
    /// </summary>
    public static readonly Color NavajoWhite3 = FromSpectreColor(SpectreColor.NavajoWhite3);

    /// <summary>
    /// Gets the color "Grey69" (RGB 175,175,175).
    /// </summary>
    public static readonly Color Grey69 = FromSpectreColor(SpectreColor.Grey69);

    /// <summary>
    /// Gets the color "LightSteelBlue3" (RGB 175,175,215).
    /// </summary>
    public static readonly Color LightSteelBlue3 = FromSpectreColor(SpectreColor.LightSteelBlue3);

    /// <summary>
    /// Gets the color "LightSteelBlue" (RGB 175,175,255).
    /// </summary>
    public static readonly Color LightSteelBlue = FromSpectreColor(SpectreColor.LightSteelBlue);

    /// <summary>
    /// Gets the color "Yellow3" (RGB 175,215,0).
    /// </summary>
    public static readonly Color Yellow3 = FromSpectreColor(SpectreColor.Yellow3);

    /// <summary>
    /// Gets the color "DarkOliveGreen3_2" (RGB 175,215,95).
    /// </summary>
    public static readonly Color DarkOliveGreen3_2 = FromSpectreColor(SpectreColor.DarkOliveGreen3_2);

    /// <summary>
    /// Gets the color "DarkSeaGreen3_1" (RGB 175,215,135).
    /// </summary>
    public static readonly Color DarkSeaGreen3_1 = FromSpectreColor(SpectreColor.DarkSeaGreen3_1);

    /// <summary>
    /// Gets the color "DarkSeaGreen2" (RGB 175,215,175).
    /// </summary>
    public static readonly Color DarkSeaGreen2 = FromSpectreColor(SpectreColor.DarkSeaGreen2);

    /// <summary>
    /// Gets the color "LightCyan3" (RGB 175,215,215).
    /// </summary>
    public static readonly Color LightCyan3 = FromSpectreColor(SpectreColor.LightCyan3);

    /// <summary>
    /// Gets the color "LightSkyBlue1" (RGB 175,215,255).
    /// </summary>
    public static readonly Color LightSkyBlue1 = FromSpectreColor(SpectreColor.LightSkyBlue1);

    /// <summary>
    /// Gets the color "GreenYellow" (RGB 175,255,0).
    /// </summary>
    public static readonly Color GreenYellow = FromSpectreColor(SpectreColor.GreenYellow);

    /// <summary>
    /// Gets the color "DarkOliveGreen2" (RGB 175,255,95).
    /// </summary>
    public static readonly Color DarkOliveGreen2 = FromSpectreColor(SpectreColor.DarkOliveGreen2);

    /// <summary>
    /// Gets the color "PaleGreen1_1" (RGB 175,255,135).
    /// </summary>
    public static readonly Color PaleGreen1_1 = FromSpectreColor(SpectreColor.PaleGreen1_1);

    /// <summary>
    /// Gets the color "DarkSeaGreen2_1" (RGB 175,255,175).
    /// </summary>
    public static readonly Color DarkSeaGreen2_1 = FromSpectreColor(SpectreColor.DarkSeaGreen2_1);

    /// <summary>
    /// Gets the color "DarkSeaGreen1" (RGB 175,255,215).
    /// </summary>
    public static readonly Color DarkSeaGreen1 = FromSpectreColor(SpectreColor.DarkSeaGreen1);

    /// <summary>
    /// Gets the color "PaleTurquoise1" (RGB 175,255,255).
    /// </summary>
    public static readonly Color PaleTurquoise1 = FromSpectreColor(SpectreColor.PaleTurquoise1);

    /// <summary>
    /// Gets the color "Red3_1" (RGB 215,0,0).
    /// </summary>
    public static readonly Color Red3_1 = FromSpectreColor(SpectreColor.Red3_1);

    /// <summary>
    /// Gets the color "DeepPink3" (RGB 215,0,95).
    /// </summary>
    public static readonly Color DeepPink3 = FromSpectreColor(SpectreColor.DeepPink3);

    /// <summary>
    /// Gets the color "DeepPink3_1" (RGB 215,0,135).
    /// </summary>
    public static readonly Color DeepPink3_1 = FromSpectreColor(SpectreColor.DeepPink3_1);

    /// <summary>
    /// Gets the color "Magenta3_1" (RGB 215,0,175).
    /// </summary>
    public static readonly Color Magenta3_1 = FromSpectreColor(SpectreColor.Magenta3_1);

    /// <summary>
    /// Gets the color "Magenta3_2" (RGB 215,0,215).
    /// </summary>
    public static readonly Color Magenta3_2 = FromSpectreColor(SpectreColor.Magenta3_2);

    /// <summary>
    /// Gets the color "Magenta2" (RGB 215,0,255).
    /// </summary>
    public static readonly Color Magenta2 = FromSpectreColor(SpectreColor.Magenta2);

    /// <summary>
    /// Gets the color "DarkOrange3_1" (RGB 215,95,0).
    /// </summary>
    public static readonly Color DarkOrange3_1 = FromSpectreColor(SpectreColor.DarkOrange3_1);

    /// <summary>
    /// Gets the color "IndianRed_1" (RGB 215,95,95).
    /// </summary>
    public static readonly Color IndianRed_1 = FromSpectreColor(SpectreColor.IndianRed_1);

    /// <summary>
    /// Gets the color "HotPink3_1" (RGB 215,95,135).
    /// </summary>
    public static readonly Color HotPink3_1 = FromSpectreColor(SpectreColor.HotPink3_1);

    /// <summary>
    /// Gets the color "HotPink2" (RGB 215,95,175).
    /// </summary>
    public static readonly Color HotPink2 = FromSpectreColor(SpectreColor.HotPink2);

    /// <summary>
    /// Gets the color "Orchid" (RGB 215,95,215).
    /// </summary>
    public static readonly Color Orchid = FromSpectreColor(SpectreColor.Orchid);

    /// <summary>
    /// Gets the color "MediumOrchid1" (RGB 215,95,255).
    /// </summary>
    public static readonly Color MediumOrchid1 = FromSpectreColor(SpectreColor.MediumOrchid1);

    /// <summary>
    /// Gets the color "Orange3" (RGB 215,135,0).
    /// </summary>
    public static readonly Color Orange3 = FromSpectreColor(SpectreColor.Orange3);

    /// <summary>
    /// Gets the color "LightSalmon3_1" (RGB 215,135,95).
    /// </summary>
    public static readonly Color LightSalmon3_1 = FromSpectreColor(SpectreColor.LightSalmon3_1);

    /// <summary>
    /// Gets the color "LightPink3" (RGB 215,135,135).
    /// </summary>
    public static readonly Color LightPink3 = FromSpectreColor(SpectreColor.LightPink3);

    /// <summary>
    /// Gets the color "Pink3" (RGB 215,135,175).
    /// </summary>
    public static readonly Color Pink3 = FromSpectreColor(SpectreColor.Pink3);

    /// <summary>
    /// Gets the color "Plum3" (RGB 215,135,215).
    /// </summary>
    public static readonly Color Plum3 = FromSpectreColor(SpectreColor.Plum3);

    /// <summary>
    /// Gets the color "Violet" (RGB 215,135,255).
    /// </summary>
    public static readonly Color Violet = FromSpectreColor(SpectreColor.Violet);

    /// <summary>
    /// Gets the color "Gold3_1" (RGB 215,175,0).
    /// </summary>
    public static readonly Color Gold3_1 = FromSpectreColor(SpectreColor.Gold3_1);

    /// <summary>
    /// Gets the color "LightGoldenrod3" (RGB 215,175,95).
    /// </summary>
    public static readonly Color LightGoldenrod3 = FromSpectreColor(SpectreColor.LightGoldenrod3);

    /// <summary>
    /// Gets the color "Tan" (RGB 215,175,135).
    /// </summary>
    public static readonly Color Tan = FromSpectreColor(SpectreColor.Tan);

    /// <summary>
    /// Gets the color "MistyRose3" (RGB 215,175,175).
    /// </summary>
    public static readonly Color MistyRose3 = FromSpectreColor(SpectreColor.MistyRose3);

    /// <summary>
    /// Gets the color "Thistle3" (RGB 215,175,215).
    /// </summary>
    public static readonly Color Thistle3 = FromSpectreColor(SpectreColor.Thistle3);

    /// <summary>
    /// Gets the color "Plum2" (RGB 215,175,255).
    /// </summary>
    public static readonly Color Plum2 = FromSpectreColor(SpectreColor.Plum2);

    /// <summary>
    /// Gets the color "Yellow3_1" (RGB 215,215,0).
    /// </summary>
    public static readonly Color Yellow3_1 = FromSpectreColor(SpectreColor.Yellow3_1);

    /// <summary>
    /// Gets the color "Khaki3" (RGB 215,215,95).
    /// </summary>
    public static readonly Color Khaki3 = FromSpectreColor(SpectreColor.Khaki3);

    /// <summary>
    /// Gets the color "LightGoldenrod2" (RGB 215,215,135).
    /// </summary>
    public static readonly Color LightGoldenrod2 = FromSpectreColor(SpectreColor.LightGoldenrod2);

    /// <summary>
    /// Gets the color "LightYellow3" (RGB 215,215,175).
    /// </summary>
    public static readonly Color LightYellow3 = FromSpectreColor(SpectreColor.LightYellow3);

    /// <summary>
    /// Gets the color "Grey84" (RGB 215,215,215).
    /// </summary>
    public static readonly Color Grey84 = FromSpectreColor(SpectreColor.Grey84);

    /// <summary>
    /// Gets the color "LightSteelBlue1" (RGB 215,215,255).
    /// </summary>
    public static readonly Color LightSteelBlue1 = FromSpectreColor(SpectreColor.LightSteelBlue1);

    /// <summary>
    /// Gets the color "Yellow2" (RGB 215,255,0).
    /// </summary>
    public static readonly Color Yellow2 = FromSpectreColor(SpectreColor.Yellow2);

    /// <summary>
    /// Gets the color "DarkOliveGreen1" (RGB 215,255,95).
    /// </summary>
    public static readonly Color DarkOliveGreen1 = FromSpectreColor(SpectreColor.DarkOliveGreen1);

    /// <summary>
    /// Gets the color "DarkOliveGreen1_1" (RGB 215,255,135).
    /// </summary>
    public static readonly Color DarkOliveGreen1_1 = FromSpectreColor(SpectreColor.DarkOliveGreen1_1);

    /// <summary>
    /// Gets the color "DarkSeaGreen1_1" (RGB 215,255,175).
    /// </summary>
    public static readonly Color DarkSeaGreen1_1 = FromSpectreColor(SpectreColor.DarkSeaGreen1_1);

    /// <summary>
    /// Gets the color "Honeydew2" (RGB 215,255,215).
    /// </summary>
    public static readonly Color Honeydew2 = FromSpectreColor(SpectreColor.Honeydew2);

    /// <summary>
    /// Gets the color "LightCyan1" (RGB 215,255,255).
    /// </summary>
    public static readonly Color LightCyan1 = FromSpectreColor(SpectreColor.LightCyan1);

    /// <summary>
    /// Gets the color "Red1" (RGB 255,0,0).
    /// </summary>
    public static readonly Color Red1 = FromSpectreColor(SpectreColor.Red1);

    /// <summary>
    /// Gets the color "DeepPink2" (RGB 255,0,95).
    /// </summary>
    public static readonly Color DeepPink2 = FromSpectreColor(SpectreColor.DeepPink2);

    /// <summary>
    /// Gets the color "DeepPink1" (RGB 255,0,135).
    /// </summary>
    public static readonly Color DeepPink1 = FromSpectreColor(SpectreColor.DeepPink1);

    /// <summary>
    /// Gets the color "DeepPink1_1" (RGB 255,0,175).
    /// </summary>
    public static readonly Color DeepPink1_1 = FromSpectreColor(SpectreColor.DeepPink1_1);

    /// <summary>
    /// Gets the color "Magenta2_1" (RGB 255,0,215).
    /// </summary>
    public static readonly Color Magenta2_1 = FromSpectreColor(SpectreColor.Magenta2_1);

    /// <summary>
    /// Gets the color "Magenta1" (RGB 255,0,255).
    /// </summary>
    public static readonly Color Magenta1 = FromSpectreColor(SpectreColor.Magenta1);

    /// <summary>
    /// Gets the color "OrangeRed1" (RGB 255,95,0).
    /// </summary>
    public static readonly Color OrangeRed1 = FromSpectreColor(SpectreColor.OrangeRed1);

    /// <summary>
    /// Gets the color "IndianRed1" (RGB 255,95,95).
    /// </summary>
    public static readonly Color IndianRed1 = FromSpectreColor(SpectreColor.IndianRed1);

    /// <summary>
    /// Gets the color "IndianRed1_1" (RGB 255,95,135).
    /// </summary>
    public static readonly Color IndianRed1_1 = FromSpectreColor(SpectreColor.IndianRed1_1);

    /// <summary>
    /// Gets the color "HotPink" (RGB 255,95,175).
    /// </summary>
    public static readonly Color HotPink = FromSpectreColor(SpectreColor.HotPink);

    /// <summary>
    /// Gets the color "HotPink_1" (RGB 255,95,215).
    /// </summary>
    public static readonly Color HotPink_1 = FromSpectreColor(SpectreColor.HotPink_1);

    /// <summary>
    /// Gets the color "MediumOrchid1_1" (RGB 255,95,255).
    /// </summary>
    public static readonly Color MediumOrchid1_1 = FromSpectreColor(SpectreColor.MediumOrchid1_1);

    /// <summary>
    /// Gets the color "DarkOrange" (RGB 255,135,0).
    /// </summary>
    public static readonly Color DarkOrange = FromSpectreColor(SpectreColor.DarkOrange);

    /// <summary>
    /// Gets the color "Salmon1" (RGB 255,135,95).
    /// </summary>
    public static readonly Color Salmon1 = FromSpectreColor(SpectreColor.Salmon1);

    /// <summary>
    /// Gets the color "LightCoral" (RGB 255,135,135).
    /// </summary>
    public static readonly Color LightCoral = FromSpectreColor(SpectreColor.LightCoral);

    /// <summary>
    /// Gets the color "PaleVioletRed1" (RGB 255,135,175).
    /// </summary>
    public static readonly Color PaleVioletRed1 = FromSpectreColor(SpectreColor.PaleVioletRed1);

    /// <summary>
    /// Gets the color "Orchid2" (RGB 255,135,215).
    /// </summary>
    public static readonly Color Orchid2 = FromSpectreColor(SpectreColor.Orchid2);

    /// <summary>
    /// Gets the color "Orchid1" (RGB 255,135,255).
    /// </summary>
    public static readonly Color Orchid1 = FromSpectreColor(SpectreColor.Orchid1);

    /// <summary>
    /// Gets the color "Orange1" (RGB 255,175,0).
    /// </summary>
    public static readonly Color Orange1 = FromSpectreColor(SpectreColor.Orange1);

    /// <summary>
    /// Gets the color "SandyBrown" (RGB 255,175,95).
    /// </summary>
    public static readonly Color SandyBrown = FromSpectreColor(SpectreColor.SandyBrown);

    /// <summary>
    /// Gets the color "LightSalmon1" (RGB 255,175,135).
    /// </summary>
    public static readonly Color LightSalmon1 = FromSpectreColor(SpectreColor.LightSalmon1);

    /// <summary>
    /// Gets the color "LightPink1" (RGB 255,175,175).
    /// </summary>
    public static readonly Color LightPink1 = FromSpectreColor(SpectreColor.LightPink1);

    /// <summary>
    /// Gets the color "Pink1" (RGB 255,175,215).
    /// </summary>
    public static readonly Color Pink1 = FromSpectreColor(SpectreColor.Pink1);

    /// <summary>
    /// Gets the color "Plum1" (RGB 255,175,255).
    /// </summary>
    public static readonly Color Plum1 = FromSpectreColor(SpectreColor.Plum1);

    /// <summary>
    /// Gets the color "Gold1" (RGB 255,215,0).
    /// </summary>
    public static readonly Color Gold1 = FromSpectreColor(SpectreColor.Gold1);

    /// <summary>
    /// Gets the color "LightGoldenrod2_1" (RGB 255,215,95).
    /// </summary>
    public static readonly Color LightGoldenrod2_1 = FromSpectreColor(SpectreColor.LightGoldenrod2_1);

    /// <summary>
    /// Gets the color "LightGoldenrod2_2" (RGB 255,215,135).
    /// </summary>
    public static readonly Color LightGoldenrod2_2 = FromSpectreColor(SpectreColor.LightGoldenrod2_2);

    /// <summary>
    /// Gets the color "NavajoWhite1" (RGB 255,215,175).
    /// </summary>
    public static readonly Color NavajoWhite1 = FromSpectreColor(SpectreColor.NavajoWhite1);

    /// <summary>
    /// Gets the color "MistyRose1" (RGB 255,215,215).
    /// </summary>
    public static readonly Color MistyRose1 = FromSpectreColor(SpectreColor.MistyRose1);

    /// <summary>
    /// Gets the color "Thistle1" (RGB 255,215,255).
    /// </summary>
    public static readonly Color Thistle1 = FromSpectreColor(SpectreColor.Thistle1);

    /// <summary>
    /// Gets the color "Yellow1" (RGB 255,255,0).
    /// </summary>
    public static readonly Color Yellow1 = FromSpectreColor(SpectreColor.Yellow1);

    /// <summary>
    /// Gets the color "LightGoldenrod1" (RGB 255,255,95).
    /// </summary>
    public static readonly Color LightGoldenrod1 = FromSpectreColor(SpectreColor.LightGoldenrod1);

    /// <summary>
    /// Gets the color "Khaki1" (RGB 255,255,135).
    /// </summary>
    public static readonly Color Khaki1 = FromSpectreColor(SpectreColor.Khaki1);

    /// <summary>
    /// Gets the color "Wheat1" (RGB 255,255,175).
    /// </summary>
    public static readonly Color Wheat1 = FromSpectreColor(SpectreColor.Wheat1);

    /// <summary>
    /// Gets the color "Cornsilk1" (RGB 255,255,215).
    /// </summary>
    public static readonly Color Cornsilk1 = FromSpectreColor(SpectreColor.Cornsilk1);

    /// <summary>
    /// Gets the color "Grey100" (RGB 255,255,255).
    /// </summary>
    public static readonly Color Grey100 = FromSpectreColor(SpectreColor.Grey100);

    /// <summary>
    /// Gets the color "Grey3" (RGB 8,8,8).
    /// </summary>
    public static readonly Color Grey3 = FromSpectreColor(SpectreColor.Grey3);

    /// <summary>
    /// Gets the color "Grey7" (RGB 18,18,18).
    /// </summary>
    public static readonly Color Grey7 = FromSpectreColor(SpectreColor.Grey7);

    /// <summary>
    /// Gets the color "Grey11" (RGB 28,28,28).
    /// </summary>
    public static readonly Color Grey11 = FromSpectreColor(SpectreColor.Grey11);

    /// <summary>
    /// Gets the color "Grey15" (RGB 38,38,38).
    /// </summary>
    public static readonly Color Grey15 = FromSpectreColor(SpectreColor.Grey15);

    /// <summary>
    /// Gets the color "Grey19" (RGB 48,48,48).
    /// </summary>
    public static readonly Color Grey19 = FromSpectreColor(SpectreColor.Grey19);

    /// <summary>
    /// Gets the color "Grey23" (RGB 58,58,58).
    /// </summary>
    public static readonly Color Grey23 = FromSpectreColor(SpectreColor.Grey23);

    /// <summary>
    /// Gets the color "Grey27" (RGB 68,68,68).
    /// </summary>
    public static readonly Color Grey27 = FromSpectreColor(SpectreColor.Grey27);

    /// <summary>
    /// Gets the color "Grey30" (RGB 78,78,78).
    /// </summary>
    public static readonly Color Grey30 = FromSpectreColor(SpectreColor.Grey30);

    /// <summary>
    /// Gets the color "Grey35" (RGB 88,88,88).
    /// </summary>
    public static readonly Color Grey35 = FromSpectreColor(SpectreColor.Grey35);

    /// <summary>
    /// Gets the color "Grey39" (RGB 98,98,98).
    /// </summary>
    public static readonly Color Grey39 = FromSpectreColor(SpectreColor.Grey39);

    /// <summary>
    /// Gets the color "Grey42" (RGB 108,108,108).
    /// </summary>
    public static readonly Color Grey42 = FromSpectreColor(SpectreColor.Grey42);

    /// <summary>
    /// Gets the color "Grey46" (RGB 118,118,118).
    /// </summary>
    public static readonly Color Grey46 = FromSpectreColor(SpectreColor.Grey46);

    /// <summary>
    /// Gets the color "Grey50" (RGB 128,128,128).
    /// </summary>
    public static readonly Color Grey50 = FromSpectreColor(SpectreColor.Grey50);

    /// <summary>
    /// Gets the color "Grey54" (RGB 138,138,138).
    /// </summary>
    public static readonly Color Grey54 = FromSpectreColor(SpectreColor.Grey54);

    /// <summary>
    /// Gets the color "Grey58" (RGB 148,148,148).
    /// </summary>
    public static readonly Color Grey58 = FromSpectreColor(SpectreColor.Grey58);

    /// <summary>
    /// Gets the color "Grey62" (RGB 158,158,158).
    /// </summary>
    public static readonly Color Grey62 = FromSpectreColor(SpectreColor.Grey62);

    /// <summary>
    /// Gets the color "Grey66" (RGB 168,168,168).
    /// </summary>
    public static readonly Color Grey66 = FromSpectreColor(SpectreColor.Grey66);

    /// <summary>
    /// Gets the color "Grey70" (RGB 178,178,178).
    /// </summary>
    public static readonly Color Grey70 = FromSpectreColor(SpectreColor.Grey70);

    /// <summary>
    /// Gets the color "Grey74" (RGB 188,188,188).
    /// </summary>
    public static readonly Color Grey74 = FromSpectreColor(SpectreColor.Grey74);

    /// <summary>
    /// Gets the color "Grey78" (RGB 198,198,198).
    /// </summary>
    public static readonly Color Grey78 = FromSpectreColor(SpectreColor.Grey78);

    /// <summary>
    /// Gets the color "Grey82" (RGB 208,208,208).
    /// </summary>
    public static readonly Color Grey82 = FromSpectreColor(SpectreColor.Grey82);

    /// <summary>
    /// Gets the color "Grey85" (RGB 218,218,218).
    /// </summary>
    public static readonly Color Grey85 = FromSpectreColor(SpectreColor.Grey85);

    /// <summary>
    /// Gets the color "Grey89" (RGB 228,228,228).
    /// </summary>
    public static readonly Color Grey89 = FromSpectreColor(SpectreColor.Grey89);

    /// <summary>
    /// Gets the color "Grey93" (RGB 238,238,238).
    /// </summary>
    public static readonly Color Grey93 = FromSpectreColor(SpectreColor.Grey93);

    #endregion
}
