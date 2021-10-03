using BookFinder.Tools;
using Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CategoryFinder
{
    class Program
    {
        static bool isProgramTerminated = false;
        static ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = BookFinder.Tools.Common.GetSettings("RabbitMQ:host"),
            UserName = BookFinder.Tools.Common.GetSettings("RabbitMQ:user"),
            Password = BookFinder.Tools.Common.GetSettings("RabbitMQ:password"),
            Port = int.Parse(BookFinder.Tools.Common.GetSettings("RabbitMQ:port"))
        };

        static string[] timeSlotsNames = { "", "", "", "", "", "", "", "", "", "" };
        static long[] timeSlots = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        //static int[] timeSlotsPercentage = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        static long[] ts = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        static long[] te = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


        static IConnection connection;

        const string Blanks = "                                                                                                     ";
        static int csvlines = 0;
        static StreamWriter performanceTracingSw;
        static long freq = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

#if DEBUG
            performanceTracingSw = new StreamWriter("f:\\performance-tracing.csv");
            QueryPerformanceMethd.QueryPerformanceFrequency(ref freq);
            freq /= 1000;

            performanceTracingSw.WriteLine("Id, Time, SystemInitTime, OnRabbitMQMessageReceived, ParseMQMessage, AllHttpRequest, HttpRequest, htmlToRabbitMQ, RegexHTML, GetMatchesAndAddNonDuplicateToRedis, t9, t10");
#endif

            QueryPerformanceMethd.QueryPerformanceCounter(ref ts[0]);
            AdjustThreads();

            QueryPerformanceMethd.QueryPerformanceCounter(ref te[0]);

            timeSlotsNames[0] = "System Initial Time";
            timeSlots[0] = te[0] - ts[0];
            //Timer timer = new Timer(delegate
            //    {
            //        AdjustThreads();
            //    },
            //    null,
            //    2000,
            //    1000
            //);
            var programTimeCnt = 0;
            while (!isProgramTerminated)
            {
                Thread.Sleep(1000);
                var restartDuration = int.Parse(Common.GetSettings("General:restartDuration"));

#if DEBUG
                var totalTime = timeSlots.Sum();
                // 刷新显示几个数据：
                Console.WriteLine("#######################################################");
                for (var i = 0; i < timeSlotsNames.Length; i++)
                {
                    if (string.IsNullOrEmpty(timeSlotsNames[i]))
                    {
                        break;
                    }
                    var s = string.Format("{0,30}: {1} ({2:P})", timeSlotsNames[i], timeSlots[i], timeSlots[i] * 1.0 / totalTime);
                    Console.WriteLine(s);
                }
#endif
                if (++programTimeCnt >= restartDuration)
                {
                    break;
                }
            }
#if DEBUG
            performanceTracingSw.Close();
            performanceTracingSw.Dispose();
#endif
        }

        private static void InitRabbitMQ()
        {
            // 正在启动线程
            Console.WriteLine("正在启动线程：" + Thread.CurrentThread.ManagedThreadId.ToString());
            factory.AutomaticRecoveryEnabled = true;

            connection = factory.CreateConnection();
            Console.WriteLine("正在创建：" + Thread.CurrentThread.ManagedThreadId.ToString());
            var categoryChannel = connection.CreateModel();
            categoryChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var novelChannel = connection.CreateModel();
            novelChannel.QueueDeclare(queue: "novel", true, false, false, null);//创建一个名称为novel的消息队列

            var htmlChannel = connection.CreateModel();
            htmlChannel.QueueDeclare(queue: "html", true, false, false, null);//创建一个名称为html的消息队列
            var htmlChannelProperties = htmlChannel.CreateBasicProperties();
            htmlChannelProperties.DeliveryMode = 2;

            categoryChannel.QueueDeclare(queue: "category", true, false, false, null);//创建一个名称为category的消息队列
            var consumer = new EventingBasicConsumer(categoryChannel);
            categoryChannel.BasicConsume(queue: "category", autoAck: false, consumer: consumer);

            consumer.Received += async (sender, e) =>
            {
                await OnRabbitMQMessageReceived(sender, e, htmlChannel, htmlChannelProperties, novelChannel, categoryChannel);
            };

            Console.WriteLine("线程启动完成：" + Thread.CurrentThread.ManagedThreadId.ToString());
        }

        private static async Task OnRabbitMQMessageReceived(object sender, BasicDeliverEventArgs e, IModel htmlChannel, IBasicProperties htmlChannelProperties, IModel novelChannel, IModel categoryChannel)
        {
            timeSlotsNames[1] = "OnRabbitMQMessageReceived";
            QueryPerformanceMethd.QueryPerformanceCounter(ref ts[1]);
            var httpCode = HttpStatusCode.Moved;
            try
            {
                timeSlotsNames[2] = "ParseMQMessage";
                QueryPerformanceMethd.QueryPerformanceCounter(ref ts[2]);
                var message = Encoding.UTF8.GetString(e.Body.ToArray());
                QueryPerformanceMethd.QueryPerformanceCounter(ref te[2]);
                timeSlots[2] = te[2] - ts[2];
                //Console.WriteLine("已接收： {0}", message);
                timeSlotsNames[3] = "AllHttpRequest";
                QueryPerformanceMethd.QueryPerformanceCounter(ref ts[3]);
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36");
                HttpResponseMessage response = null;

                for (var page = 1; true; page++)
                {
                    var url = message + "-page" + page;
                    Console.WriteLine("正在获取： {0}", url);
                    // 只管第一个
                    timeSlotsNames[4] = "HttpRequest";
                    QueryPerformanceMethd.QueryPerformanceCounter(ref ts[4]);

                    while (true)
                    {
                        response = await httpClient.GetAsync(url);

                        httpCode = response.StatusCode;
                        if (httpCode == HttpStatusCode.Moved || httpCode == HttpStatusCode.Redirect)
                        {
                            url = response.Headers.Location.ToString();
                        }
                        else
                        {
                            break;
                        }
                    }
                    var html = await response.Content.ReadAsStringAsync();

                    if (page == 1)
                    {
                        QueryPerformanceMethd.QueryPerformanceCounter(ref te[4]);
                        timeSlots[4] = te[4] - ts[4];
                    };

                    timeSlotsNames[5] = "htmlToRabbitMQ";
                    QueryPerformanceMethd.QueryPerformanceCounter(ref ts[5]);

                    // html内容保存到rabbitmq中，由程序逐步同步到pg数据库中
                    var htmlObj = new PageHtml()
                    {
                        Url = url,
                        Html = html,
                        CreatedTime = DateTime.Now
                    };
                    htmlChannel.BasicPublish("", "html", htmlChannelProperties, Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(htmlObj)));

                    QueryPerformanceMethd.QueryPerformanceCounter(ref te[5]);
                    timeSlots[5] = te[5] - ts[5];

                    //StackExchangeRedisHelper.Set(url, html);
                    // 对HTML进行正则表达式查找，查找下一页的链接
                    if (GetNovelsLink(novelChannel, html) < 10)
                    {
                        categoryChannel.BasicAck(e.DeliveryTag, true);
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", GetInnerExceptionString(ex, 0));
                if (ex.Message.Contains("redis"))
                {
                    Console.WriteLine("Error: Redis error, program terminating...");
                    isProgramTerminated = true;
                }
            }
            finally
            {
#if DEBUG
                QueryPerformanceMethd.QueryPerformanceCounter(ref te[1]);
                timeSlots[1] = te[1] - ts[1];
                QueryPerformanceMethd.QueryPerformanceCounter(ref te[3]);
                timeSlots[3] = te[3] - ts[3];



                var s = string.Format("{11},{0:G},{1:C5},{2:C5},{3:C5},{4:C5},{5:C5},{6:C5},{7:C5},{8:C5},{9:C5},{10:C5}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
                    timeSlots[0] / 1000000.0,
                    timeSlots[1] / 1000000.0,
                    timeSlots[2] / 1000000.0,
                    timeSlots[3] / 1000000.0,
                    timeSlots[4] / 1000000.0,
                    timeSlots[5] / 1000000.0,
                    timeSlots[6] / 1000000.0,
                    timeSlots[7] / 1000000.0,
                    timeSlots[8] / 1000000.0,
                    timeSlots[9] / 1000000.0,
                    ++csvlines
                    );
                performanceTracingSw.WriteLine(s);
#endif
            }

        }

        private static string GetInnerExceptionString(Exception ex, int level)
        {
            if (ex == null)
            {
                return "";
            }
            var str = ex.Message;
            if (ex.InnerException != null)
            {
                str += "\n" + Blanks.Substring(0, level) + "- " + GetInnerExceptionString(ex.InnerException, level + 1);
            }
            return str;
        }

        //static void NewPageq(object model, BasicDeliverEventArgs ea)
        //{
        //    try
        //    {
        //        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
        //        Console.WriteLine("已接收： {0}", message);
        //        var httpCode = HttpStatusCode.Moved;
        //        var httpClient = new HttpClient();
        //        HttpResponseMessage response = null;
        //        var url = message;
        //        while (true)
        //        {
        //            response = httpClient.GetAsync(url).Result;

        //            httpCode = response.StatusCode;
        //            if (httpCode == HttpStatusCode.Moved || httpCode == HttpStatusCode.Redirect)
        //            {
        //                url = response.Headers.Location.ToString();
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        var html = response.Content.ReadAsStringAsync().Result;

        //        StackExchangeRedisHelper.Set(message, html);
        //        // 对HTML进行正则表达式查找，查找下一页的链接
        //        GetNovelsLink(html);
        //        categoryChannel.BasicAck(ea.DeliveryTag, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error: {0}", ex.Message);
        //    }
        //}

        private static int GetNovelsLink(IModel novelChannel, string html)
        {
            timeSlotsNames[6] = "RegexHTML";
            QueryPerformanceMethd.QueryPerformanceCounter(ref ts[6]);

            var reg = "\\<a href=.*?/\\\" target=\\\"_blank\\\" data-eid=\\\".*?\\\" data-bid=\\\"(\\d*)\\\"\\>.*?\\</a\\>";
            var regex = new Regex(reg);
            var matches = regex.Matches(html);
            if (matches.Count == 0)
            {
                Console.WriteLine("no match");
            }
            QueryPerformanceMethd.QueryPerformanceCounter(ref te[6]);
            timeSlots[6] = te[6] - ts[6];

            timeSlotsNames[7] = "GetMatchesAndAddNonDuplicateToRedis";
            QueryPerformanceMethd.QueryPerformanceCounter(ref ts[7]);

            foreach (Match match in matches)
            {
                var url = string.Format("https://book.qidian.com/info/{0}/", match.Groups[1].Value);
                Console.WriteLine("novel found: {0}", url);
                // 检查这个url出现过没
                if (AddNonDuplicateToRedis(url))
                {
                    var properties = novelChannel.CreateBasicProperties();
                    properties.DeliveryMode = 2;
                    novelChannel.BasicPublish("", "novel", properties, Encoding.UTF8.GetBytes(url));
                }
            }
            QueryPerformanceMethd.QueryPerformanceCounter(ref te[7]);
            timeSlots[7] = te[7] - ts[7];

            return matches.Count;
        }
        private static bool AddNonDuplicateToRedis(string url)
        {
            if (!StackExchangeRedisHelper.Exists(url))
            {
                StackExchangeRedisHelper.Set(url, "");
                return true;
            }
            return false;
        }

        static List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();
        private static void AdjustThreads()
        {
            // 检查配置文件的线程数量
            var threadNumber = int.Parse(Common.GetSettings("General:threadNumber"));
            var increaseNum = threadNumber;
            // 检查现在的剩余线程数量

            // 如果现在剩余的多，挑几个给kill掉

            // 如果现在剩余的少，补足


            for (var i = 0; i < increaseNum; i++)
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                var task = Task.Run(() =>
                {
                    InitRabbitMQ();
                });
                Thread.Sleep(1000);
            }
        }

    }

    public static class QueryPerformanceMethd
    {
#if DEBUG

        [DllImport("kernel32.dll")]
        public extern static short QueryPerformanceCounter(ref long x);


        [DllImport("kernel32.dll")]
        public extern static short QueryPerformanceFrequency(ref long x);
#else
        public static short QueryPerformanceCounter(ref long x)
        {
            // do nothing
            return 1;
        }
        public static short QueryPerformanceFrequency(ref long x)
        {
            // do nothing
            return 1;
        }
#endif

    }
}
