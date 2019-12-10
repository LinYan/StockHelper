using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using YahooFinanceApi;

namespace PlayWithData
{
    public class Process
    {
        public static int lookBackYear = 1;

        private static readonly HttpClient client = new HttpClient();

        public Dictionary<string, StockData> interested;

        public Dictionary<string, StockData> alls;

        public Dictionary<string, StockData> bought;

        public Process()
        {
            interested = new Dictionary<string, StockData>();
            alls = new Dictionary<string, StockData>();
            bought = new Dictionary<string, StockData>();
        }

        private void LoadSingle(string fileName, TagType tagType)
        {
            using (StreamReader sr = new StreamReader(File.OpenRead(fileName)))
            {
                while (!sr.EndOfStream)
                {
                    string symbol = sr.ReadLine();
                    if (!interested.ContainsKey(symbol))
                    {
                        interested.Add(symbol, new StockData(symbol, tagType));
                    }
                }
            }
        }

        private void LoadSingleToAlls(string fileName, TagType tagType)
        {
            using (StreamReader sr = new StreamReader(File.OpenRead(fileName)))
            {
                while (!sr.EndOfStream)
                {
                    string symbol = sr.ReadLine();
                    if (!alls.ContainsKey(symbol))
                    {
                        alls.Add(symbol, new StockData(symbol, tagType));
                    }
                }
            }
        }

        private void LoadByCategory()
        {
            LoadSingle("OtherInterested.txt", TagType.OtherInterested);

            //LoadSingleToAlls("SP500Symbols.txt", TagType.SP500Symbols);
            //LoadSingleToAlls("DowJones30Symbols.txt", TagType.DowJones30Symbols);
            //LoadSingleToAlls("NASDAQ100Symbols.txt", TagType.NASDAQ100Symbols);
            LoadSingleToAlls("OtherInterested.txt", TagType.OtherInterested);
        }

        public void MainLogic()
        {
            // Get the Symbol List
            LoadByCategory();

            // Overall Analysis
            foreach (var item in alls)// for each symbol
            {
                try
                {
                    var securities = Yahoo.Symbols(item.Key).Fields(Field.TrailingPE).QueryAsync().Result;
                    item.Value.TrailingPE = securities[item.Key].TrailingPE;
                }
                catch (Exception e)
                {
                    if (e.Message == "The given key was not present in the dictionary.")
                    {
                        continue;
                    }
                    throw;
                }
            }

            // Get their history data and generate feature
            foreach (var item in interested)// for each symbol
            {
                item.Value.HisData.Add(Yahoo.GetHistoricalAsync(item.Key, DateTime.Now.AddYears(-1 * lookBackYear), DateTime.Now).Result.ToList());

                CurrentPriceAnalysis(item);

                CycleAnlaysis(item);

                item.Value.Prepare();// Prepare to show
            }

            // Prepare the bought stocks data

            using (StreamReader sr = new StreamReader(File.OpenRead("Bought.txt")))
            {
                while (!sr.EndOfStream)
                {
                    string symbol = sr.ReadLine();
                    interested[symbol].IBoughtPrice = double.Parse(sr.ReadLine());
                    interested[symbol].IBoughtAmount = double.Parse(sr.ReadLine());
                    interested[symbol].IBoughtDate = new DateTime(int.Parse(sr.ReadLine()), int.Parse(sr.ReadLine()), int.Parse(sr.ReadLine()));
                    interested[symbol].CurrentProfitPercentage = Math.Round(((double)interested[symbol].HisData[0].Last().Close - interested[symbol].IBoughtPrice) / interested[symbol].IBoughtPrice * 100, 2);
                    for (int i = 0; i < 7; i++)
                    {
                        double yes = (double)interested[symbol].HisData[0][interested[symbol].HisData[0].Count - 1 - i].Close;
                        double yesyes = (double)interested[symbol].HisData[0][interested[symbol].HisData[0].Count - 2 - i].Close;
                        interested[symbol].ProfitPercentageComparedToBefore.Add(Math.Round((yes - yesyes) / yesyes * 100, 2));
                    }
                    interested[symbol].CurrentProfitAmount = Math.Round(interested[symbol].CurrentProfitPercentage * interested[symbol].IBoughtAmount * interested[symbol].IBoughtPrice / 100, 2);
                    interested[symbol].Bought = true;
                    bought.Add(symbol, interested[symbol]);
                }
            }
        }

        private void CurrentPriceAnalysis(KeyValuePair<string, StockData> item)
        {
            decimal today = item.Value.HisData[0].Last().Close;
            decimal LowerThanToday = 0;
            decimal HigherThanToday = 0;
            int higherCount = 0;
            for (int i = 0; i < item.Value.HisData[0].Count; i++)
            {
                if (item.Value.HisData[0][i].Close > today)
                {
                    HigherThanToday += item.Value.HisData[0][i].Close - today;
                    higherCount++;
                }
                else
                {
                    LowerThanToday += today - item.Value.HisData[0][i].Close;
                }
            }
            if (higherCount == 0)
            {
                item.Value.HigherThanTodayPercentage = 0;
            }
            else
            {
                item.Value.HigherThanTodayPercentage = (double)Math.Round(HigherThanToday / higherCount * 100, 2);
            }

            if (item.Value.HisData[0].Count - higherCount == 0)
            {
                item.Value.LowerThanTodayPercentage = 0;
            }
            else
            {
                item.Value.LowerThanTodayPercentage = (double)Math.Round(LowerThanToday / (item.Value.HisData[0].Count - higherCount) * 100, 2);
            }
        }

        private void CycleAnlaysis(KeyValuePair<string, StockData> item)
        {
            int index = 0;
            int step = 1;
            //1-2-1-2-1 R-M-R-M-R-M
            while (index < 5)// Merge by 1 and 2 step with neighbor points for 5 rounds in total
            {
                index++;
                MergeLoop(index, step, item.Value.HisData);
                step = step == 1 ? 2 : 1;
            }
            while (index < 11)
            {
                RemoveProcess(++index, item.Value.HisData);// Remove the too closed point
                MergeLoop(++index, 1, item.Value.HisData);// Merge the 3 points Polyline
            }

            MergeFourPolyLine(++index, item.Value.HisData);// Merge the 4 points Polyline

            List<Candle> lifeCycle = item.Value.HisData.Last();
            item.Value.CyclePerYear = lifeCycle.Count / lookBackYear;
            int upCount = 0;
            int downCount = 0;
            double upValueCount = 0;
            double downValueCount = 0;
            int days = 0;
            for (int i = 0; i < lifeCycle.Count - 1; i++)
            {
                days += (lifeCycle[i + 1].DateTime - lifeCycle[i].DateTime).Days;
                if (lifeCycle[i].Close == 0)
                {
                    continue;
                }
                if (lifeCycle[i].Close < lifeCycle[i + 1].Close)
                {
                    upCount++;
                    upValueCount += (double)((lifeCycle[i + 1].Close - lifeCycle[i].Close) / lifeCycle[i].Close * 100);
                }
                else
                {
                    downCount++;
                    downValueCount += (double)((lifeCycle[i].Close - lifeCycle[i + 1].Close) / lifeCycle[i].Close * 100);
                }
            }
            item.Value.UpCycleCount = upCount;
            item.Value.DownCycleCount = downCount;
            item.Value.UpCycleAvgPercentage = Math.Round(upValueCount / upCount);
            item.Value.DownCycleAvgPercentage = Math.Round(downValueCount / downCount);
            item.Value.DaysPerCycle = days / (lifeCycle.Count - 1);
            item.Value.CycleAvgPercentage = Math.Round((upValueCount + downValueCount) / (upCount + downCount), 2);

        }

        private void MergeFourPolyLine(int index, List<List<Candle>> temp)
        {
            var avg = CalWeekAvg(index, temp);

            temp.Add(new List<Candle>());
            temp[index].Add(temp[index - 1][0]);
            for (int i = 1; i < temp[index - 1].Count - 2; i++)
            {
                decimal k1 = Math.Abs(temp[index - 1][i - 1].Close - temp[index - 1][i].Close);
                decimal k2 = Math.Abs(temp[index - 1][i].Close - temp[index - 1][i + 1].Close);
                decimal k3 = Math.Abs(temp[index - 1][i + 1].Close - temp[index - 1][i + 2].Close);
                if (k1 + k3 > 6 * k2)
                {
                    i++;
                    continue;
                }
                else
                {
                    temp[index].Add(temp[index - 1][i]);
                }
            }
            // Last point, special case
            int last = temp[index - 1].Count - 2;
            decimal k11 = Math.Abs(temp[index - 1][last - 2].Close - temp[index - 1][last - 1].Close);
            decimal k22 = Math.Abs(temp[index - 1][last - 1].Close - temp[index - 1][last].Close);
            decimal k33 = Math.Abs(temp[index - 1][last].Close - temp[index - 1][last + 1].Close);
            if (!(k11 + k33 > 5 * k22))
            {
                temp[index].Add(temp[index - 1][last]);
            }

            temp[index].Add(temp[index - 1][last + 1]);
        }

        private decimal CalWeekAvg(int index, List<List<Candle>> temp)// tried the origin one for week diff, not in a big enough granularity, change back to last processed
        {
            decimal avg = 0;
            for (int i = 0; i < temp[index - 1].Count - 1; i++)
            {
                avg += Math.Abs(temp[index - 1][i].Close - temp[index - 1][i + 1].Close);
            }
            avg /= (temp[index - 1].Count - 1);
            return avg;
        }

        private void RemoveProcess(int index, List<List<Candle>> temp)
        {
            var avg = CalWeekAvg(index, temp);

            temp.Add(new List<Candle>());
            temp[index].Add(temp[index - 1][0]);
            for (int i = 1; i < temp[index - 1].Count - 1; i++)
            {
                if (Math.Abs(temp[index - 1][i].Close - temp[index - 1][i + 1].Close) + Math.Abs(temp[index - 1][i].Close - temp[index - 1][i - 1].Close) < avg)
                {
                    continue;
                }
                else
                {
                    temp[index].Add(temp[index - 1][i]);
                }
            }
            temp[index].Add(temp[index - 1][temp[index - 1].Count - 1]);
        }

        private void MergeLoop(int index, int step, List<List<Candle>> item)
        {
            item.Add(new List<Candle>());
            item[index].Add(item[index - 1][0]);
            if (step == 2)
            {
                item[index].Add(item[index - 1][1]);
            }
            for (int i = step; i < item[index - 1].Count - step; i++)
            {
                if ((item[index - 1][i].Close - item[index - 1][i - step].Close) * (item[index - 1][i].Close - item[index - 1][i + step].Close) > 0)
                {
                    item[index].Add(item[index - 1][i]);
                }
            }
            item[index].Add(item[index - 1][item[index - 1].Count - 1]);
            if (step == 2)
            {
                item[index].Add(item[index - 1][item[index - 1].Count - 2]);
            }
            item[index] = item[index].OrderBy(x => x.DateTime).ToList();
        }

    }
}
