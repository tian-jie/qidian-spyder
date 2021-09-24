using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CategoryFinder
{
    class Program
    {
        static ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = Common.GetSettings("RabbitMQ:host"),
            UserName = Common.GetSettings("RabbitMQ:user"),
            Password = Common.GetSettings("RabbitMQ:password"),
            Port = int.Parse(Common.GetSettings("RabbitMQ:port"))
        };

        static IConnection connection;
        static IModel categoryChannel;


        static IModel novelChannel;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            InitRabbitMQ();

        }

        private static void InitRabbitMQ()
        {
            factory.AutomaticRecoveryEnabled = true;

            connection = factory.CreateConnection();
            categoryChannel = connection.CreateModel();

            categoryChannel.QueueDeclare(queue: "category", false, false, false, null);//创建一个名称为category的消息队列
            var consumer = new EventingBasicConsumer(categoryChannel);
            categoryChannel.BasicConsume("category", false, consumer);
            consumer.Received += NewPageq;
            categoryChannel.BasicConsume(queue: "category", autoAck: false, consumer: consumer);

            novelChannel = connection.CreateModel();
            novelChannel.QueueDeclare(queue: "novel", false, false, false, null);//创建一个名称为category的消息队列

            while (true)
            {
                Thread.Sleep(int.MaxValue);
            }
        }

        static void NewPageq(object model, BasicDeliverEventArgs ea)
        {
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine("已接收： {0}", message);

                var httpClient = new HttpClient();
                var response = httpClient.GetAsync(message).Result;
                var html = response.Content.ReadAsStringAsync().Result;
                StackExchangeRedisHelper.Set(message, html);

                StackExchangeRedisHelper.Set(message, html);
                // 对HTML进行正则表达式查找，查找下一页的链接
                GetNovelsLink(html);
                categoryChannel.BasicAck(ea.DeliveryTag, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        private static void GetNovelsLink(string html)
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
                    novelChannel.BasicPublish("", "novel", properties, Encoding.UTF8.GetBytes(url));
                }
            }

        }
        private static bool AddNonDuplicateToRedis(string url)
        {
            
            if (StackExchangeRedisHelper.Exists(url))
            {
                StackExchangeRedisHelper.Set(url, "");
                return true;
            }
            return false;
        }

    }
}
