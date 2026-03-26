namespace Application.Processing;

/// <summary>
/// Очередь длительных задач с ограничением параллелизма через <see cref="SemaphoreSlim"/>.
/// </summary>
public sealed class LongTaskProcessor : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrentTasks;
    private readonly bool _ownsSemaphore;
    private readonly List<Task> _runningTasks = new();
    private readonly object _lock = new();

    public LongTaskProcessor(int maxConcurrentTasks)
    {
        _maxConcurrentTasks = maxConcurrentTasks;
        _semaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
        _ownsSemaphore = true;
    }

    /// <summary>Внешний семафор (например, из <c>WeekRationGenerationConcurrencyGate</c>). Не освобождается при <see cref="Dispose"/>.</summary>
    public LongTaskProcessor(SemaphoreSlim semaphore, int maxConcurrentTasksForBusyCount)
    {
        _semaphore = semaphore;
        _maxConcurrentTasks = maxConcurrentTasksForBusyCount;
        _ownsSemaphore = false;
    }

    public int GetBusyThreads() => _maxConcurrentTasks - _semaphore.CurrentCount;

    public void Enqueue(Func<Task> taskFactory, CancellationToken cancellationToken = default)
    {
        var task = Task.Run(
            async () =>
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await taskFactory().ConfigureAwait(false);
                }
                finally
                {
                    _semaphore.Release();
                }
            },
            cancellationToken);

        lock (_lock)
        {
            _runningTasks.RemoveAll(t => t.IsCompleted);
            _runningTasks.Add(task);
        }
    }

    public async Task WaitAllAsync(CancellationToken cancellationToken = default)
    {
        Task[] tasksCopy;
        lock (_lock)
        {
            tasksCopy = _runningTasks.ToArray();
        }

        if (tasksCopy.Length == 0)
            return;

        await Task.WhenAll(tasksCopy).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_ownsSemaphore)
            _semaphore.Dispose();
    }
}
