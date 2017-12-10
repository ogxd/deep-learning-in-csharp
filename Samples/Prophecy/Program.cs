using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ogee.AI.Derp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

class Program {

    static void Main(string[] args) {

        DailyBitcoinAngler angler = new DailyBitcoinAngler();

        Activity activity = ActivityManager.CreateActivity("Prophecy", angler.daysToLearn, angler.daysToPredict);
        activity.bias = 1;

        var trainingResults = activity.think(angler.getTrainingData, TimeSpan.FromSeconds(10), 1000);

        Console.WriteLine(trainingResults);
        Console.WriteLine("Error : " + activity.previousError);

        double[] recent = angler.bitcoinIndexes.Skip(angler.bitcoinIndexes.Length - angler.daysToLearn).Take(angler.daysToLearn).ToArray();
        double[] results = activity.guess(recent);

        results.Multiply(1d / angler.normalizationFactor);
        double[] tenLastDays = angler.bitcoinIndexes.Skip(angler.bitcoinIndexes.Length - angler.daysToPredict).Take(angler.daysToPredict).ToArray();
        tenLastDays.Multiply(1d / angler.normalizationFactor);

        Dictionary<DateTime, double> dateToIndexPrediction = new Dictionary<DateTime, double>();

        int day = 0;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        foreach (double result in tenLastDays) {
            dateToIndexPrediction.Add(angler.startOfPrediction.AddDays(-angler.daysToPredict + day++ - 1), result);
            Console.WriteLine("BTC Value @ {0} : {1}", angler.startOfPrediction.AddDays(-angler.daysToPredict + day++).ToString("yyyy-MM-dd"), result);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        day = 0;
        foreach (double result in results) {
            dateToIndexPrediction.Add(angler.startOfPrediction.AddDays(day++), result);
            Console.WriteLine("BTC Value @ {0} : {1}", angler.startOfPrediction.AddDays(day++).ToString("yyyy-MM-dd"), result);
        }

        using (FileStream fs = new FileStream(@"result.xls", FileMode.Create)) {
            using (StreamWriter sw = new StreamWriter(fs)) {
                foreach (KeyValuePair<DateTime, double> pair in dateToIndexPrediction) {
                    sw.WriteLine(pair.Key.ToString("dd/MM/yyyy") + "\t" + pair.Value);
                }
            }
        }

        Console.ReadKey();
    }

    public class DailyBitcoinAngler {

        public double[] bitcoinIndexes;
        public double normalizationFactor;
        public DateTime startOfPrediction;

        // Parameters
        public int daysToLearn = 100;
        public int daysToPredict = 10;

        public DailyBitcoinAngler() {
            startOfPrediction = DateTime.Now.AddDays(-1);
            pull();
        }

        public void pull() {

            // Pull Bitcoin Information
            WebClient client = new WebClient();
            string downloadString = client.DownloadString(@"https://api.coindesk.com/v1/bpi/historical/close.json?start=2013-09-01&end=" + startOfPrediction.ToString("yyyy-MM-dd"));
            JObject xml = JsonConvert.DeserializeObject<JObject>(downloadString);

            List<double> bitcoinIndexesList = new List<double>(512);
            double max = double.MinValue;
            foreach (JProperty a in xml.First.First) {
                //DateTime time = DateTime.Parse(a.Name);
                double value = a.First.Value<float>();
                if (value > max)
                    max = value;
                bitcoinIndexesList.Add(value);
            }
            double[] bitcoinIndexesPerDay = bitcoinIndexesList.ToArray();

            max *= 2; // Necessary ? to be tested without
            normalizationFactor = 1d / max;
            bitcoinIndexesPerDay.Multiply(normalizationFactor);

            bitcoinIndexes = bitcoinIndexesPerDay;
        }

        public void getTrainingData(int index, out double[] input, out double[] output) {
            Random random = new Random(index);
            int sessionPos = random.Next(daysToLearn, bitcoinIndexes.Length - daysToPredict);
            input = bitcoinIndexes.Skip(sessionPos - daysToLearn).Take(daysToLearn).ToArray();
            output = bitcoinIndexes.Skip(sessionPos).Take(daysToPredict).ToArray();
        }
    }
}

