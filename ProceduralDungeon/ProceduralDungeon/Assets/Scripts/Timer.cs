using System;

public class Timer
{
    private double currentTime;
    private float maxTime;
    
    private bool running;
    
    private readonly Action onEnd;
    // public Action<double> onPlaying;

    public Timer(float maxTime, Action onEnd = null!)
    {
        this.maxTime = maxTime;
        this.onEnd = onEnd;
        this.running = true;
    }

    public void Reset()
    {
        this.currentTime = 0;
        this.running = true;
    }

    public void Update(double dt)
    {
        if (!this.running)
            return;
        this.currentTime += dt;
        // this.onPlaying?.Invoke(this.currentTime);
        CheckIfEndIsReached();
    }

    private void CheckIfEndIsReached()
    {
        if (!(this.currentTime >= this.maxTime)) return;
        this.onEnd?.Invoke();
        this.running = false;
    }

    public void Add(float time) => this.maxTime += time;
}