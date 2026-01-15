namespace Jumbee.Console;

using System;

using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;


/// <summary>
/// A ConsoleGUI.IConsole implementation that writes to a buffer.
/// </summary>
public class ConsoleBuffer : IConsole
{
    #region Properties
    public Size Size 
    {
        get => field;
        set
        {
            Resize(value);
            field = value;  
        }
    }
    public bool KeyAvailable => false;
    #endregion

    #region Indexers
    public Cell this[Position position] => buffer[position.Y][position.X];
    
    public Cell this[int x, int y] => buffer[y][x];
    #endregion

    #region Methods
    /// <summary>
    /// Fill buffer with empty/transparent cells.
    /// </summary>
    public void Initialize()
    {    
        for (int y = 0; y < Size.Height; y++)
        {
            Array.Fill(buffer[y], emptyCell);
        }        
    }

    public void OnRefresh() { }

    /// <summary>
    /// Sets the console buffer cell character.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="character"></param>
    public void Write(Position position, in Character character)
    {
        if (position.X >= 0 && position.X < Size.Width && position.Y >= 0 && position.Y < Size.Height)
        {
            buffer[position.Y][position.X] = new Cell(character);
        }
    }

    /// <summary>
    /// Sets the console buffer cell character.
    /// </summary>
    public void Write(in int X, in int Y, in Cell cell)
    {
        if (X >= 0 && X < Size.Width && Y >= 0 && Y < Size.Height)
        {
            buffer[Y][X] = cell;
        }
    }
    /// <summary>
    /// Will be handled by IInputListeners.
    /// </summary>
    /// <returns></returns>
    public ConsoleKeyInfo ReadKey() => default;
    
    public Position GetPosition(int distance)
    {
        if (Size.Width == 0)
        {
            return new Position(0, 0);
        }
        int x = distance % Size.Width;
        int y = distance / Size.Width;
        return new Position(x, y);
    }

    public Position AddX(Position pos1, int x)
    {
        if (Size.Width == 0)
        {
            return new Position(0, 0);
        }

        int linear_pos1 = pos1.Y * Size.Width + pos1.X;
        int total_linear_distance = linear_pos1 + x;

        return GetPosition(total_linear_distance);
    }

    /// <summary>
    /// Resizing the control dimensions resizes the console buffer.
    /// </summary>
    /// <param name="size"></param>
    protected void Resize(Size size)
    {
        buffer = new Cell[size.Height][] ;
        for (int i = 0; i < size.Height; i++)
        {
            buffer[i] = new Cell[size.Width + 1];
        }
        for (int y = 0; y < size.Height; y++)
        {
            Array.Fill(buffer[y], emptyCell);
        }
    }
    #endregion

    #region Fields
    private static readonly Cell emptyCell = new Cell(Character.Empty);
    private Cell[][] buffer = [];
    #endregion
}
