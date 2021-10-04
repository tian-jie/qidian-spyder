using BookFinder.Tools;
using BookFinder.EntityFrameworkCore;
using Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SyncHtmlToDatabase
{
    class Program
    {
        static List<PageHtmlAck> pageHtmlAckList = new List<PageHtmlAck>();
        static object _pageHtmlAckLocker = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! - SyncHtmlToDatabase");

            using (var context = new BookFinderDbContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = Common.GetSettings("RabbitMQ:host"),
                UserName = Common.GetSettings("RabbitMQ:user"),
                Password = Common.GetSettings("RabbitMQ:password"),
                Port = int.Parse(Common.GetSettings("RabbitMQ:port"))
            };

            var connection = factory.CreateConnection();

            var htmlChannel = connection.CreateModel();
            htmlChannel.QueueDeclare(queue: "html", true, false, false, null);//创建一个名称为html的消息队列
            var htmlChannelProperties = htmlChannel.CreateBasicProperties();
            htmlChannelProperties.DeliveryMode = 2;
            htmlChannel.BasicQos(prefetchSize: 0, prefetchCount: 1000, global: false);


            var consumer = new EventingBasicConsumer(htmlChannel);
            htmlChannel.BasicConsume(queue: "html", autoAck: false, consumer: consumer);

            consumer.Received += (sender, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());
                var pageHtml = Newtonsoft.Json.JsonConvert.DeserializeObject<PageHtml>(message);
                lock (_pageHtmlAckLocker)
                {
                    pageHtmlAckList.Add(new PageHtmlAck()
                    {
                        Url = pageHtml.Url.Replace("\u0000", ""),
                        Html = pageHtml.Html.Replace("\u0000", ""),
                        CreatedTime = pageHtml.CreatedTime,
                        DeliveryTag = e.DeliveryTag
                    });
                }

            };

            while (true)
            {
                Thread.Sleep(5000);
                // 10秒钟同步一次数据，将预取出来的数据保存到数据库中，并且同时相应rabbitmq的ack
                lock (_pageHtmlAckLocker)
                {
                    Console.Write("{1} - Time up for syncing data - {0} records ...... ", pageHtmlAckList.Count, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    using (var context = new BookFinderDbContext())
                    {
                        context.PageHtmls.AddRange(pageHtmlAckList);
                        context.SaveChanges();
                        foreach (var phd in pageHtmlAckList)
                        {
                            htmlChannel.BasicAck(phd.DeliveryTag, true);
                        }
                        pageHtmlAckList.Clear();
                    }
                    Console.WriteLine("Done");
                }

            }
        }
    }
}
