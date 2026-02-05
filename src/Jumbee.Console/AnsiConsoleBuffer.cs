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
public class AnsiConsoleBuffer : IAnsiConsole, IExclusivityMode, IDisposable
{
    #region Constructors
    public AnsiConsoleBuffer(ConsoleBuffer console)
    {
        _console = console;
        _cursor = new AnsiConsoleBufferCursor(this);
        _input = new AnsiConsoleBufferInput(_console);
        _exclusivityMode = this;
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
        Invoke(() =>
        {
            bool wasVisible = _cursor.IsVisible;
            _cursor.Forget();
            _console.Initialize();
            if (home)
            {
                _cursorX = 0;
                _cursorY = 0;
            }

            if (wasVisible)
            {
                _cursor.Show(true);
            }
        });
    }

    public void Write(IRenderable renderable)
    {
        Invoke(() =>
        {
            bool wasVisible = _cursor.Hide();

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

                        var width = c.GetCellWidth();
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

            if (wasVisible)
            {
                _cursor.Show(true);
            }
        });
    }

    public void Dispose()
    {
    }

    public int CursorDistance
    {
        get => _cursorY * _console.Size.Width + _cursorX;
        set => SetCursorPosition(_console.GetPosition(value));
    }


    public T Run<T>(Func<T> func)
    {
        // Try acquiring the exclusivity semaphore
        if (!_semaphore.Wait(0))
        {
            throw new Exception();
        }

        try
        {
            return func();
        }
        finally
        {
            _semaphore.Release(1);
        }
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        // Try acquiring the exclusivity semaphore
        if (!await _semaphore.WaitAsync(0).ConfigureAwait(false))
        {
            throw new Exception();
        }

        try
        {
            return await func().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release(1);
        }
    }

    internal void SetCursorPosition(int x, int y)
    {
        Invoke(() =>
        {
            _cursorX = x;
            _cursorY = y;
        });
    }

    internal void SetCursorPosition(Position position)
    {
        Invoke(() =>
        {
            _cursorX = position.X;
            _cursorY = position.Y;
        });
    }

    internal void MoveCursor(int dx, int dy)
    {
        Invoke(() =>
        {
            _cursorX += dx;
            _cursorY += dy;
        });
    }

    private bool IsValidPosition(Position position)
    {
        return position.X >= 0 && position.X < _console.Size.Width &&
               position.Y >= 0 && position.Y < _console.Size.Height;
    }

    internal void Invoke(Action action)
    {
        if (_lock.IsHeldByCurrentThread)
        {
            action();
        }
        else
        {
            lock (_lock)
            {
                action();
            }
        }
    }

    internal T Invoke<T>(Func<T> func)
    {
        if (_lock.IsHeldByCurrentThread)
        {
            return func();
        }
        else
        {
            lock (_lock)
            {
                return func();
            }
        }
    }
    #endregion

    #region Fields
    internal readonly ConsoleBuffer _console;
    internal readonly AnsiConsoleBufferCursor _cursor;
    private readonly AnsiConsoleBufferInput _input;
    private readonly IExclusivityMode _exclusivityMode;
    private readonly RenderPipeline _pipeline;
    private readonly Profile _profile;
    private int _cursorX;
    private int _cursorY;
    private readonly Lock _lock = new Lock();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
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
    #region Constructors
    public AnsiConsoleBufferCursor(AnsiConsoleBuffer parent) => _parent = parent;
    #endregion

    #region Properties
    internal bool IsVisible => _isVisible;
    #endregion

    #region Methods
    public void Show(bool show)
    {
        _parent.Invoke(() =>
        {
            if (show)
            {
                if (_isVisible)
                {
                    if (_savedPosition.HasValue && _savedPosition.Value.X == _parent.CursorX && _savedPosition.Value.Y == _parent.CursorY)
                    {
                        return;
                    }
                    else
                    {
                        HideCursor();
                    }
                }
                ShowCursor();
            }
            else
            {
                if (_isVisible)
                {
                    HideCursor();
                }
            }
        });
    }

    internal bool Hide()
    {
        return _parent.Invoke(() =>
        {
            if (_isVisible)
            {
                HideCursor();
                return true;
            }
            return false;
        });
    }

    internal void Forget()
    {
        _isVisible = false;
        _savedPosition = null;
        _savedCell = default;
    }

    private void ShowCursor()
    {
        var x = _parent.CursorX;
        var y = _parent.CursorY;

        if (x < 0 || y < 0 || x >= _parent._console.Size.Width || y >= _parent._console.Size.Height)
            return;
        var currentCell = _parent._console[x, y];
        _savedCell = currentCell;
        _savedPosition = new Position(x, y);
        if (currentCell.Content == null || currentCell.Content == '\0' || currentCell.Content == ' ')
        {
            _parent._console.Write(x, y, cursorEmptyCell);

        }
        else
        {
            _parent._console.Write(x, y, currentCell.WithBackground(_cursorBackgroundColor));
        }
        _isVisible = true;
    }

    private void HideCursor()
    {
        if (_savedPosition.HasValue)
        {
            var pos = _savedPosition.Value;
            if (pos.X >= 0 && pos.Y >= 0 && pos.X < _parent._console.Size.Width && pos.Y < _parent._console.Size.Height)
            {
                if (_parent._console[pos].Equals(cursorEmptyCell))
                {
                    _parent._console.Write(pos, emptyCell);
                }
                else
                {
                    _parent._console.Write(pos, _savedCell);
                }
            }
        }
        _isVisible = false;
        _savedPosition = null;
    }

    public void SetPosition(int column, int line)
    {
        _parent.Invoke(() =>
        {
            var wasVisible = Hide();
            _parent.SetCursorPosition(column, line);
            if (wasVisible) Show(true);
        });
    }

    public void Move(CursorDirection direction, int steps)
    {
        _parent.Invoke(() =>
        {
            var wasVisible = Hide();
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
            if (wasVisible) Show(true);
        });
    }
    #endregion

    #region Fields
    private readonly AnsiConsoleBuffer _parent;
    private Position? _savedPosition;
    private Cell _savedCell;
    private bool _isVisible;

    private static readonly ConsoleGUI.Data.Color _cursorBackgroundColor = new ConsoleGUI.Data.Color(100, 100, 100);
    private static readonly Cell emptyCell = new Cell(' ');
    private static readonly Cell cursorEmptyCell = emptyCell.WithBackground(_cursorBackgroundColor);
    #endregion
}

internal class AnsiConsoleBufferInput : IAnsiConsoleInput
{
    private readonly ConsoleBuffer _console;

    public AnsiConsoleBufferInput(ConsoleBuffer console) => _console = console;

    public bool IsKeyAvailable() => throw new NotSupportedException();

    public ConsoleKeyInfo? ReadKey(bool intercept) => throw new NotSupportedException();

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) => throw new NotSupportedException();
}
