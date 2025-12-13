namespace Jumbee.Console;

using System;
using System.Threading;

using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using Spectre.Console.Rendering;

using ConsoleGuiSize = ConsoleGUI.Space.Size;
using ConsoleGUIColor = ConsoleGUI.Data.Color;
using SpectreConsoleColor = Spectre.Console.Color;

public class SpectreControl<T> : Control, IDisposable where T : IRenderable
{
    #region Constructors
    public SpectreControl(T control)
    {
        _control = control;
        _bufferConsole = new BufferConsole();
        _ansiConsole = new AnsiConsoleBuffer(_bufferConsole);
        UIUpdate.Tick += OnTick;
    }
    #endregion

    #region Properties
    public IRenderable Content 
    {
        get => _control;
        set 
        {
            _control = value;
            Interlocked.Increment(ref _updatesRequested);
        }
    }
    #endregion

    #region Indexers
    public override Cell this[Position position]
    {
        get
        {
            lock (UIUpdate.Lock)
            {
                if (_bufferConsole.Buffer == null || position.X < 0 || position.X >= Size.Width || position.Y < 0 || position.Y >= Size.Height)
                {
                    return _emptyCell;
                }
                else
                {
                    return _bufferConsole.Buffer[position.X, position.Y];
                }
            }
        }
    }
    #endregion

    #region Methods
    public void Dispose()
    {
        UIUpdate.Tick -= OnTick;
    }

    private void OnTick(object? sender, UIUpdateTimerEventArgs e)
    {
        lock (e.Lock)
        {
            if (_updatesRequested > 0)
            {
                Render();
                Interlocked.Exchange(ref _updatesRequested, 0u); // Explicitly use 0u for uint
            }
        }
    }

    protected override void Initialize()
    {
        lock (UIUpdate.Lock)
        {
            // Resize the control to fill the available space
            // We clip it to avoid issues if MaxSize is 'infinite' (though unlikely in this specific layout)
            var targetSize = MaxSize;
            if (targetSize.Width > 1000) targetSize = new ConsoleGuiSize(1000, targetSize.Height); 
            if (targetSize.Height > 1000) targetSize = new ConsoleGuiSize(targetSize.Width, 1000);
            
            Resize(targetSize);

            // Resize buffer
            _bufferConsole.Resize(Size);
            
            Render();
        }
    }

    private void Render()
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }
        
        // Render Spectre content to buffer
        _ansiConsole.Clear(true);
        // We probably want to render with the full width of the control
        // Spectre will look at the Profile.Width which comes from the IConsole.Size (BufferConsole.Size)
        _ansiConsole.Write(_control);
        Redraw();
    }
    #endregion

    #region Fields
    private readonly BufferConsole _bufferConsole;
    private readonly AnsiConsoleBuffer _ansiConsole;
    private IRenderable _control;
    private uint _updatesRequested;
    private static readonly Cell _emptyCell = new Cell(Character.Empty);
    #endregion
}
