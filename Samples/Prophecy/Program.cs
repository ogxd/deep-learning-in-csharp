using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ogee.AI.Derp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

class Program {

    static void Main(string[] args) {

        // Parameters
        int daysToLearn = 100;
        int daysToPredict = 10;
        DateTime startOfPrediction = DateTime.Now.AddDays(-1);

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

        max *= 2;

        bitcoinIndexesPerDay.Multiply(1 / max);

        Random rnd = new Random();

        Activity activity = ActivityManager.CreateActivity("Prophecy", daysToLearn, daysToPredict, 31);
        activity.bias = 1;

        int trainings = 1000000;
        int percents = 0;
        double mod = (1d * trainings / 100d);

        for (int i = 0; i < trainings; i++) {
            int sessionPos = bitcoinIndexesPerDay.Length - daysToPredict; // To check convergence
            //int sessionPos = rnd.Next(daysToLearn, bitcoinIndexesPerDay.Length - daysToPredict);
            double[] input = bitcoinIndexesPerDay.Skip(sessionPos - daysToLearn).Take(daysToLearn).ToArray();
            double[] output = bitcoinIndexesPerDay.Skip(sessionPos).Take(daysToPredict).ToArray();
            activity.train(input, output);
            if (i % mod == 0) {
                Console.WriteLine("Progress : {0}%", percents);
                Console.WriteLine("Error : {0}", activity.previousError);
                Console.WriteLine("Convergence : {0}", activity.convergence);
                percents++;
            }
        }

        double[] recent = bitcoinIndexesPerDay.Skip(bitcoinIndexesPerDay.Length - daysToLearn).Take(daysToLearn).ToArray();
        double[] results = activity.guess(recent);
        results.Multiply(max);

        foreach (double result in results) {
            Console.WriteLine("BTC Value @ {0} : {1}", startOfPrediction.AddDays(1).ToString("yyyy-MM-dd"), result);
        }

        Console.ReadKey();
    }
}

