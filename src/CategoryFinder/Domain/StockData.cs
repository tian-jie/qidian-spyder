using System;

namespace Domain
{
    public class StockData
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double FloatPencentage { get; set; }
        public double FloatPrice { get; set; }
        public string TransactionNumber { get; set; }
        public double TransactionPrice { get; set; }
        public string Market { get; set; }
    }

}
