using Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
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
            HostName = Common.GetSettings("RabbitMQ:host"),
            UserName = Common.GetSettings("RabbitMQ:user"),
            Password = Common.GetSettings("RabbitMQ:password"),
            Port = int.Parse(Common.GetSettings("RabbitMQ:port"))
        };

        static IConnection connection;

        const string Blanks = "                                                                                                     ";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            AdjustThreads();
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
                if (++programTimeCnt >= restartDuration)
                {
                    break;
                }
            }
        }

        private static void InitRabbitMQ()
        {
            // 正在启动线程
            Console.WriteLine("正在启动线程：" + Thread.CurrentThread.ManagedThreadId.ToString());
            factory.AutomaticRecoveryEnabled = true;

            connection = factory.CreateConnection();
            var categoryChannel = connection.CreateModel();
            categoryChannel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

            var novelChannel = connection.CreateModel();
            novelChannel.QueueDeclare(queue: "novel", true, false, false, null);//创建一个名称为novel的消息队列

            var htmlChannel = connection.CreateModel();
            htmlChannel.QueueDeclare(queue: "html", true, false, false, null);//创建一个名称为html的消息队列
            var htmlChannelProperties = htmlChannel.CreateBasicProperties();
            htmlChannelProperties.DeliveryMode = 2;

            categoryChannel.QueueDeclare(queue: "category", true, false, false, null);//创建一个名称为category的消息队列
            var consumer = new EventingBasicConsumer(categoryChannel);
            categoryChannel.BasicConsume(queue: "category", autoAck: false, consumer: consumer);
            var httpCode = HttpStatusCode.Moved;

            consumer.Received += (sender, e) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(e.Body.ToArray());
                    //Console.WriteLine("已接收： {0}", message);
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36");
                    HttpResponseMessage response = null;
                    for (var page = 1; true; page++)
                    {
                        var url = message + "-page" + page;
                        Console.WriteLine("正在获取： {0}", url);
                        while (true)
                        {
                            response = httpClient.GetAsync(url).Result;

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

                        var html = response.Content.ReadAsStringAsync().Result;
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
            };
            categoryChannel.BasicConsume(queue: "category", autoAck: false, consumer: consumer);
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
            var reg = "\\<a href=\\\"(//book\\.qidian.com/info/\\d*)\\\" target=\\\"_blank\\\" data-eid=\\\".*?\\\" data-bid=\\\"\\d*\\\"\\>.*?\\</a\\>";
            var regex = new Regex(reg);
            var matches = regex.Matches(html);
            if (matches.Count == 0)
            {
                Console.WriteLine("no match");
            }
            foreach (Match match in matches)
            {
                var url = "https:" + match.Groups[1].Value;
                Console.WriteLine("novel found: {0}", url);
                // 检查这个url出现过没
                if (AddNonDuplicateToRedis(url))
                {
                    var properties = novelChannel.CreateBasicProperties();
                    properties.DeliveryMode = 2;
                    novelChannel.BasicPublish("", "novel", properties, Encoding.UTF8.GetBytes(url));
                }
            }
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

            }
        }

    }
}
