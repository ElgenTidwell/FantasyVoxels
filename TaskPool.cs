using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class TaskPool
{
    private readonly BlockingCollection<Action> _taskQueue;
    private readonly List<Task> _workers;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public TaskPool(int numberOfWorkers)
    {
        _taskQueue = new BlockingCollection<Action>();
        _workers = new List<Task>(numberOfWorkers);
        _cancellationTokenSource = new CancellationTokenSource();

        for (int i = 0; i < numberOfWorkers; i++)
        {
            _workers.Add(Task.Factory.StartNew(Work, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default));
        }
    }

    private void Work()
    {
        foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
        {
            task.Invoke();
        }
    }

    public void EnqueueTask(Action task)
    {
        if (!_taskQueue.IsAddingCompleted)
        {
            _taskQueue.Add(task);
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _taskQueue.CompleteAdding();
        _taskQueue.Dispose();
    }
}