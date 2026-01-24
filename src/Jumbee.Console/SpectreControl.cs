namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using Spectre.Console.Rendering;

/// <summary>
/// Wraps an existing Spectre.Console <see cref="IRenderable"/> control for use with ConsoleGUI control and layout types. 
/// </summary>
/// <remarks>
/// Uses an <see cref="AnsiConsoleBuffer"/> to render the control to a buffer.
/// Public property setters and methods that change a control's visual state should call <see cref="Invalidate"/> to request a re-render on the next UI update tick.
/// Non thread-safe changes should use CloneContent to create a copy of the IRenderable content and modify that before replacing the control's content with the modified copy.
/// </remarks>
/// <typeparam name="T"></typeparam>
public class SpectreControl<T> : RenderableControl where T : IRenderable
{
    #region Constructors
    public SpectreControl(T content) : base()
    {
        _content = content;
    }
    #endregion
    
    #region Properties
    public T Content
    {
        get => _content;
        set 
        {
            _content = value;
            Invalidate();
        }
    }
    #endregion

    #region Methods           
    /// <summary>
    /// Creates a copy of the current instance's control content.
    /// </summary>
    /// <remarks>This method is intended to be overridden in a derived class to clone the content of the current instance including all properties that can be modified by the user. 
    /// By default, it throws a <see /// cref="NotImplementedException"/>.
    /// </remarks>
    /// <returns>A new instance of type <typeparamref name="T"/> that is a copy of the current instance's content.</returns>
    /// <exception cref="NotImplementedException">Thrown if the method is not overridden in a derived class.</exception>
    protected virtual T CloneContent() => throw new NotImplementedException($"Cloning not implemented for type {typeof(T).Name}. Override CloneContent() in derived class.");

    /// <summary>
    /// Provides a thread-safe but much more expensive way of updating the control content.
    /// </summary>
    /// <param name="update">The update operation.</param>
    protected void UpdateContent(Action<T> update)
    {
        var buffer = CloneContent();
        update(buffer);
        Content = buffer;
    }
    
    protected override Measurement Measure(RenderOptions options, int maxWidth) => _content.Measure(options, maxWidth);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => _content.Render(options, maxWidth);  
    
    #endregion

    #region Fields
    private T _content;
    #endregion
}
