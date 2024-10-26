using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class TaskPool
{
    private readonly BlockingCollection<Action> _taskQueue;
    private readonly List<Task> _workers;
    private bool stop = false;

    public TaskPool(int numberOfWorkers)
    {
        _taskQueue = new BlockingCollection<Action>();
        _workers = new List<Task>(numberOfWorkers);
        stop = false;

        for (int i = 0; i < numberOfWorkers; i++)
        {
            var worker = new Task(Work);
            worker.Start();
            _workers.Add(worker);
        }
    }

    private void Work()
    {
        while (true)
        {
            if (stop) break;

            if(_taskQueue.Count == 0)
            {
                Thread.Sleep(150);
                continue;
            }

            if(_taskQueue.TryTake(out Action task))
                task.Invoke(); // Execute the task
        }
    }

    public void EnqueueTask(Action task)
    {
        _taskQueue.Add(task);
    }

    public void Stop()
    {
        stop = true;
        _taskQueue.CompleteAdding();
        _taskQueue.Dispose();
    }
}
