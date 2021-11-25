using BookFinder.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class TaskMonitorManager
    {
        TaskManager _taskManager = new TaskManager((token) => RunAction(token));
        static DateTime _startTime;

        public void Start(int threadNum)
        {
            _taskManager.IncreaseThread(threadNum);
            _startTime = DateTime.Now;
        }

        public void Stop()
        {
            _taskManager.CancelAll();
        }

        private static async void RunAction(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000);

                var now = DateTime.Now;
                var systemUpTime = now - _startTime;

                if (systemUpTime.Seconds % 30 == 0)
                {
                    ConsoleLogger.Debug(" DataContainers.GetInstance().Serialize");
                    DataContainers.GetInstance().Serialize();
                    Console.WriteLine(" DataContainers.GetInstance().Serialize done");
                }

                if (systemUpTime.Seconds == 0)
                {
                    Console.WriteLine(" strarting allurllist archiving thread...");
                    await Task.Factory.StartNew(() =>
                    {
                        // 同时，每60秒archive一下url，每次补充
                        Console.WriteLine(" AllUrlList.Archive");
                        DataContainers.GetInstance().AllUrlList.Archive();
                        Console.WriteLine(" AllUrlList.Archive done");
                    });
                }
            }

        }

    }

}
