namespace Jumbee.Console;

using System;

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
        }
    }

    public Style Style
    {
        get => _style;
        set
        {
            _style = value;
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
        }
    }
    #endregion

    #region Methods
    protected sealed override void Render()
    {
        if (Size.Width <= 0 || Size.Height <= 0) return;

        ansiConsole.Clear(true);

        var frame = _spinner.Frames[frameIndex % _spinner.Frames.Count];
        var frameMarkup = $"[{_style.ToMarkup()}]{Markup.Escape(frame)}[/]";
        ansiConsole.Markup(frameMarkup);

        if (!string.IsNullOrEmpty(_text))
        {
            ansiConsole.Write(" ");
            ansiConsole.Markup(_text);
        }   
    }
    #endregion

    #region Fields
    private Spectre.Console.Spinner _spinner = Spectre.Console.Spinner.Known.Default;
    private Style _style = Style.Plain;
    private string _text = string.Empty;
    #endregion
}
