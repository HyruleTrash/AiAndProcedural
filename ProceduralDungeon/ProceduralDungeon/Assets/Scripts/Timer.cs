using System;

public class Timer
{
    private double _currentTime = 0;
    private float _maxTime = 0;
    public Action onEnd;
    public Action<double> onPlaying;
    public bool running;

    public Timer(float maxTime, Action onEnd)
    {
        this._maxTime = maxTime;
        this.onEnd = onEnd;
        this.running = true;
    }
        
    public Timer(float maxTime)
    {
        this._maxTime = maxTime;
        this.running = true;
    }
        
    public void Reset()
    {
        this._currentTime = 0;
        this.running = true;
    }

    public void Update(double dt)
    {
        if (!this.running)
            return;
        this._currentTime += dt;
        this.onPlaying?.Invoke(this._currentTime);
        CheckIfEndIsReached();
    }

    public void CheckIfEndIsReached()
    {
        if (this._currentTime >= this._maxTime)
        {
            this.onEnd?.Invoke();
            this.running = false;
        }
    }

    public void Add(float time) => this._maxTime += time;
}