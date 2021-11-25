using BookFinder.EntityFrameworkCore;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    class Program
    {
        private static DateTime _startTime;
        static void Main(string[] args)
        {
            //StartBookSpider();
            StartStockSpider();
        }

        private static void StartStockSpider()
        {
            // 初始化一堆url先，放到data里

            // 启动stock spider进程跑
        }

        private static void StartBookSpider()
        {
            _startTime = DateTime.Now;

            Console.WriteLine(" Hello world");
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }
            DataContainers.GetInstance().Deserialize();
            Console.WriteLine(" Loaded.");


            //DataContainers.GetInstance().CategoryUrlList.AddOne("https://www.qidian.com");
            // 做几个定时器，不同的定时器做不同的事情
            Console.WriteLine(" Starting Spider");
            var pageSpider = new PageSpiderManager();
            pageSpider.Start(36);
            Console.WriteLine(" Starting html syncing tool...");
            var htmlSyncing = new SyncHtmlToDatabaseManager();
            htmlSyncing.Start(5);
            var taskMonitorManager = new TaskMonitorManager();
            taskMonitorManager.Start(1);

            System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += (ctx) =>
            {
                pageSpider.Stop();
                htmlSyncing.Stop();

                Thread.Sleep(5000);

                DataContainers.GetInstance().Serialize();

                Thread.Sleep(2000);
            };

            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine(" Thread.Sleep 1000 done");
                // 每秒打印一次
                PrintConsole(pageSpider.AvaliableThreadNum());

                var now = DateTime.Now;
                var systemUpTime = now - _startTime;

                if (systemUpTime.Seconds == 30)
                {
                    // 每60秒调整一下相关线程数量
                    Console.WriteLine(" AutoAdjusting threads");
                    pageSpider.AutoAdjust(DataContainers.GetInstance().PageHtmlAckList.Count(), 5000, 1000);
                }
            }
        }

        private static void PrintConsole(int pageSpiderThreadNum)
        {
            var categoryUrlListCount1 = DataContainers.GetInstance().CategoryUrlList.Count(false);
            var categoryUrlListCount2 = DataContainers.GetInstance().CategoryUrlList.Count(true);
            var bookUrlListCount = DataContainers.GetInstance().BookUrlList.Count();
            var temporaryCategoryUrlListCount = DataContainers.GetInstance().TemporaryCategoryUrlList.Count();
            var temporaryBookUrlListCount = DataContainers.GetInstance().TemporaryBookUrlList.Count();
            var pageHtmlAckListCount = DataContainers.GetInstance().PageHtmlAckList.Count();
            var allUrlListCount1 = DataContainers.GetInstance().AllUrlList.Count(false);
            var allUrlListCount2 = DataContainers.GetInstance().AllUrlList.Count(true);

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" 起点阅读小说抓取器正在运行中...\t\t");
            var now = DateTime.Now;
            var systemUpTime = now - _startTime;
            Console.WriteLine(" 系统启动时间: {0}", _startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine("  当 前 时 间: {0}", now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine(@" 本次运行时间: {0:hh\:mm\:ss}", systemUpTime);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("==========================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Format("  Category Url Numbers: {0,15:N0} / {1,15:N0}", categoryUrlListCount1, categoryUrlListCount2));
            Console.WriteLine(string.Format("     Novel Url Numbers: {0,15:N0}", bookUrlListCount));
            Console.WriteLine(string.Format("       All Url Numbers: {0,15:N0} / {1,15:N0}", allUrlListCount1, allUrlListCount2));
            Console.WriteLine(string.Format("  Captured Url Numbers: {0,15:N0}", temporaryCategoryUrlListCount));
            Console.WriteLine(string.Format(" Captured Book Numbers: {0,15:N0}", temporaryBookUrlListCount));
            Console.WriteLine(string.Format("   Html catch  Numbers: {0,15:N0}", pageHtmlAckListCount));
            Console.WriteLine("==========================================================");
            Console.WriteLine(string.Format("  Page Spider 线程数: {0,3:N0}", pageSpiderThreadNum));
        }

    }
}
