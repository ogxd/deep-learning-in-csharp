using Ogee.AI.Moulpu;
using System;
using System.Diagnostics;

class Program {

    static void Main(string[] args) {

        double[] input = new double[] { 0.156, 0.4548, 0, 1, 0.674, 0.14048 };
        double[] output = new double[] { 0.9544, 0.7845 };

        Stopwatch sw = Stopwatch.StartNew();

        Activity activity = ActivityManager.CreateActivity("Test", 6, 2, 4);

        for (int i = 0; i < 128; i++) {
            activity.train(input, output);
            Console.WriteLine("Convergence : {0}", activity.convergence);
            Console.WriteLine("QI : {0}", activity.QI);
        }

        Console.WriteLine("Done in {0} ms", sw.ElapsedMilliseconds);
        Console.ReadKey();
    }
}

