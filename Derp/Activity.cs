
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ogee.AI.Derp {

    public class Activity {

        public delegate void TrainingInputPicker(int inputIndex, out double[] input, out double[] output);

        private const double COST_EXPONENT = 1;
        private const double QI_MAX = 160;

        private double[][] neurons;
        private double[][][] weights;

        private double _convergence = 0;
        private double _previousError = 1;

        public double previousError => _previousError;

        public double convergence => _convergence;

        public double _correctionFactor = 1;
        public double correctionFactor => _correctionFactor;

        public bool isCorrectionFactorEvolutive = false;

        public double QI => QI_MAX * (_sigmoid(0.01 / _previousError));

        private const int SIGMOID_STEEPNESS = 1;
        public int bias = 1;

        public Activity(int inputLength, int outputLength, params int[] hiddenLayersLengths) {
            initializeNeuralNetwork(inputLength, outputLength, hiddenLayersLengths);
        }

        public void initializeNeuralNetwork(int inputLength, int outputLength, int[] hiddenLayersSizes) {
            //-- BIAISES --
            inputLength++;
            hiddenLayersSizes.Add(1);

            //-- NEURONS --
            // Number or columns of neurons
            neurons = new double[hiddenLayersSizes.Length + 2][];
            // Initialize Neurons for Inputs
            neurons[0] = new double[inputLength];
            neurons[neurons.Length - 1] = new double[outputLength];
            // Initialize Neurons for each Hidden Layer
            for (int i = 1; i < neurons.Length - 1; i++) {
                neurons[i] = new double[hiddenLayersSizes[i - 1]];
            }

            //-- WEIGHTS --
            initializeWeights(false);
        }

        /// <summary>
        /// Initialize all weights.
        /// Value used
        /// </summary>
        /// <param name="seed"></param>
        public void initializeWeights(bool randomWeights, int seed = 0) {
            weights = new double[neurons.Length - 1][][];
            if (randomWeights) {
                Random random = new Random(seed);
                for (int i = 0; i < weights.Length; i++) {
                    weights[i] = new double[neurons[i + 1].Length][];
                    for (int j = 0; j < neurons[i + 1].Length; j++) {
                        weights[i][j] = random.GetRandomArray(neurons[i].Length, -4d / neurons[i].Length, 4d / neurons[i].Length);
                    }
                }
            } else {
                for (int i = 0; i < weights.Length; i++) {
                    weights[i] = new double[neurons[i + 1].Length][];
                    for (int j = 0; j < neurons[i + 1].Length; j++) {
                        weights[i][j] = Extensions.GetFilledArray(neurons[i].Length, 0d);
                    }
                }
            }
        }

        const double E = 0.00001;

        public bool isConvergent(double[] input, double[] output, int iterations = 100) {
            for (int i = 0; i < iterations; i++) {
                train(input, output);
            }
            return (convergence < 1 + E && convergence > 1 - E);
        }

        const int SEED = 123456;

        private double _customIndicator = 0;

        public ActivityTrainingResult think(TrainingInputPicker getTrainingInput, TimeSpan timeOut, int startPoints = 1000) {

            double[] input;
            double[] output;

            // Calibrating the seed
            // This operation is necessary to better optimize the starting point.
            // Without this, weights at initialization might make the model converge towards a bad solution.
            Stopwatch swCalibration = Stopwatch.StartNew();
            int s = 0;
            int trainingsPerCalibration = 100;
            Dictionary<int, double> seeds = new Dictionary<int, double>();
            Random seedGenerator = new Random(SEED);
            for (int i = 0; i < startPoints; i++) {
                int seed = seedGenerator.Next(int.MinValue, int.MaxValue);
                if (seeds.ContainsKey(seed))
                    continue;
                for (int j = 0; j < trainingsPerCalibration; j++) {
                    initializeWeights(true, seed);
                    getTrainingInput(j, out input, out output); // Picking a training data set
                    train(input, output); // Training (this is not the actual training, weights are changed afterwards)
                    if (j % (trainingsPerCalibration / 10) == 0) {
                        if (_convergence < 1 + E && _convergence > 1 - E) {
                            seeds.Add(seed, previousError); // Add the error for this seed
                            goto startPointChecked;
                        }
                    }
                }
                seeds.Add(seed, double.MaxValue);
                startPointChecked:;
            }
            int bestSeed = 0;
            double min = double.MaxValue;
            foreach (KeyValuePair<int, double> pair in seeds) {
                if (pair.Value < min) {
                    min = pair.Value;
                    bestSeed = pair.Key;
                }
            }
            initializeWeights(true, bestSeed); // Initialize the weights for this better start point
            swCalibration.Stop();

            // Training
            // Will stop learning if it converges already or if it went timeout 
            Stopwatch swTraining = Stopwatch.StartNew();
            int trainings = 0;
            double maxMs = timeOut.TotalMilliseconds;
            while (true) {
                getTrainingInput(trainings, out input, out output); // Picking a training data set
                train(input, output); // Training
                if (trainings % 100 == 0) {
                    if (_convergence < 1 + E && _convergence > 1 - E) {
                        break;
                    } else if (swTraining.ElapsedMilliseconds > maxMs) {
                        break;
                    }
                }
                trainings++;
            }
            swTraining.Stop();

            return new ActivityTrainingResult() {
                convergence = _convergence,
                trainingTime = TimeSpan.FromMilliseconds(swTraining.ElapsedMilliseconds),
                calibratingTime = TimeSpan.FromMilliseconds(swCalibration.ElapsedMilliseconds),
                trainings = trainings,
                bestSeed = bestSeed,
                customIndicator = _customIndicator,
            };
        }

        private int _trainings = 0;
        public void train(double[] input, double[] output) {
            //-- PROPAGATION --
            // Compute the output for the actual state of the Neural Network
            double[] result = guess(input);
            // Compute the total Error
            _computeError(result, output);

            if (isCorrectionFactorEvolutive && _trainings > 0) {
                _correctionFactor = 0.01 / _convergence; 
            }

            //-- BACKPROPAGATION --
            // Initialize Errors. First row is useless. It is kept for simpler code later.
            double[][] errors = new double[neurons.Length][];
            for (int i = 0; i < errors.Length; i++) {
                errors[i] = new double[neurons[i].Length];
            }
            // Fill with Errors at the end
            for (int i = 0; i < result.Length; i++) {
                errors[errors.Length - 1][i] = correctionFactor * Math.Pow(output[i] - result[i], COST_EXPONENT);
            }
            // Initialize Deltas
            double[][] deltas = new double[neurons.Length][];
            for (int i = 0; i < deltas.Length; i++) {
                deltas[i] = new double[neurons[i].Length];
            }
            // Compute all deltas and errors
            for (int i = neurons.Length - 1; i > 0 ; i--) {
                // Compute Delta at i
                for (int j = 0; j < neurons[i].Length; j++) {
                    deltas[i][j] = errors[i][j] * _dsigmoid(neurons[i][j]);
                }
                // Compute Error at i - 1
                if (i > 0) {
                    for (int j = 0; j < neurons[i].Length; j++) {
                        for (int k = 0; k < weights[i - 1][j].Length; k++) {
                            errors[i - 1][k] += deltas[i][j] * weights[i - 1][j][k];
                        }
                    }
                }
            }
            // Adjust Weights
            for (int i = neurons.Length - 1; i > 0; i--) {
                for (int j = 0; j < neurons[i].Length; j++) {
                    for (int k = 0; k < weights[i - 1][j].Length; k++) {
                        weights[i - 1][j][k] += neurons[i - 1][k] * deltas[i][j];
                    }
                }
            }

            _trainings++;
        }

        public double[] guess(double[] inputs) {
            // Inject input as first Neurons
            for (int i = 0; i <inputs.Length; i++) {
                neurons[0][i] = inputs[i];
            }
            // Set first Bias
            neurons[0][neurons[0].Length - 1] = bias;
            // Propagate Neurons
            for (int i = 0; i < weights.Length; i++) {
                for (int j = 0; j < weights[i].Length; j++) {
                    double value = 0;
                    for (int k = 0; k < weights[i][j].Length; k++) {
                        value += weights[i][j][k] * neurons[i][k];
                    }
                    neurons[i + 1][j] = _sigmoid(value);
                }
                // Set subsequent Bias
                if (i < neurons.Length - 2)
                    neurons[i + 1][neurons[i + 1].Length - 1] = bias;
            }
            return neurons[neurons.Length - 1];
        }


        private double _computeError(double[] result, double[] expected) {
            double error = 0;
            for (int i = 0; i < result.Length; i++) {
                error += Math.Pow(Math.Abs(expected[i] - result[i]), COST_EXPONENT);
            }
            error /= result.Length; // Mean
            _convergence = error / _previousError;
            _customIndicator = _customIndicator / 2 + _convergence - 1;
            _previousError = error;
            return error;
        }

        private double _sigmoid(double x) {
            return 1 / (1 + Math.Exp(-x));
        }

        private double _dsigmoid(double x) {
            return x * (1 - x);
        }
    }
}