using BookFinder.Tools;
using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class DataContainers
    {

        private static DataContainers _dataContainers = new DataContainers();

        private DataContainers()
        {
            BookUrlList.EnsureCapacity(1000000);
            CategoryUrlList.EnsureCapacity(10000000);
            TemporaryCategoryUrlList.EnsureCapacity(10000000);
            TemporaryBookUrlList.EnsureCapacity(10000000);
            PageHtmlAckList.EnsureCapacity(100000);

        }

        public static DataContainers GetInstance()
        {
            return _dataContainers;
        }

        public ComplexStringListObj CategoryUrlList = new ComplexStringListObj("CategoryUrlList");

        public StringListObj BookUrlList = new StringListObj("BookUrlList");

        public StringListObj TemporaryCategoryUrlList = new StringListObj("TemporaryCategoryUrlList");

        public StringListObj TemporaryBookUrlList = new StringListObj("TemporaryBookUrlList");

        public ComplexStringListObj AllUrlList = new ComplexStringListObj("AllUrlList");

        public NonStringListObj<PageHtmlAck> PageHtmlAckList = new NonStringListObj<PageHtmlAck>("PageHtmlAckList");
        public NonStringListObj<UrlObject> StockUrlList = new NonStringListObj<UrlObject>("StockUrlList");


        public void Serialize()
        {
            ConsoleLogger.Debug("   CategoryUrlList.Count: ...");
            var categoryUrlListCnt = CategoryUrlList.Count(false);
            ConsoleLogger.Debug("\r   CategoryUrlList.Count = {0}", categoryUrlListCnt);
            if (categoryUrlListCnt > 100000)
            {
                ConsoleLogger.Debug("   CategoryUrlList.Archive");
                // 只留下5万
                CategoryUrlList.Archive(categoryUrlListCnt - 50000);
                ConsoleLogger.Debug("   CategoryUrlList.Archive done");
            }
            else if (categoryUrlListCnt < 10000)
            {
                ConsoleLogger.Debug("   CategoryUrlList.Dearchive");
                CategoryUrlList.Dearchive(50000);
                ConsoleLogger.Debug("   CategoryUrlList.Dearchive done");
            }
            ConsoleLogger.Debug("   CategoryUrlList.Serialize");
            CategoryUrlList.Serialize(true);
            ConsoleLogger.Debug("   BookUrlList.Serialize");
            BookUrlList.Serialize();
            ConsoleLogger.Debug("   TemporaryCategoryUrlList.Serialize");
            TemporaryCategoryUrlList.Serialize();
            ConsoleLogger.Debug("   TemporaryBookUrlList.Serialize");
            TemporaryBookUrlList.Serialize();
            ConsoleLogger.Debug("   PageHtmlAckList.Serialize");
            PageHtmlAckList.Serialize();
            ConsoleLogger.Debug("   AllUrlList.Archive");
            AllUrlList.Serialize(true);
            ConsoleLogger.Debug("   Serialize finished");
        }

        public void Deserialize()
        {
            CategoryUrlList.Deserialize();
            BookUrlList.Deserialize();
            TemporaryCategoryUrlList.Deserialize();
            TemporaryBookUrlList.Deserialize();
            PageHtmlAckList.Deserialize();
            AllUrlList.Deserialize();
        }
    }


}
