using System;
using System.Collections.Generic;
using YahooFinanceApi;

namespace PlayWithData
{
    public class StockData
    {
        public List<TagType> Tags;

        public string Symbol;

        public List<List<Candle>> HisData;

        public int CyclePerYear;

        public int DaysPerCycle;

        public int UpCycleCount;

        public int DownCycleCount;

        public double CycleAvgPercentage;

        public double UpCycleAvgPercentage;

        public double DownCycleAvgPercentage;

        public double HigherThanTodayPercentage;

        public double LowerThanTodayPercentage;

        public List<double[]> xs;

        public List<double[]> ys;

        public double IBoughtPrice;

        public double IBoughtAmount;

        public double CurrentProfitPercentage;

        public List<double> ProfitPercentageComparedToBefore;

        public double CurrentProfitAmount;

        public DateTime IBoughtDate;

        public bool Bought;

        public double TrailingPE;

        public StockData()
        {

        }

        public StockData(string symbol, TagType type)
        {
            Symbol = symbol;
            HisData = new List<List<Candle>>();
            Tags = new List<TagType>();
            xs = new List<double[]>();
            ys = new List<double[]>();
            Tags.Add(type);
            Bought = false;
            ProfitPercentageComparedToBefore = new List<double>();
        }

        public int DateTimeToInt(DateTime date)
        {
            return (int)(date - DateTime.Now).TotalDays + 365;
        }

        public void Prepare()
        {
            // Raw

            for (int i = 0; i < HisData.Count; i++)
            {
                double[] rawxs = new double[HisData[i].Count];
                double[] rawys = new double[HisData[i].Count];
                for (int j = 0; j < HisData[i].Count; j++)
                {
                    rawxs[j] = DateTimeToInt(HisData[i][j].DateTime);
                    double temp = (double)(HisData[i][j].Close);
                    rawys[j] = temp;
                }
                xs.Add(rawxs);
                ys.Add(rawys);
            }

        }

    }
}
