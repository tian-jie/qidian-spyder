using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace BookFinderFullSolution
{
    public class PageSpiderManager
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
            var httpClient = new HttpClient();
            while (!token.IsCancellationRequested)
            {
                var url = "";
                try
                {
                    url = DataContainers.GetInstance().CategoryUrlList.GetOne();
                    //Console.WriteLine(url);
                    HttpResponseMessage response = null;

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

                    // 写到html列表里
                    DataContainers.GetInstance().PageHtmlAckList.AddOne(new BookFinder.Tools.PageHtmlAck()
                    {
                        CreatedTime = DateTime.Now,
                        DeliveryTag = 0,
                        Html = html,
                        Url = url
                    });
                    GetNovelsLink(url.Split('/')[2], html);

                    //if(DataContainers.GetInstance().PageHtmlAckList.Count() > 10000)
                    //{
                    //    Thread.Sleep(10000);
                    //}
                }
                catch (Exception ex)
                {
                    // 消费失败，要塞回去的
                    DataContainers.GetInstance().CategoryUrlList.AddOne(url);
                }

            }

        }

        private static int GetNovelsLink(string host, string html)
        {
            var reg = "\\<a.*?href=\\\"(.*?)\\\"";
            var regex = new Regex(reg);
            var matches = regex.Matches(html);

            var newNovelNum = 0;
            var newCateNum = 0;
            var novelNum = 0;
            var cateNum = 0;

            foreach (Match match in matches)
            {
                var url = match.Groups[1].Value;
                if (url.StartsWith("//book.qidian.com"))
                {
                    novelNum++;
                    url = "https:" + url;
                    // 是一个book被找到了，检查这个url出现过没
                    if (!DataContainers.GetInstance().AllUrlList.Contains(url))
                    {
                        DataContainers.GetInstance().AllUrlList.AddOne(url);
                        DataContainers.GetInstance().BookUrlList.AddOne(url);
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
                    if (!DataContainers.GetInstance().AllUrlList.Contains(url))
                    {
                        DataContainers.GetInstance().AllUrlList.AddOne(url);
                        DataContainers.GetInstance().CategoryUrlList.AddOne(url);
                        newCateNum++;
                    }
                }
                else if (url.StartsWith("/"))
                {
                    cateNum++;
                    url = "https://" + host + url;
                    // 是一个页面被找到了，检查这个url出现过没
                    if (!DataContainers.GetInstance().AllUrlList.Contains(url))
                    {
                        DataContainers.GetInstance().AllUrlList.AddOne(url);
                        DataContainers.GetInstance().CategoryUrlList.AddOne(url);
                        newCateNum++;
                    }
                }
                //Thread.Sleep(10);
            }
            return matches.Count;
        }


        public int AvaliableThreadNum()
        {
            return _taskManager.ThreadCount();
        }

    }
}
