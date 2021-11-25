using BookFinder.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class SyncHtmlToDatabaseManager
    {
        TaskManager _taskManager = new TaskManager((token) => RunAction(token));

        public void Start(int threadNum)
        {
            _taskManager.IncreaseThread(threadNum);
        }

        public void Stop()
        {
            _taskManager.CancelAll();
        }

        private static async void RunAction(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (DataContainers.GetInstance().PageHtmlAckList.Count() < 500)
                {
                    Thread.Sleep(5000);
                    continue;
                }
                using (var context = new BookFinderDbContext())
                {
                    try
                    {
                        // 先同步到数据库，减少文件大小
                        context.PageHtmls.AddRange(DataContainers.GetInstance().PageHtmlAckList.GetList(1000));
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                Thread.Sleep(1000);
            }

        }

    }

}
