using System;

namespace Domain
{
    public class PageHtml
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Html { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
