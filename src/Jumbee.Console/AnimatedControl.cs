namespace Jumbee.Console;

using System;

public abstract class AnimatedControl : Control
{
    #region Constructors
    public AnimatedControl() : base() 
    {
        Invalidate();
    }
    #endregion
   
    #region Methods
    public void Start()
    {
        if (isRunning) return;
        isRunning = true;
        lastUpdate = DateTime.Now.Ticks;
        accumulated = 0L;
    }

    public void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
    }

    public override void Dispose()
    {
        Stop();
        base.Dispose();        
    }
        
    protected override void Paint()
    {        
        if (!isRunning) return;
        var now = DateTime.Now.Ticks;
        var delta = now - lastUpdate;
        lastUpdate = now;
        accumulated += delta;
        if (accumulated >= interval)
        {
            accumulated = 0L;
            frameIndex = (frameIndex + 1) % frameCount;
            Render();
        }        
    }

    // Control should always repaint itself
    protected override void Validate() {}
    #endregion

    #region Fields
    protected int frameCount = 0;
    protected int frameIndex = 0;    
    protected long lastUpdate;
    protected long accumulated;
    protected long interval;
    protected bool isRunning = false;
    #endregion
}
