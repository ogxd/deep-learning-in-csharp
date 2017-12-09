
using System.Collections.Generic;
using System;

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

    public class Activity {

        private const double COST_EXPONENT = 1;
        private const double QI_MAX = 160;
        // Guarantees same results on each run (nice for debugging)
        private const int RANDOM_SEED = 32145;

        private Random random = new Random(RANDOM_SEED);

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
        private const int BIAS = 1;

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
            weights = new double[neurons.Length - 1][][];
            for (int i = 0; i < weights.Length; i++) {
                weights[i] = new double[neurons[i + 1].Length][];
                for (int j = 0; j < neurons[i + 1].Length; j++) {
                    // Initialize Weights from Neurons at i to Neurons at i + 1
                    weights[i][j] = random.GetRandomArray(neurons[i].Length, -4d / neurons[i].Length, 4d / neurons[i].Length);
                }
            }
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
                        weights[i - 1][j][k] += neurons[i][j] * deltas[i][j];
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
            neurons[0][neurons[0].Length - 1] = BIAS;
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
                neurons[i + 1][neurons[i + 1].Length - 1] = BIAS;
            }
            return neurons[neurons.Length - 1];
        }


        private double _computeError(double[] result, double[] expected) {
            double error = 0;
            for (int i = 0; i < result.Length; i++) {
                error += Math.Pow(Math.Abs(expected[i] - result[i]), COST_EXPONENT);
            }
            error /= result.Length; // Mean
            _convergence = _previousError - error;
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