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
            urlToRedisChannel.BasicQos(prefetchSize: 0, prefetchCount: 5000, global: false);


            var consumer = new EventingBasicConsumer(urlToRedisChannel);
            urlToRedisChannel.BasicConsume(queue: ackQueueName, autoAck: false, consumer: consumer);

            consumer.Received += (sender, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());
                //var pageHtml = Newtonsoft.Json.JsonConvert.DeserializeObject<PageHtml>(message);
                lock (_pageHtmlAckLocker)
                {
                    pageHtmlAckList.Add(new PageHtmlAck()
                    {
                        Url = message.Replace("\u0000", ""),
                        Html = "",
                        CreatedTime = DateTime.Now,
                        DeliveryTag = e.DeliveryTag
                    });
                }

            };


            while (true)
            {
                var novelNum = 0;
                var cateNum = 0;
                try
                {
                    Thread.Sleep(1000);
                    // 10秒钟同步一次数据，将预取出来的数据保存到数据库中，并且同时相应rabbitmq的ack
                    lock (_pageHtmlAckLocker)
                    {
                        Console.Write("{1} - {0} records recieved, duplicate checking...... ", pageHtmlAckList.Count, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        // 开始检查是否重复
                        var kvList = new List<KVPair>();
                        foreach (var pageHtmlAck in pageHtmlAckList)
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
                                urlToRedisChannel.BasicPublish("", "novel", properties, Encoding.UTF8.GetBytes(url));
                            }
                            else
                            {
                                cateNum++;
                                // 是一个页面被找到了，检查这个url出现过没
                                urlToRedisChannel.BasicPublish("", "category", properties, Encoding.UTF8.GetBytes(url));
                            }
                            _logger.Fatal(pageHtmlAck.Url);
                        }
                        Console.WriteLine("-- Finished, final to sync {0} records, {1} pages, {2} novels", kvList.Count, cateNum, novelNum);

                        StackExchangeRedisHelper.BatchInsert(kvList);
                        // 完事后给ack掉
                        foreach (var pageHtmlAck in pageHtmlAckList)
                        {
                            urlToRedisChannel.BasicAck(pageHtmlAck.DeliveryTag, true);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("{1} [{2}] - Error: {0}", BookFinder.Tools.Common.GetInnerExceptionString(ex, 0), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Thread.CurrentThread.ManagedThreadId.ToString()));
                }
                finally
                {
                    pageHtmlAckList.Clear();
                }
            }
        }

    }
}
