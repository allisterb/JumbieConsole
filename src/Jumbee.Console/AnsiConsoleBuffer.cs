namespace Jumbee.Console;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using Spectre.Console;
using Spectre.Console.Interop;
using Spectre.Console.Rendering;

/// <summary>
/// An implementation of Spectre.Console.IAnsiConsole that writes to a ConsoleBuffer.
/// </summary>
public class AnsiConsoleBuffer : IAnsiConsole, IDisposable
{
    #region Constructors
    public AnsiConsoleBuffer(ConsoleBuffer console)
    {
        _console = console; 
        _cursor = new AnsiConsoleBufferCursor(this);
        _input = new AnsiConsoleBufferInput(console);
        _exclusivityMode = new AnsiConsoleBufferExclusivityMode();
        _pipeline = new RenderPipeline();               
        _profile = new Profile(new AnsiConsoleBufferOutput(console), Encoding.UTF8);        
        _profile.Capabilities.Ansi = AnsiConsole.Profile.Capabilities.Ansi;
        _profile.Capabilities.ColorSystem = AnsiConsole.Profile.Capabilities.ColorSystem;
        _profile.Capabilities.Interactive = AnsiConsole.Profile.Capabilities.Interactive;
        _profile.Capabilities.Unicode = AnsiConsole.Profile.Capabilities.Unicode;        
        _cursorX = 0;
        _cursorY = 0;
    }

    #endregion

    #region Properties
    public Profile Profile => _profile;
    public IAnsiConsoleCursor Cursor => _cursor;
    public IAnsiConsoleInput Input => _input;
    public IExclusivityMode ExclusivityMode => _exclusivityMode;
    public RenderPipeline Pipeline => _pipeline;
    
    internal int CursorX => _cursorX;
    internal int CursorY => _cursorY;
    #endregion

    #region Methods
    public void Clear(bool home)
    {
        _console.Initialize(); 
        if (home)
        {
            _cursorX = 0;
            _cursorY = 0;
        }
    }

    public void Write(IRenderable renderable)
    {
        var segments = renderable.GetSegments(this);
        foreach (var segment in segments)
        {
            if (segment.IsControlCode)
            {                
                foreach (var c in segment.Text)
                {
                    var position = new Position(_cursorX, _cursorY);
                    if (IsValidPosition(position))
                    {
                        _console.Write(position, new Character(c, isControl: true));
                    }
                    _cursorX++;
                }                
            }
            else
            {
                var style = segment.Style;
                var fg = style.Foreground.ToConsoleGUIColor();
                var bg = style.Background.ToConsoleGUIColor();
                var decoration = (ConsoleGUI.Data.Decoration)style.Decoration;
                foreach (char c in segment.Text)
                {
                    if (c == '\n')
                    {
                        _cursorY++;
                        _cursorX = 0;
                        continue;
                    }

                    if (c == '\r') continue;

                    var width = c.ToString().GetCellWidth();
                    if (width <= 0) continue; // Skip zero-width chars

                    var position = new Position(_cursorX, _cursorY);
                    if (IsValidPosition(position))
                    {
                        _console.Write(position, new Character(c, fg, bg, decoration));
                    }

                    _cursorX += width;
                }
            }
        }
    }

    public void Dispose()
    {
    }
    
    internal void SetCursorPosition(int x, int y)
    {
        _cursorX = x;
        _cursorY = y;
    }
    
    internal void MoveCursor(int dx, int dy)
    {
        _cursorX += dx;
        _cursorY += dy;
    }

    private bool IsValidPosition(Position position)
    {
        return position.X >= 0 && position.X < _console.Size.Width &&
               position.Y >= 0 && position.Y < _console.Size.Height;
    }
    #endregion

    #region Fields
    internal readonly ConsoleBuffer _console;
    internal readonly AnsiConsoleBufferCursor _cursor;
    private readonly AnsiConsoleBufferInput _input;
    private readonly AnsiConsoleBufferExclusivityMode _exclusivityMode;
    private readonly RenderPipeline _pipeline;
    private readonly Profile _profile;
    private int _cursorX;
    private int _cursorY;
    #endregion
}

internal class AnsiConsoleBufferOutput : IAnsiConsoleOutput
{
    private readonly IConsole _console;
    
    public AnsiConsoleBufferOutput(IConsole console) => _console = console;

    public TextWriter Writer => throw new NotSupportedException(); 
    public bool IsTerminal => true;
    public int Width => _console.Size.Width;
    public int Height => _console.Size.Height;

    public void SetEncoding(Encoding encoding) { }
}

internal class AnsiConsoleBufferCursor : IAnsiConsoleCursor
{
    private readonly AnsiConsoleBuffer _parent;

    public AnsiConsoleBufferCursor(AnsiConsoleBuffer parent) => _parent = parent;

    public void Show(bool show)
    {
        var x = _parent.CursorX;
        var y = _parent.CursorY;
        var cell = _parent._console[x, y];
        if (show)
        {
            if (cell.Content == null || cell.Content == '\0' || cell.Content == ' ')
            {
                _parent._console.Write(x, y, _cursorEmptyCell);

            }
            else
            {
                _parent._console.Write(x, y, cell.WithBackground(_cursorBackgroundColor));
            }
        }
        else
        {

            if (cell.Content == null || cell.Content == '\0' || cell.Content == ' ')
            {
                _parent._console.Write(x, y, __cursorEmptyCell);

            }
            else
            {
                _parent._console.Write(x, y, cell);
            }
        }
    }

    public void SetPosition(int column, int line)
    {
        _parent.SetCursorPosition(column, line);
    }

    public void Move(CursorDirection direction, int steps)
    {
        switch (direction)
        {
            case CursorDirection.Up:
                _parent.MoveCursor(0, -steps);
                break;
            case CursorDirection.Down:
                _parent.MoveCursor(0, steps);
                break;
            case CursorDirection.Left:
                _parent.MoveCursor(-steps, 0);
                break;
            case CursorDirection.Right:
                _parent.MoveCursor(steps, 0);
                break;
        }
    }

    private static readonly ConsoleGUI.Data.Color _cursorBackgroundColor = new ConsoleGUI.Data.Color(100, 100, 100);
    private static readonly Cell __cursorEmptyCell = new Cell(' ');
    private static readonly Cell _cursorEmptyCell = __cursorEmptyCell.WithBackground(_cursorBackgroundColor);
}

internal class AnsiConsoleBufferInput : IAnsiConsoleInput
{
    private readonly IConsole _console;

    public AnsiConsoleBufferInput(IConsole console) => _console = console;

    public bool IsKeyAvailable() => throw new NotSupportedException();

    public ConsoleKeyInfo? ReadKey(bool intercept) => throw new NotSupportedException();
    
    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal class AnsiConsoleBufferExclusivityMode : IExclusivityMode
{
    public T Run<T>(Func<T> func) => throw new NotSupportedException();
    public Task<T> RunAsync<T>(Func<Task<T>> func) => throw new NotSupportedException();
}
