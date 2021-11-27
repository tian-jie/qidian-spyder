using System;

namespace Domain
{
    public class StockData
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string date { get; set; }
        public double open { get; set; }
        public double close { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double float_percentage { get; set; }
        public double float_price { get; set; }
        public long transaction_number { get; set; }
        public double transaction_price { get; set; }
        public string market { get; set; }
    }

}
