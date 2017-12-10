using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogee.AI.Derp {

    public class ActivityTrainingResult {
        public double convergence;
        public TimeSpan trainingTime;
        public TimeSpan calibratingTime;
        public int trainings;
        public int bestSeed;
        public double customIndicator;

        public override string ToString() {
            StringBuilder strbldr = new StringBuilder();
            strbldr.AppendLine("#--- Training Results ---#");
            strbldr.AppendLine("Convergence : " + convergence);
            strbldr.AppendLine("Custom Indicator : " + customIndicator);
            strbldr.AppendLine("Training Time : " + trainingTime);
            strbldr.AppendLine("Calibration Time : " + calibratingTime);
            strbldr.AppendLine("Trainings : " + trainings);
            strbldr.AppendLine("Best Seed : " + bestSeed);
            return strbldr.ToString();
        }
    }
}
