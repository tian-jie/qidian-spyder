using BookFinder.EntityFrameworkCore;
using BookFinder.Tools;
using CategoryFinder;
using Domain;
using log4net;
using log4net.Config;
using log4net.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinder.DuplicateUrlCheck
{
    class Program
    {
        static List<PageHtmlAck> pageHtmlAckList = new List<PageHtmlAck>();
        static object _pageHtmlAckLocker = new object();

        const string ackQueueName = "urlToRedis";
        private static ILoggerRepository LoggerRepository;
        private static ILog _logger;


        static void Main(string[] args)
        {
            LoggerRepository = LogManager.CreateRepository("Log4netConsolePractice");
            XmlConfigurator.ConfigureAndWatch(LoggerRepository, new FileInfo(BookFinder.Tools.Common.GetSettings("LogConfiguration:log4netConfigFile")));
            _logger = LogManager.GetLogger(LoggerRepository.Name, typeof(Program));

            _logger.Debug("System warm up...");

            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = Common.GetSettings("RabbitMQ:host"),
                UserName = Common.GetSettings("RabbitMQ:user"),
                Password = Common.GetSettings("RabbitMQ:password"),
                Port = int.Parse(Common.GetSettings("RabbitMQ:port"))
            };

            var connection = factory.CreateConnection();

            var urlToRedisChannel = connection.CreateModel();
            urlToRedisChannel.QueueDeclare(queue: ackQueueName, true, false, false, null);//创建一个名称为html的消息队列
            var properties = urlToRedisChannel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            urlToRedisChannel.BasicQos(prefetchSize: 0, prefetchCount: 2000, global: false);


            var consumer = new EventingBasicConsumer(urlToRedisChannel);
            urlToRedisChannel.BasicConsume(queue: ackQueueName, autoAck: false, consumer: consumer);

            var novelChannel = connection.CreateModel();
            novelChannel.QueueDeclare(queue: "novel", true, false, false, null);//创建一个名称为html的消息队列
            var categoryChannel = connection.CreateModel();
            categoryChannel.QueueDeclare(queue: "category", true, false, false, null);//创建一个名称为html的消息队列


            Task.Run(() =>
            {
                Thread.Sleep(5000);
                WorkerThread(urlToRedisChannel, novelChannel, categoryChannel, properties);
            });

            while (true)
            {
                try
                {
                    var message = urlToRedisChannel.BasicGet(queue: ackQueueName, autoAck: false);
                    if (message == null)
                    {
                        Console.WriteLine("没找到，等1秒再找...");
                        Thread.Sleep(1000);
                        continue;
                    }
                    lock (_pageHtmlAckLocker)
                    {
                        pageHtmlAckList.Add(new PageHtmlAck()
                        {
                            Url = Encoding.UTF8.GetString(message.Body.ToArray()).Replace("\u0000", ""),
                            Html = "",
                            CreatedTime = DateTime.Now,
                            DeliveryTag = message.DeliveryTag
                        });
                    }
                }
                catch (Exception ex)
                {
                    //_logger.Error(string.Format("{1} [{2}] - Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Thread.CurrentThread.ManagedThreadId.ToString()));
                    Console.WriteLine(string.Format("{1} [{2}] - Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Thread.CurrentThread.ManagedThreadId.ToString()));
                }
            }
        }

        private static void WorkerThread(IModel urlToRedisChannel, IModel novelChannel, IModel categoryChannel, IBasicProperties properties)
        {
            while (true)
            {
                var novelNum = 0;
                var cateNum = 0;
                try
                {
                    Thread.Sleep(500);
                    // 给list复制出来，然后释放list接着用
                    var tmpList = new List<PageHtmlAck>();
                    lock (_pageHtmlAckLocker)
                    {
                        Console.Write("{1} - {0} records recieved, duplicate checking...... ", pageHtmlAckList.Count, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        foreach (var pageHtmlAck in pageHtmlAckList)
                        {
                            tmpList.Add(pageHtmlAck);
                        }

                        pageHtmlAckList.Clear();
                    }

                    // 10秒钟同步一次数据，将预取出来的数据保存到数据库中，并且同时相应rabbitmq的ack
                    // 开始检查是否重复
                    var kvList = new List<KVPair>();
                    foreach (var pageHtmlAck in tmpList)
                    {
                        if (StackExchangeRedisHelper.Exists(pageHtmlAck.Url))
                        {
                            continue;
                        }
                        if (kvList.Exists(a => a.Key == pageHtmlAck.Url))
                        {
                            continue;
                        }
                        kvList.Add(new KVPair()
                        {
                            Key = pageHtmlAck.Url,
                            Value = pageHtmlAck.DeliveryTag.ToString()
                        });
                        var url = pageHtmlAck.Url;

                        // 放到rabbitmq里
                        if (url.StartsWith("https://book.qidian.com"))
                        {
                            novelNum++;
                            // 是一个book被找到了，检查这个url出现过没
                            novelChannel.BasicPublish("", "novel", properties, Encoding.UTF8.GetBytes(url));
                        }
                        else
                        {
                            cateNum++;
                            // 是一个页面被找到了，检查这个url出现过没
                            categoryChannel.BasicPublish("", "category", properties, Encoding.UTF8.GetBytes(url));
                        }
                        _logger.Fatal(pageHtmlAck.Url);
                    }
                    Console.WriteLine("-- Finished, final to sync {0} records, {1} pages, {2} novels", kvList.Count, cateNum, novelNum);

                    StackExchangeRedisHelper.BatchInsert(kvList);
                    // 完事后给ack掉
                    foreach (var pageHtmlAck in tmpList)
                    {
                        urlToRedisChannel.BasicAck(pageHtmlAck.DeliveryTag, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("{1} [{2}] - Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Thread.CurrentThread.ManagedThreadId.ToString()));
                }
            }
        }
    }
}
