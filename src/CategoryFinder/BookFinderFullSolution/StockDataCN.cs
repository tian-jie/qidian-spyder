using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace BookFinderFullSolution
{
    public class StockDataCN
    {
        public string 证券代码 { get; set; }
        public string 证券简称 { get; set; }
        public string 交易日期 { get; set; }
        public double 开盘价 { get; set; }
        public double 收盘价 { get; set; }
        public double 最高价 { get; set; }
        public double 最低价 { get; set; }
        public double 涨跌 { get; set; }
        public double 涨跌幅 { get; set; }
        public string 交易所 { get; set; }
        public string 币种 { get; set; }
        public long 成交数量 { get; set; }
        public double 成交金额 { get; set; }
    }

    public class StockResponse
    {
        public int count { get; set; }
        public List<StockDataCN> records { get; set; }
        public int resultcode { get; set; }
        public string resultmsg { get; set; }
        public int total { get; set; }
    }
}
