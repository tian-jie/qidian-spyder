using Domain;
using log4net;
using log4net.Config;
using log4net.Repository;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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

        static IConnection connection;


        private static ILoggerRepository LoggerRepository;
        private static ILog _logger;
        // 检查配置文件的线程数量
        private static int configThreadNumber = int.Parse(BookFinder.Tools.Common.GetSettings("General:threadNumber"));

        private static IModel _categoryChannel;
        private static IModel _novelChannel;
        private static IModel _urlToRedisChannel;
        private static IModel _htmlChannel;
        private static IBasicProperties _htmlChannelProperties;


        static void Main(string[] args)
        {

            LoggerRepository = LogManager.CreateRepository("Log4netConsolePractice");
            XmlConfigurator.ConfigureAndWatch(LoggerRepository, new FileInfo(BookFinder.Tools.Common.GetSettings("LogConfiguration:log4netConfigFile")));
            _logger = LogManager.GetLogger(LoggerRepository.Name, typeof(Program));

            _logger.Debug("System warm up...");

            //Timer timer = new Timer(delegate
            //    {
            //        AdjustThreads();
            //    },
            //    null,
            //    2000,
            //    1000
            //);
            var programTimeCnt = 0;

            InitRabbitMQ();

            while (!isProgramTerminated)
            {
                var restartDuration = int.Parse(BookFinder.Tools.Common.GetSettings("General:restartDuration"));

                AdjustThreads();
                if (++programTimeCnt >= restartDuration)
                {
                    break;
                }
                Thread.Sleep(10000);
            }
        }

        private static void InitRabbitMQ()
        {
            try
            {
                // 正在启动线程
                Console.WriteLine("正在初始化RabbitMQ......");
                factory.AutomaticRecoveryEnabled = true;

                connection = factory.CreateConnection();

                _categoryChannel = connection.CreateModel();
                _categoryChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                _categoryChannel.QueueDeclare(queue: "category", true, false, false, null);//创建一个名称为category的消息队列
                var consumer = new DefaultBasicConsumer(_categoryChannel);
                _categoryChannel.BasicConsume(queue: "category", autoAck: false, consumer: consumer);

                _novelChannel = connection.CreateModel();
                _novelChannel.QueueDeclare(queue: "novel", true, false, false, null);//创建一个名称为novel的消息队列

                _urlToRedisChannel = connection.CreateModel();
                _urlToRedisChannel.QueueDeclare(queue: "urlToRedis", true, false, false, null);//创建一个名称为novel的消息队列

                _htmlChannel = connection.CreateModel();
                _htmlChannel.QueueDeclare(queue: "html", true, false, false, null);//创建一个名称为html的消息队列
                _htmlChannelProperties = _htmlChannel.CreateBasicProperties();
                _htmlChannelProperties.DeliveryMode = 2;


                Console.WriteLine("初始化RabbitMQ完成");

                //consumer.Received += async (sender, e) =>
                //{
                //    await OnRabbitMQMessageReceived(e.Body.ToArray(), e.DeliveryTag, htmlChannel, htmlChannelProperties, novelChannel, categoryChannel, httpClient);
                //};

                // 初始化Redis
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0)));
                Console.WriteLine(string.Format("Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0)));
                Console.WriteLine("初始化RabbitMQ错误");
            }
        }

        private static async void RabbitMQThread(IModel categoryChannel_T, IModel novelChannel, IModel htmlChannel, IBasicProperties htmlChannelProperties)
        {
            try
            {
                var categoryChannel = connection.CreateModel();
                //categoryChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                categoryChannel.QueueDeclare(queue: "category", true, false, false, null);//创建一个名称为category的消息队列
                //var consumer = new DefaultBasicConsumer(categoryChannel);
                //categoryChannel.BasicConsume(queue: "category", autoAck: false, consumer: consumer);

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36");

                currentThreadNumber++;
                while (true)
                {
                    var message = categoryChannel.BasicGet(queue: "category", autoAck: false);
                    if(message == null)
                    {
                        Console.WriteLine("没找到，等10秒再找...");
                        Thread.Sleep(10000);
                        continue;
                    }
                    await OnRabbitMQMessageReceived(message.Body.ToArray(), message.DeliveryTag, htmlChannel, htmlChannelProperties, novelChannel, categoryChannel, httpClient);
                }
            }catch(Exception ex)
            {
                _logger.Error(string.Format("{1} [{2}] - Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Thread.CurrentThread.ManagedThreadId.ToString()));
                Console.WriteLine(string.Format("{1} [{2}] - Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Thread.CurrentThread.ManagedThreadId.ToString()));
                Console.WriteLine("线程退出：" + Thread.CurrentThread.ManagedThreadId.ToString());
                currentThreadNumber--;
            }
        }

        private static async Task OnRabbitMQMessageReceived(byte[] body, ulong deliveryTag, IModel htmlChannel, IBasicProperties htmlChannelProperties, IModel novelChannel, IModel categoryChannel, HttpClient httpClient)
        {
            var message = Encoding.UTF8.GetString(body);
            HttpResponseMessage response = null;

            var url = message;
            _logger.Debug(string.Format("正在获取： {0}", url));

            while (true)
            {
                response = await httpClient.GetAsync(url);

                var httpCode = response.StatusCode;
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

            // html内容保存到rabbitmq中，由程序逐步同步到pg数据库中
            var htmlObj = new PageHtml()
            {
                Url = url,
                Html = html,
                CreatedTime = DateTime.Now
            };
            htmlChannel.BasicPublish("", "html", htmlChannelProperties, Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(htmlObj)));

            //StackExchangeRedisHelper.Set(url, html);
            // 对HTML进行正则表达式查找，查找下一页的链接
            GetNovelsLink(url.Split('/')[2], html);
            categoryChannel.BasicAck(deliveryTag, true);
            _logger.Debug(string.Format("获取完成： {0}", url));

        }


        private static int GetNovelsLink(string host, string html)
        {
            //timeSlotsNames[6] = "RegexHTML";
            //QueryPerformanceMethd.QueryPerformanceCounter(ref ts[6]);

            //var reg = "\\<a href=.*?/\\\" target=\\\"_blank\\\" data-eid=\\\".*?\\\" data-bid=\\\"(\\d*)\\\"\\>.*?\\</a\\>";
            var reg = "\\<a.*?href=\\\"(.*?)\\\"";
            var regex = new Regex(reg);
            var matches = regex.Matches(html);
            if (matches.Count == 0)
            {
                Console.WriteLine("no match");
            }
            //QueryPerformanceMethd.QueryPerformanceCounter(ref te[6]);
            //timeSlots[6] = te[6] - ts[6];

            //timeSlotsNames[7] = "GetMatchesAndAddNonDuplicateToRedis";
            //QueryPerformanceMethd.QueryPerformanceCounter(ref ts[7]);
            var properties = _novelChannel.CreateBasicProperties();
            properties.DeliveryMode = 2;

            var newNovelNum = 0;
            var newCateNum = 0;
            var novelNum = 0;
            var cateNum = 0;

            foreach (Match match in matches)
            {
                //var url = string.Format("https://book.qidian.com/info/{0}/", match.Groups[1].Value);
                var url = match.Groups[1].Value;
                if (url.StartsWith("//book.qidian.com"))
                {
                    novelNum++;
                    url = "https:" + url;
                    // 是一个book被找到了，检查这个url出现过没
                    //if (AddNonDuplicateToRedis(url))
                    {
                        _urlToRedisChannel.BasicPublish("", "urlToRedis", properties, Encoding.UTF8.GetBytes(url));
                        newNovelNum++;
                    }
                }
                else if (url.StartsWith("//"))
                {
                    cateNum++;
                    if (!url.Contains("qidian.com"))
                    {
                        continue;
                    }
                    url = "https:" + url;
                    // 是一个页面被找到了，检查这个url出现过没
                    //if (AddNonDuplicateToRedis(url))
                    {
                        _urlToRedisChannel.BasicPublish("", "urlToRedis", properties, Encoding.UTF8.GetBytes(url));
                        newCateNum++;
                    }
                }
                else if (url.StartsWith("/"))
                {
                    cateNum++;
                    url = "https://" + host + url;
                    // 是一个页面被找到了，检查这个url出现过没
                    //if (AddNonDuplicateToRedis(url))
                    {
                        _urlToRedisChannel.BasicPublish("", "urlToRedis", properties, Encoding.UTF8.GetBytes(url));
                        newCateNum++;
                    }
                }
                Thread.Sleep(10);
            }
            _logger.Info(string.Format("本页抓取到：页面- {0}, 小说: {1}, 新页面: {2}, 新小说: {3}", cateNum, novelNum, newCateNum, newNovelNum));

            return matches.Count;
        }
        private static bool AddNonDuplicateToRedis(string url)
        {
            if (!StackExchangeRedisHelper.Exists(url))
            {
                StackExchangeRedisHelper.Set(url, "");
                _logger.Warn(url);
                return true;
            }
            return false;
        }

        static List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();

        static List<Task> _tasks = new List<Task>();
        static bool _isIncreasingThread = false;
        static int currentThreadNumber = 0;
        private static void AdjustThreads()
        {
            if (_isIncreasingThread)
            {
                Console.WriteLine("正在启动线程中.....");
                return;
            }
            // 检查现在的剩余线程数量
            //for(var i=0; i<_tasks.Count; i++)
            //{
            //    if (_tasks[i].Status == TaskStatus.RanToCompletion)
            //    {
            //        _tasks.RemoveAt(i);
            //        i--;
            //    }
            //}
            //var currentThreadNumber = _tasks.Count;

            // 如果现在剩余的多，挑几个给kill掉

            // 如果现在剩余的少，补足
            var increaseNum = configThreadNumber - currentThreadNumber;
            Console.Write("{2} - 剩余线程数：{0}, 需要补足线程数: {1} ...  ", currentThreadNumber, increaseNum, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _isIncreasingThread = true;
            for (var i = 0; i < increaseNum; i++)
            {
                Task.Run(() =>
                {
                    RabbitMQThread(_categoryChannel, _novelChannel, _htmlChannel, _htmlChannelProperties);
                });
                //_tasks.Add(task);
                Thread.Sleep(1000);
            }
            Console.WriteLine("补充完成！");
            _isIncreasingThread = false;
        }

    }

}
