namespace Jumbee.Console;

using SCStyle = Spectre.Console.Style; 
using SCDecoration = Spectre.Console.Decoration;    
using SystemDrawingColor = System.Drawing.Color;    

public readonly struct Style
{    
    #region Constructors
    public Style(SCStyle spectreConsoleStyle)
    {
        SpectreConsoleStyle = spectreConsoleStyle;
    }       

    public Style(string style) : this(SCStyle.Parse(style)) {}
    #endregion

    #region Properties
    public SCStyle SpectreConsoleStyle { get; }
    #endregion

    #region Indexers
    public string this[string text] => $"[{this.ToMarkup()}]{EscapeMarkup(text)}[/]";
    #endregion

    #region Methods
    public static string EscapeMarkup(string text) => Spectre.Console.Markup.Escape(text);

    public string ToMarkup() => SpectreConsoleStyle.ToMarkup();
    #endregion

    #region Operators
    public static implicit operator SCStyle(Style style) => style.SpectreConsoleStyle;

    public static implicit operator Style(SCStyle spectreConsoleStyle) => new Style(spectreConsoleStyle);   
    
    public static implicit operator string(Style style) => style.ToMarkup();

    public static implicit operator Style(Color color) => new Style(new SCStyle(color) );

    public static implicit operator Color(Style style) => style.SpectreConsoleStyle.Foreground;

    public static Style operator | (Style a, Style b) => new Style(a.SpectreConsoleStyle.Combine(b.SpectreConsoleStyle));

    public static implicit operator SystemDrawingColor(Style style)
    {
        var scColor = style.SpectreConsoleStyle.Foreground;
        return SystemDrawingColor.FromArgb(scColor.R, scColor.G, scColor.B);
    }
    #endregion

    #region Fields

    #region Text decorations
    public readonly static Style Plain = SCStyle.Plain;

    public readonly static Style Bold = new SCStyle(decoration: SCDecoration.Bold);

    public readonly static Style Dim = new SCStyle(decoration: SCDecoration.Dim);

    public readonly static Style Italic = new SCStyle(decoration: SCDecoration.Italic);

    public readonly static Style Underline = new SCStyle(decoration: SCDecoration.Underline);

    public readonly static Style Invert = new SCStyle(decoration: SCDecoration.Invert);

    public readonly static Style Conceal = new SCStyle(decoration: SCDecoration.Conceal);

    public readonly static Style SlowBlink = new SCStyle(decoration: SCDecoration.SlowBlink);

    public readonly static Style RapidBlink = new SCStyle(decoration: SCDecoration.RapidBlink);

    public readonly static Style Strikethrough = new SCStyle(decoration: SCDecoration.Strikethrough);
    #endregion

    #region Colors
    public readonly static Style Black = Color.Black;
    public readonly static Style Maroon = Color.Maroon;
    public readonly static Style Green = Color.Green;
    public readonly static Style Olive = Color.Olive;
    public readonly static Style Navy = Color.Navy;
    public readonly static Style Purple = Color.Purple;
    public readonly static Style Teal = Color.Teal;
    public readonly static Style Silver = Color.Silver;
    public readonly static Style Grey = Color.Grey;
    public readonly static Style Red = Color.Red;
    public readonly static Style Lime = Color.Lime;
    public readonly static Style Yellow = Color.Yellow;
    public readonly static Style Blue = Color.Blue;
    public readonly static Style Fuchsia = Color.Fuchsia;
    public readonly static Style Aqua = Color.Aqua;
    public readonly static Style White = Color.White;
    public readonly static Style Grey0 = Color.Grey0;
    public readonly static Style NavyBlue = Color.NavyBlue;
    public readonly static Style DarkBlue = Color.DarkBlue;
    public readonly static Style Blue3 = Color.Blue3;
    public readonly static Style Blue3_1 = Color.Blue3_1;
    public readonly static Style Blue1 = Color.Blue1;
    public readonly static Style DarkGreen = Color.DarkGreen;
    public readonly static Style DeepSkyBlue4 = Color.DeepSkyBlue4;
    public readonly static Style DeepSkyBlue4_1 = Color.DeepSkyBlue4_1;
    public readonly static Style DeepSkyBlue4_2 = Color.DeepSkyBlue4_2;
    public readonly static Style DodgerBlue3 = Color.DodgerBlue3;
    public readonly static Style DodgerBlue2 = Color.DodgerBlue2;
    public readonly static Style Green4 = Color.Green4;
    public readonly static Style SpringGreen4 = Color.SpringGreen4;
    public readonly static Style Turquoise4 = Color.Turquoise4;
    public readonly static Style DeepSkyBlue3 = Color.DeepSkyBlue3;
    public readonly static Style DeepSkyBlue3_1 = Color.DeepSkyBlue3_1;
    public readonly static Style DodgerBlue1 = Color.DodgerBlue1;
    public readonly static Style Green3 = Color.Green3;
    public readonly static Style SpringGreen3 = Color.SpringGreen3;
    public readonly static Style DarkCyan = Color.DarkCyan;
    public readonly static Style LightSeaGreen = Color.LightSeaGreen;
    public readonly static Style DeepSkyBlue2 = Color.DeepSkyBlue2;
    public readonly static Style DeepSkyBlue1 = Color.DeepSkyBlue1;
    public readonly static Style Green3_1 = Color.Green3_1;
    public readonly static Style SpringGreen3_1 = Color.SpringGreen3_1;
    public readonly static Style SpringGreen2 = Color.SpringGreen2;
    public readonly static Style Cyan3 = Color.Cyan3;
    public readonly static Style DarkTurquoise = Color.DarkTurquoise;
    public readonly static Style Turquoise2 = Color.Turquoise2;
    public readonly static Style Green1 = Color.Green1;
    public readonly static Style SpringGreen2_1 = Color.SpringGreen2_1;
    public readonly static Style SpringGreen1 = Color.SpringGreen1;
    public readonly static Style MediumSpringGreen = Color.MediumSpringGreen;
    public readonly static Style Cyan2 = Color.Cyan2;
    public readonly static Style Cyan1 = Color.Cyan1;
    public readonly static Style DarkRed = Color.DarkRed;
    public readonly static Style DeepPink4 = Color.DeepPink4;
    public readonly static Style Purple4 = Color.Purple4;
    public readonly static Style Purple4_1 = Color.Purple4_1;
    public readonly static Style Purple3 = Color.Purple3;
    public readonly static Style BlueViolet = Color.BlueViolet;
    public readonly static Style Orange4 = Color.Orange4;
    public readonly static Style Grey37 = Color.Grey37;
    public readonly static Style MediumPurple4 = Color.MediumPurple4;
    public readonly static Style SlateBlue3 = Color.SlateBlue3;
    public readonly static Style SlateBlue3_1 = Color.SlateBlue3_1;
    public readonly static Style RoyalBlue1 = Color.RoyalBlue1;
    public readonly static Style Chartreuse4 = Color.Chartreuse4;
    public readonly static Style DarkSeaGreen4 = Color.DarkSeaGreen4;
    public readonly static Style PaleTurquoise4 = Color.PaleTurquoise4;
    public readonly static Style SteelBlue = Color.SteelBlue;
    public readonly static Style SteelBlue3 = Color.SteelBlue3;
    public readonly static Style CornflowerBlue = Color.CornflowerBlue;
    public readonly static Style Chartreuse3 = Color.Chartreuse3;
    public readonly static Style DarkSeaGreen4_1 = Color.DarkSeaGreen4_1;
    public readonly static Style CadetBlue = Color.CadetBlue;
    public readonly static Style CadetBlue_1 = Color.CadetBlue_1;
    public readonly static Style SkyBlue3 = Color.SkyBlue3;
    public readonly static Style SteelBlue1 = Color.SteelBlue1;
    public readonly static Style Chartreuse3_1 = Color.Chartreuse3_1;
    public readonly static Style PaleGreen3 = Color.PaleGreen3;
    public readonly static Style SeaGreen3 = Color.SeaGreen3;
    public readonly static Style Aquamarine3 = Color.Aquamarine3;
    public readonly static Style MediumTurquoise = Color.MediumTurquoise;
    public readonly static Style SteelBlue1_1 = Color.SteelBlue1_1;
    public readonly static Style Chartreuse2 = Color.Chartreuse2;
    public readonly static Style SeaGreen2 = Color.SeaGreen2;
    public readonly static Style SeaGreen1 = Color.SeaGreen1;
    public readonly static Style SeaGreen1_1 = Color.SeaGreen1_1;
    public readonly static Style Aquamarine1 = Color.Aquamarine1;
    public readonly static Style DarkSlateGray2 = Color.DarkSlateGray2;
    public readonly static Style DarkRed_1 = Color.DarkRed_1;
    public readonly static Style DeepPink4_1 = Color.DeepPink4_1;
    public readonly static Style DarkMagenta = Color.DarkMagenta;
    public readonly static Style DarkMagenta_1 = Color.DarkMagenta_1;
    public readonly static Style DarkViolet = Color.DarkViolet;
    public readonly static Style Purple_1 = Color.Purple_1;
    public readonly static Style Orange4_1 = Color.Orange4_1;
    public readonly static Style LightPink4 = Color.LightPink4;
    public readonly static Style Plum4 = Color.Plum4;
    public readonly static Style MediumPurple3 = Color.MediumPurple3;
    public readonly static Style MediumPurple3_1 = Color.MediumPurple3_1;
    public readonly static Style SlateBlue1 = Color.SlateBlue1;
    public readonly static Style Yellow4 = Color.Yellow4;
    public readonly static Style Wheat4 = Color.Wheat4;
    public readonly static Style Grey53 = Color.Grey53;
    public readonly static Style LightSlateGrey = Color.LightSlateGrey;
    public readonly static Style MediumPurple = Color.MediumPurple;
    public readonly static Style LightSlateBlue = Color.LightSlateBlue;
    public readonly static Style Yellow4_1 = Color.Yellow4_1;
    public readonly static Style DarkOliveGreen3 = Color.DarkOliveGreen3;
    public readonly static Style DarkSeaGreen = Color.DarkSeaGreen;
    public readonly static Style LightSkyBlue3 = Color.LightSkyBlue3;
    public readonly static Style LightSkyBlue3_1 = Color.LightSkyBlue3_1;
    public readonly static Style SkyBlue2 = Color.SkyBlue2;
    public readonly static Style Chartreuse2_1 = Color.Chartreuse2_1;
    public readonly static Style DarkOliveGreen3_1 = Color.DarkOliveGreen3_1;
    public readonly static Style PaleGreen3_1 = Color.PaleGreen3_1;
    public readonly static Style DarkSeaGreen3 = Color.DarkSeaGreen3;
    public readonly static Style DarkSlateGray3 = Color.DarkSlateGray3;
    public readonly static Style SkyBlue1 = Color.SkyBlue1;
    public readonly static Style Chartreuse1 = Color.Chartreuse1;
    public readonly static Style LightGreen = Color.LightGreen;
    public readonly static Style LightGreen_1 = Color.LightGreen_1;
    public readonly static Style PaleGreen1 = Color.PaleGreen1;
    public readonly static Style Aquamarine1_1 = Color.Aquamarine1_1;
    public readonly static Style DarkSlateGray1 = Color.DarkSlateGray1;
    public readonly static Style Red3 = Color.Red3;
    public readonly static Style DeepPink4_2 = Color.DeepPink4_2;
    public readonly static Style MediumVioletRed = Color.MediumVioletRed;
    public readonly static Style Magenta3 = Color.Magenta3;
    public readonly static Style DarkViolet_1 = Color.DarkViolet_1;
    public readonly static Style Purple_2 = Color.Purple_2;
    public readonly static Style DarkOrange3 = Color.DarkOrange3;
    public readonly static Style IndianRed = Color.IndianRed;
    public readonly static Style HotPink3 = Color.HotPink3;
    public readonly static Style MediumOrchid3 = Color.MediumOrchid3;
    public readonly static Style MediumOrchid = Color.MediumOrchid;
    public readonly static Style MediumPurple2 = Color.MediumPurple2;
    public readonly static Style DarkGoldenrod = Color.DarkGoldenrod;
    public readonly static Style LightSalmon3 = Color.LightSalmon3;
    public readonly static Style RosyBrown = Color.RosyBrown;
    public readonly static Style Grey63 = Color.Grey63;
    public readonly static Style MediumPurple2_1 = Color.MediumPurple2_1;
    public readonly static Style MediumPurple1 = Color.MediumPurple1;
    public readonly static Style Gold3 = Color.Gold3;
    public readonly static Style DarkKhaki = Color.DarkKhaki;
    public readonly static Style NavajoWhite3 = Color.NavajoWhite3;
    public readonly static Style Grey69 = Color.Grey69;
    public readonly static Style LightSteelBlue3 = Color.LightSteelBlue3;
    public readonly static Style LightSteelBlue = Color.LightSteelBlue;
    public readonly static Style Yellow3 = Color.Yellow3;
    public readonly static Style DarkOliveGreen3_2 = Color.DarkOliveGreen3_2;
    public readonly static Style DarkSeaGreen3_1 = Color.DarkSeaGreen3_1;
    public readonly static Style DarkSeaGreen2 = Color.DarkSeaGreen2;
    public readonly static Style LightCyan3 = Color.LightCyan3;
    public readonly static Style LightSkyBlue1 = Color.LightSkyBlue1;
    public readonly static Style GreenYellow = Color.GreenYellow;
    public readonly static Style DarkOliveGreen2 = Color.DarkOliveGreen2;
    public readonly static Style PaleGreen1_1 = Color.PaleGreen1_1;
    public readonly static Style DarkSeaGreen2_1 = Color.DarkSeaGreen2_1;
    public readonly static Style DarkSeaGreen1 = Color.DarkSeaGreen1;
    public readonly static Style PaleTurquoise1 = Color.PaleTurquoise1;
    public readonly static Style Red3_1 = Color.Red3_1;
    public readonly static Style DeepPink3 = Color.DeepPink3;
    public readonly static Style DeepPink3_1 = Color.DeepPink3_1;
    public readonly static Style Magenta3_1 = Color.Magenta3_1;
    public readonly static Style Magenta3_2 = Color.Magenta3_2;
    public readonly static Style Magenta2 = Color.Magenta2;
    public readonly static Style DarkOrange3_1 = Color.DarkOrange3_1;
    public readonly static Style IndianRed_1 = Color.IndianRed_1;
    public readonly static Style HotPink3_1 = Color.HotPink3_1;
    public readonly static Style HotPink2 = Color.HotPink2;
    public readonly static Style Orchid = Color.Orchid;
    public readonly static Style MediumOrchid1 = Color.MediumOrchid1;
    public readonly static Style Orange3 = Color.Orange3;
    public readonly static Style LightSalmon3_1 = Color.LightSalmon3_1;
    public readonly static Style LightPink3 = Color.LightPink3;
    public readonly static Style Pink3 = Color.Pink3;
    public readonly static Style Plum3 = Color.Plum3;
    public readonly static Style Violet = Color.Violet;
    public readonly static Style Gold3_1 = Color.Gold3_1;
    public readonly static Style LightGoldenrod3 = Color.LightGoldenrod3;
    public readonly static Style Tan = Color.Tan;
    public readonly static Style MistyRose3 = Color.MistyRose3;
    public readonly static Style Thistle3 = Color.Thistle3;
    public readonly static Style Plum2 = Color.Plum2;
    public readonly static Style Yellow3_1 = Color.Yellow3_1;
    public readonly static Style Khaki3 = Color.Khaki3;
    public readonly static Style LightGoldenrod2 = Color.LightGoldenrod2;
    public readonly static Style LightYellow3 = Color.LightYellow3;
    public readonly static Style Grey84 = Color.Grey84;
    public readonly static Style LightSteelBlue1 = Color.LightSteelBlue1;
    public readonly static Style Yellow2 = Color.Yellow2;
    public readonly static Style DarkOliveGreen1 = Color.DarkOliveGreen1;
    public readonly static Style DarkOliveGreen1_1 = Color.DarkOliveGreen1_1;
    public readonly static Style DarkSeaGreen1_1 = Color.DarkSeaGreen1_1;
    public readonly static Style Honeydew2 = Color.Honeydew2;
    public readonly static Style LightCyan1 = Color.LightCyan1;
    public readonly static Style Red1 = Color.Red1;
    public readonly static Style DeepPink2 = Color.DeepPink2;
    public readonly static Style DeepPink1 = Color.DeepPink1;
    public readonly static Style DeepPink1_1 = Color.DeepPink1_1;
    public readonly static Style Magenta2_1 = Color.Magenta2_1;
    public readonly static Style Magenta1 = Color.Magenta1;
    public readonly static Style OrangeRed1 = Color.OrangeRed1;
    public readonly static Style IndianRed1 = Color.IndianRed1;
    public readonly static Style IndianRed1_1 = Color.IndianRed1_1;
    public readonly static Style HotPink = Color.HotPink;
    public readonly static Style HotPink_1 = Color.HotPink_1;
    public readonly static Style MediumOrchid1_1 = Color.MediumOrchid1_1;
    public readonly static Style DarkOrange = Color.DarkOrange;
    public readonly static Style Salmon1 = Color.Salmon1;
    public readonly static Style LightCoral = Color.LightCoral;
    public readonly static Style PaleVioletRed1 = Color.PaleVioletRed1;
    public readonly static Style Orchid2 = Color.Orchid2;
    public readonly static Style Orchid1 = Color.Orchid1;
    public readonly static Style Orange1 = Color.Orange1;
    public readonly static Style SandyBrown = Color.SandyBrown;
    public readonly static Style LightSalmon1 = Color.LightSalmon1;
    public readonly static Style LightPink1 = Color.LightPink1;
    public readonly static Style Pink1 = Color.Pink1;
    public readonly static Style Plum1 = Color.Plum1;
    public readonly static Style Gold1 = Color.Gold1;
    public readonly static Style LightGoldenrod2_1 = Color.LightGoldenrod2_1;
    public readonly static Style LightGoldenrod2_2 = Color.LightGoldenrod2_2;
    public readonly static Style NavajoWhite1 = Color.NavajoWhite1;
    public readonly static Style MistyRose1 = Color.MistyRose1;
    public readonly static Style Thistle1 = Color.Thistle1;
    public readonly static Style Yellow1 = Color.Yellow1;
    public readonly static Style LightGoldenrod1 = Color.LightGoldenrod1;
    public readonly static Style Khaki1 = Color.Khaki1;
    public readonly static Style Wheat1 = Color.Wheat1;
    public readonly static Style Cornsilk1 = Color.Cornsilk1;
    public readonly static Style Grey100 = Color.Grey100;
    public readonly static Style Grey3 = Color.Grey3;
    public readonly static Style Grey7 = Color.Grey7;
    public readonly static Style Grey11 = Color.Grey11;
    public readonly static Style Grey15 = Color.Grey15;
    public readonly static Style Grey19 = Color.Grey19;
    public readonly static Style Grey23 = Color.Grey23;
    public readonly static Style Grey27 = Color.Grey27;
    public readonly static Style Grey30 = Color.Grey30;
    public readonly static Style Grey35 = Color.Grey35;
    public readonly static Style Grey39 = Color.Grey39;
    public readonly static Style Grey42 = Color.Grey42;
    public readonly static Style Grey46 = Color.Grey46;
    public readonly static Style Grey50 = Color.Grey50;
    public readonly static Style Grey54 = Color.Grey54;
    public readonly static Style Grey58 = Color.Grey58;
    public readonly static Style Grey62 = Color.Grey62;
    public readonly static Style Grey66 = Color.Grey66;
    public readonly static Style Grey70 = Color.Grey70;
    public readonly static Style Grey74 = Color.Grey74;
    public readonly static Style Grey78 = Color.Grey78;
    public readonly static Style Grey82 = Color.Grey82;
    public readonly static Style Grey85 = Color.Grey85;
    public readonly static Style Grey89 = Color.Grey89;
    public readonly static Style Grey93 = Color.Grey93;
    #endregion

    #endregion
}
