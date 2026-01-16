namespace Jumbee.Console;

using System;
using System.Linq;

using Spectre.Console;

public class Spinner : AnimatedControl
{
    #region Properties
    public Spectre.Console.Spinner SpinnerType
    {
        get => _spinner;
        set
        {
            _spinner = value;
            frameCount = _spinner.Frames.Count;
            interval = _spinner.Interval.Ticks;
            spinnerFrames = _spinner.Frames.Select(Style.EscapeMarkup).ToArray();
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(_text) ? "" : " " + _text));
        }
    }

    public Style Style
    {
        get => _style;
        set
        {
            _style = value;
            styleMarkup = _style;
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(_text) ? "" : " " + _text));
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(_text) ? "" : " " + _text));
        }
    }
    #endregion

    #region Methods
    protected sealed override void Render()
    {
        ansiConsole.Clear(true);        
        ansiConsole.Markup(spinnerFramesMarkup[frameIndex % spinnerFrames.Length]);        
    }
    #endregion

    #region Fields
    private Spectre.Console.Spinner _spinner = Spectre.Console.Spinner.Known.Default;
    private Style _style = Style.Plain;
    private string styleMarkup = Style.Plain;
    private string[] spinnerFrames = Spectre.Console.Spinner.Known.Default.Frames.Select(Markup.Escape).ToArray();
    private string[] spinnerFramesMarkup = Spectre.Console.Spinner.Known.Default.Frames.Select(f => $"[{Style.Plain}]{f}[/]").ToArray();
    private string _text = string.Empty;
    #endregion
}
