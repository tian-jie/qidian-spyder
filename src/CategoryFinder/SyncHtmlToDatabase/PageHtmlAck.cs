using Domain;

namespace SyncHtmlToDatabase
{
    class PageHtmlAck: PageHtml
    {
        public ulong DeliveryTag { get; set; }
    }
}
