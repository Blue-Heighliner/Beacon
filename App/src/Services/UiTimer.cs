namespace BlueHeighliner.Beacon.Services;

/// <summary>A restartable one-shot timer that ticks on the UI thread.</summary>
internal interface IUiTimer
{
    /// <summary>Starts the timer, or restarts it if already running.</summary>
    void Restart();
}

/// <summary>Creates UI timers.</summary>
internal interface IUiTimerFactory
{
    /// <summary>Creates a stopped timer.</summary>
    /// <param name="interval">How long after a restart the timer ticks.</param>
    /// <param name="onTick">Invoked once per elapsed interval, on the UI thread.</param>
    /// <returns>The timer.</returns>
    IUiTimer Create(TimeSpan interval, Action onTick);
}

internal sealed class UiTimerFactory : IUiTimerFactory
{
    public IUiTimer Create(TimeSpan interval, Action onTick) => new UiTimer(interval, onTick);

    private sealed class UiTimer : IUiTimer
    {
        private readonly DispatcherTimer timer;

        public UiTimer(TimeSpan interval, Action onTick)
        {
            timer = new DispatcherTimer { Interval = interval };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                onTick();
            };
        }

        public void Restart()
        {
            timer.Stop();
            timer.Start();
        }
    }
}
