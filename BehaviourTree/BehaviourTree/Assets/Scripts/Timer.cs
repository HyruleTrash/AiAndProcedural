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
        running = true;
    }
        
    public Timer(float maxTime)
    {
        this._maxTime = maxTime;
        running = true;
    }
        
    public void Reset()
    {
        _currentTime = 0;
        running = true;
    }

    public void Update(double dt)
    {
        if (!running)
            return;
        _currentTime += dt;
        onPlaying?.Invoke(_currentTime);
        CheckIfEndIsReached();
    }

    public void CheckIfEndIsReached()
    {
        if (_currentTime >= _maxTime)
        {
            onEnd?.Invoke();
            running = false;
        }
    }

    public void Add(float time) => _maxTime += time;
}