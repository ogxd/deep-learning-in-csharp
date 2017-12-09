using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogee.AI.Derp {

    public class NeuronsRowBadLengthException : Exception {
        public NeuronsRowBadLengthException() : base("Neurons length is wrong.") { }
    }



}
