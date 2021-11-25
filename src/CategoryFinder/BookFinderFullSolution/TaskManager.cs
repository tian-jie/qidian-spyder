using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class TaskManager
    {
        private Action<CancellationToken> _action;
        private Stack<TaskObject> _tasks = new Stack<TaskObject>();

        public TaskManager(Action<CancellationToken> action)
        {
            _action = action;
        }

        public void IncreaseThread(int num)
        {
            for (var i = 0; i < num; i++)
            {
                var cancellationToken = new CancellationTokenSource();
                var task = Task.Factory.StartNew(() =>
                {
                    _action(cancellationToken.Token);
                }, cancellationToken.Token, creationOptions: TaskCreationOptions.LongRunning, scheduler: TaskScheduler.Default);
                _tasks.Push(new TaskObject()
                {
                    Task = task,
                    CancellationToken = cancellationToken
                });

            }
        }


        public void DecreaseThread(int num)
        {
            for (var i = 0; i < num; i++)
            {
                var task = _tasks.Pop();
                task.CancellationToken.Cancel();
            }
        }

        public int AutoAdjust(int num, int high, int low)
        {
            if (num >= high)
            {
                // 减少25%线程数量
                var threadNum = _tasks.Count;
                var toDecreaseThreadNum = threadNum / 4;
                DecreaseThread(toDecreaseThreadNum);
                return toDecreaseThreadNum;
            }
            if (num < low)
            {
                // 增加25%线程数量
                var threadNum = _tasks.Count;
                var toIncreaseThreadNum = threadNum / 4;
                IncreaseThread(toIncreaseThreadNum);
                return toIncreaseThreadNum;
            }

            return 0;
        }

        public void CancelAll()
        {
            foreach (var task in _tasks)
            {
                task.CancellationToken.Cancel();
            }
        }

        public int ThreadCount()
        {
            return _tasks.Count;
        }
    }

}
