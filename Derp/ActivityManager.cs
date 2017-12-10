using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogee.AI.Derp {

    public static class ActivityManager {

        private static Dictionary<string, Activity> _Activities = new Dictionary<string, Activity>();
        public static Dictionary<string, Activity> Activities => _Activities;

        public static Activity CreateActivity(string activityName, int inputLength, int outputLength, params int[] hiddenLayersSizes) {
            if (_Activities.ContainsKey(activityName))
                throw new Exception("Activity \"" + activityName + "\" already exists.");
            Activity newActivity = new Activity(inputLength, outputLength, hiddenLayersSizes);
            _Activities.Add(activityName, newActivity);
            return newActivity;
        }
    }
}
