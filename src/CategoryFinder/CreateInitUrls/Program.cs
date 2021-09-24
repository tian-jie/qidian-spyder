using CategoryFinder;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Text;

namespace CreateInitUrls
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var factory = new ConnectionFactory()
            {
                HostName = Common.GetSettings("RabbitMQ:host"),
                UserName = Common.GetSettings("RabbitMQ:user"),
                Password = Common.GetSettings("RabbitMQ:password"),
                Port = int.Parse(Common.GetSettings("RabbitMQ:port"))
            };


            factory.AutomaticRecoveryEnabled = true;

            var connection = factory.CreateConnection();
            var categoryChannel = connection.CreateModel();

            categoryChannel.QueueDeclare(queue: "category", false, false, false, null);//创建一个名称为category的消息队列
            var properties = categoryChannel.CreateBasicProperties();


            string[] chancates = {
                "chanId21-subCateId8",
                "chanId21-subCateId78",
                "chanId21-subCateId58",
                "chanId21-subCateId73",
                "chanId1-subCateId38",
                "chanId1-subCateId62",
                "chanId1-subCateId201",
                "chanId1-subCateId202",
                "chanId1-subCateId20092",
                "chanId1-subCateId20093",
                "chanId2-subCateId5",
                "chanId2-subCateId30",
                "chanId2-subCateId206",
                "chanId2-subCateId20099",
                "chanId2-subCateId20100",
                "chanId22-subCateId18",
                "chanId22-subCateId44",
                "chanId22-subCateId64",
                "chanId22-subCateId207",
                "chanId22-subCateId20101",
                "chanId4-subCateId12",
                "chanId4-subCateId16",
                "chanId4-subCateId74",
                "chanId4-subCateId130",
                "chanId4-subCateId151",
                "chanId4-subCateId153",
                "chanId15-subCateId20104",
                "chanId15-subCateId20105",
                "chanId15-subCateId20106",
                "chanId15-subCateId20107",
                "chanId15-subCateId20108",
                "chanId15-subCateId6",
                "chanId15-subCateId209",
                "chanId6-subCateId54",
                "chanId6-subCateId65",
                "chanId6-subCateId80",
                "chanId6-subCateId230",
                "chanId6-subCateId231",
                "chanId5-subCateId22",
                "chanId5-subCateId48",
                "chanId5-subCateId220",
                "chanId5-subCateId32",
                "chanId5-subCateId222",
                "chanId5-subCateId223",
                "chanId5-subCateId224",
                "chanId5-subCateId225",
                "chanId5-subCateId225",
                "chanId5-subCateId20094",
                "chanId7-subCateId7",
                "chanId7-subCateId70",
                "chanId7-subCateId240",
                "chanId7-subCateId20102",
                "chanId7-subCateId20103",
                "chanId8-subCateId28",
                "chanId8-subCateId55",
                "chanId8-subCateId82",
                "chanId9-subCateId21",
                "chanId9-subCateId25",
                "chanId9-subCateId68",
                "chanId9-subCateId250",
                "chanId9-subCateId251",
                "chanId9-subCateId252",
                "chanId9-subCateId253",
                "chanId10-subCateId26",
                "chanId10-subCateId35",
                "chanId10-subCateId57",
                "chanId10-subCateId260",
                "chanId10-subCateId20095",
                "chanId12-subCateId60",
                "chanId12-subCateId66",
                "chanId12-subCateId281",
                "chanId12-subCateId282",
                "chanId20076-subCateId20097",
                "chanId20076-subCateId20098",
                "chanId20076-subCateId20075",
                "chanId20076-subCateId20077",
                "chanId20076-subCateId20078",
                "chanId20076-subCateId20079",
                "chanId20076-subCateId20096"
            };

            string[] actions = { "action0", "action1" };
            string[] vips = { "VIP0", "VIP1" };
            string[] sizes = { "size1", "size2", "size3", "size4", "size5" };
            string[] signs = { "sign1", "sign2" };
            string[] updates = { "update1", "update2", "update3", "update4" };
            string[] tags = { "tag豪门", "tag孤儿", "tag盗贼", "tag特工", "tag黑客", "tag明星", "tag特种兵", "tag杀手", "tag老师", "tag学生", "tag胖子", "tag宠物", "tag蜀山", "tag魔王附体", "tagLOL", "tag废材流", "tag护短", "tag卡片", "tag手游", "tag法师", "tag医生", "tag感情", "tag鉴宝", "tag亡灵", "tag职场", "tag吸血鬼", "tag龙", "tag西游", "tag鬼怪", "tag阵法", "tag魔兽", "tag勇猛", "tag玄学", "tag群穿", "tag丹药", "tag练功流", "tag召唤流", "tag恶搞", "tag爆笑", "tag轻松", "tag冷酷", "tag腹黑", "tag阳光", "tag狡猾", "tag机智", "tag猥琐", "tag嚣张", "tag淡定", "tag僵尸", "tag丧尸", "tag盗墓", "tag随身流", "tag软饭流", "tag无敌文", "tag异兽流", "tag系统流", "tag洪荒流", "tag学院流", "tag位面", "tag铁血", "tag励志", "tag坚毅", "tag变身", "tag强者回归", "tag赚钱", "tag争霸流", "tag种田文", "tag宅男", "tag无限流", "tag技术流", "tag凡人流", "tag热血", "tag重生", "tag穿越" };


            var totalCnt = chancates.Length * actions.Length * vips.Length * sizes.Length * signs.Length * updates.Length * tags.Length;
            var cnt = 0;
            using (var sw = new StreamWriter("f:\\1.txt"))
            {
                foreach (var chancate in chancates)
                {
                    foreach (var action in actions)
                    {
                        foreach (var vip in vips)
                        {
                            foreach (var size in sizes)
                            {
                                foreach (var sign in signs)
                                {
                                    foreach (var update in updates)
                                    {
                                        foreach (var tag in tags)
                                        {
                                            try
                                            {
                                                var str = string.Format("https://www.qidian.com/all/{0}-{1}-{2}-{3}-{4}-{5}-{6}", chancate, action, vip, size, sign, update, tag);
                                                StackExchangeRedisHelper.Set(str, "");
                                                categoryChannel.BasicPublish("", "category", properties, Encoding.UTF8.GetBytes(str));
                                                cnt++;
                                                if (cnt % 1000 == 0)
                                                {
                                                    Console.Write("\r -{0}/{1} ({2:0.00%})-", cnt, totalCnt, cnt * 1.0 / totalCnt);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.Message);
                                                Console.WriteLine(ex.StackTrace);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        //private static void InitRedis()
        //{
        //    redisClient = new ServiceStack.Redis.RedisClient(Common.GetSettings("Redis:connectionString"), int.Parse(Common.GetSettings("Redis:port")), "123");

        //    redisCli = ConnectionMultiplexer.Connect(new ConfigurationOptions());

        //    RedisConnectionHelp
        //    ////读取
        //    //string name = redisClient.Get<string>("name");
        //    //string pwd = redisClient.Get<string>("password");

        //    ////存储
        //    //redisClient.Set<string>("name1", username.Value);
        //    //redisClient.Set<string>("password1", userpwd.Value);
        //}

    }
}
