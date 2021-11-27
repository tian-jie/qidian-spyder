using BookFinder.EntityFrameworkCore;
using BookFinder.Tools;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace BookFinderFullSolution
{
    public class StockSpiderManager
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

        public void AutoAdjust(int num, int high, int low)
        {
            _taskManager.AutoAdjust(num, high, low);
        }

        private static async void RunAction(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UrlObject urlObject = null;
                try
                {
                    urlObject = DataContainers.GetInstance().StockUrlList.GetOne();
                    //Console.WriteLine(url);
                    HttpResponseMessage response = null;

                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Referrer = new Uri("http://webapi.cninfo.com.cn/");
                    var timestamp = (DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0)).TotalSeconds;
                    Console.WriteLine($"timestamp: {timestamp}");

                    var mcode = Common.EncodeBase64(((int)timestamp).ToString());
                    var content = urlObject.FormUrlEncodedContent;
                    content.Headers.Add("mcode", mcode);
                    response = await httpClient.PostAsync(urlObject.Url, content);

                    var html = await response.Content.ReadAsStringAsync();

                    // 反序列化
                    var stockResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<StockResponse>(html);
                    if(stockResponse.count == 0)
                    {
                        break;
                    }
                    // 从stockDataCN转换成stockData
                    var stockData = stockResponse.records.Select(stockDataCN => new StockData()
                    {
                        code = stockDataCN.证券代码.Substring(0, 6),
                        name = stockDataCN.证券简称,
                        date = stockDataCN.交易日期,
                        open = stockDataCN.开盘价,
                        close = stockDataCN.收盘价,
                        high = stockDataCN.最高价,
                        low = stockDataCN.最低价,
                        float_percentage = stockDataCN.涨跌幅,
                        float_price = stockDataCN.涨跌,
                        transaction_number = stockDataCN.成交数量,
                        transaction_price = stockDataCN.成交金额,
                        market = stockDataCN.交易所
                    });

                    // 完事了塞数据库啊
                    using (var context = new BookFinderDbContext())
                    {
                        context.stock.AddRange(stockData);
                        context.SaveChanges();
                    }

                }
                catch (Exception ex)
                {
                    // 消费失败，要塞回去的
                    DataContainers.GetInstance().StockUrlList.AddOne(urlObject);
                }

            }

        }


        public int AvaliableThreadNum()
        {
            return _taskManager.ThreadCount();
        }

    }
}
