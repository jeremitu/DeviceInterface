﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LabNation.Common;

namespace LabNation.DeviceInterface.Devices
{
    public enum AnalogWaveForm { SINE, COSINE, SQUARE, SAWTOOTH, TRIANGLE, SAWTOOTH_SINE, MULTISINE,
#if DEBUG 
        HALF_BIG_HALF_UGLY 
#endif
    };
    public enum DigitalWaveForm { Counter, OneHot, Marquee, Pulse };

    partial class DummyScope
    {
        public static float[] GenerateWave(uint waveLength, double samplePeriod, double timeOffset, DummyScopeChannelConfig config )
        {
            AnalogWaveForm waveForm = config.waveform;
            double frequency = config.frequency;
            double amplitude = config.amplitude;
            double phase = config.phase;
            double dutyCycle = config.dutyCycle;
            double dcOffset = config.dcOffset;

            float[] wave = new float[waveLength];
            switch(waveForm) {
                case AnalogWaveForm.SINE:
                    wave = DummyScope.WaveSine(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase);
                    break;
                case AnalogWaveForm.COSINE:
                    wave = DummyScope.WaveCosine(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase);
                    break;
                case AnalogWaveForm.SQUARE:
                    wave = DummyScope.WaveSquare(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase, dutyCycle);
                    break;
                case AnalogWaveForm.SAWTOOTH:
                    wave = DummyScope.WaveSawTooth(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase);
                    break;
                case AnalogWaveForm.TRIANGLE:
                    wave = DummyScope.WaveTriangle(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase);
                    break;
                case AnalogWaveForm.SAWTOOTH_SINE:
                    wave = DummyScope.WaveSawtoothSine(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase);
                    break;
                case AnalogWaveForm.MULTISINE:
                    wave = DummyScope.WaveMultiCosine(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase, new float[] {1, 2, 4, 8 });
                    break;
#if DEBUG
                case AnalogWaveForm.HALF_BIG_HALF_UGLY:
                    wave = DummyScope.WaveHalfBigHalfUgly(waveLength, samplePeriod, timeOffset, frequency, amplitude, phase);
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
            Func<float, float> offsetAdder = x => (float)(x + dcOffset);
            wave = Utils.TransformArray(wave, offsetAdder);
            return wave;
        }

        public static byte[] GenerateWaveDigital(uint waveLength, double samplePeriod, double timeOffset)
        {
            byte[] output = new byte[waveLength];
            for (int i = 0; i < waveLength; i++)
            {
                output[i] = (byte)(i / 10);
            }
            return output;
        }

        public static T[] CropWave<T>(uint outputLength, T[] sourceWave, int triggerIndex, int holdoff)
        {
            T[] output = new T[outputLength];

            if (triggerIndex - holdoff >= 0)
                if (triggerIndex - holdoff + outputLength <= sourceWave.Length)
                    Array.Copy(sourceWave, triggerIndex - holdoff, output, 0, outputLength);

            return output;
        }

        public static float[] WaveSine(uint nSamples, double samplePeriod, double timeOffset, double frequency, double amplitude, double phase)
        {
            float[] wave = new float[nSamples];
            for (int i = 0; i < wave.Length; i++)
                wave[i] = (float)(amplitude * Math.Sin(2.0 * Math.PI * frequency * ((double)i * samplePeriod + timeOffset) + phase));
            return wave;
        }

        public static float[] WaveCosine(uint nSamples, double samplePeriod, double timeOffset, double frequency, double amplitude, double phase)
        {
            return WaveSine(nSamples, samplePeriod, timeOffset, frequency, amplitude, phase + Math.PI / 2);
        }

        public static float[] WaveMultiCosine(uint awgPoints, double awgSamplePeriod, double timeOffset, double frequency, double amplitude, double phase, float[] harmonics)
        {
            return WaveMultiCosine(awgPoints, awgSamplePeriod, timeOffset, harmonics.Select(x => frequency * x).ToArray(), amplitude, phase);
        }

        public static float[] WaveMultiCosine(uint awgPoints, double awgSamplePeriod, double timeOffset, double[] frequencies, double amplitude, double phase)
        {
            float scaler = frequencies.Length;
            float[] result = new float[awgPoints];
            Func<float, float, float> Sum = (x, y) => x + y;
            foreach (float freq in frequencies)
                result = Utils.CombineArrays(result, WaveCosine(awgPoints, awgSamplePeriod, timeOffset, freq, amplitude / scaler, 0), Sum);
            return result;
        }

        public static float[] WaveSquare(uint nSamples, double samplePeriod, double timeOffset, double frequency, double amplitude, double phase, double dutyCycle = 0.5)
        {
            float[] wave = new float[nSamples];
            for (int i = 0; i < wave.Length; i++)
                wave[i] = (((double)i * samplePeriod + timeOffset + (phase / 2.0 / Math.PI / frequency)) % (1.0 / frequency)) * frequency < dutyCycle ? (float)amplitude : -1f * (float)amplitude;
            return wave;
        }
        public static float[] WaveSawTooth(uint nSamples, double samplePeriod, double timeOffset, double frequency, double amplitude, double phase)
        {
            float[] wave = new float[nSamples];
            for (int i = 0; i < wave.Length; i++)
                wave[i] = (float)((((double)i * samplePeriod + timeOffset + (phase / 2.0 / Math.PI / frequency)) % (1.0 / frequency)) * frequency * amplitude);
            return wave;
        }
        public static float[] WaveTriangle(uint nSamples, double samplePeriod, double timeOffset, double frequency, double amplitude, double phase)
        {
            float[] wave = new float[nSamples];
            for (int i = 0; i < wave.Length; i++)
            {
                //Number between 0 and 1 indicating which part of the period we're in
                double periodSection = ((i * samplePeriod + timeOffset) * frequency + (phase / 2.0 / Math.PI)) % 1.0;
                double scaler = periodSection < 1f/2 ? (periodSection - 1f/4) * 4 : (periodSection - 3f/4) * -4;
                wave[i] = (float)(scaler * amplitude);
            }
            return wave;
        }

        public static float[] WaveSawtoothSine(uint nSamples, double samplePeriod, double timeOffset, double frequency, double amplitude, double phase)
        {
            Func<float, float, float> sumFloat = (x, y) => (x + y);
            float[] wave1 = WaveSawTooth(nSamples, samplePeriod, timeOffset, frequency, amplitude, phase);
            float[] wave2 = WaveSine(nSamples, samplePeriod, timeOffset, frequency * 7.0, amplitude * 0.1, phase);
            float[] wave = Utils.CombineArrays(wave1, wave2, sumFloat);
            return wave;
        }
        
        public static float[] WaveHalfBigHalfUgly(uint awgPoints, double awgSamplePeriod, double timeOffset, double frequency, double amplitude, double phase)
        {
            float[] wave1 = DummyScope.WaveSine(awgPoints, awgSamplePeriod, timeOffset, frequency * 21f, amplitude, phase + 0 * 168);
            float[] wave2 = DummyScope.WaveSine(awgPoints, awgSamplePeriod, timeOffset, frequency * 21f, amplitude * 0.4f, phase + 0 * 168);

            float[] wave3 = DummyScope.WaveSawTooth(awgPoints, awgSamplePeriod, timeOffset, 50f, amplitude * 1f, phase + 0);
            float[] wave4 = DummyScope.WaveSawTooth(awgPoints, awgSamplePeriod, timeOffset, 50f, amplitude * 0.4f, phase + 0);

            float[] wave5 = DummyScope.WaveSquare(awgPoints, awgSamplePeriod, timeOffset, frequency * 31, amplitude, phase + 0 * 912);
            float[] wave6 = DummyScope.WaveSquare(awgPoints, awgSamplePeriod, timeOffset, frequency * 31f, amplitude * 0.5f, phase + 0 * 912);

            float[] wave7 = DummyScope.WaveSquare(awgPoints, awgSamplePeriod, timeOffset, frequency * 60, amplitude, phase + 0 * 912);
            float[] wave8 = DummyScope.WaveSquare(awgPoints, awgSamplePeriod, timeOffset, frequency * 60f, amplitude * 0.5f, phase + 0 * 912);

            float[] wave9 = DummyScope.WaveSine(awgPoints, awgSamplePeriod, timeOffset, frequency * 800f, amplitude, 0 * 168);
            float[] wave10 = DummyScope.WaveSine(awgPoints, awgSamplePeriod, timeOffset, frequency * 800f, amplitude * 0.4f, phase + 0 * 168);

            float[] finalWave = new float[wave1.Length];

            for (int i = 0; i < wave1.Length; i++)
            {
                if (i < 1 * 1600)
                    finalWave[i] = wave1[i];
                else if (i < 2 * 1600)
                    finalWave[i] = wave2[i];
                else if (i < 3 * 1600)
                    finalWave[i] = wave3[i];
                else if (i < 4 * 1600)
                    finalWave[i] = wave4[i];
                else if (i < 5 * 1600)
                    finalWave[i] = wave5[i];
                else if (i < 6 * 1600)
                    finalWave[i] = wave6[i];
                else if (i < 7 * 1600)
                    finalWave[i] = wave7[i];
                else if (i < 8 * 1600)
                    finalWave[i] = wave8[i];
                else if (i < 9 * 1600)
                    finalWave[i] = wave9[i];
                else if (i < 10 * 1600)
                    finalWave[i] = wave10[i];
            }

            return finalWave;
        }

        public static byte[] WaveCounter(byte min, byte max)
        {
            byte[] counter = new byte[max - min + 1];
            byte count = min;
            for (int i = 0; i < counter.Length; i++)
                counter[i] = count++;
            return counter;
        }

        public static byte[] WaveOneHot(int numberOfBits)
        {
            if (numberOfBits < 1 || numberOfBits > 8)
                throw new Exception("Number of bits out of range for one-hot digital sequence");

            byte[] counter = new byte[numberOfBits];
            for (int i = 0; i < counter.Length; i++)
                counter[i] = (byte)(1 << i);
            return counter;
        }

        public static byte[] WaveMarquee(int numberOfBits)
        {
            if (numberOfBits < 1 || numberOfBits > 8)
                throw new Exception("Number of bits out of range for one-hot digital sequence");

            int mask = (int)Math.Pow(2, numberOfBits) - 1;
            int full = mask << numberOfBits;
            byte[] counter = new byte[numberOfBits * 2];
            for (int i = 0; i < counter.Length; i++)
                counter[i] = (byte)((full >> (numberOfBits * 2 - i)) & mask);
            return counter;
        }

		public static byte[] WavePulse(double dutyCycle)
		{
			byte[] pulse = new byte[100];
			int highUntilIndex = (int)Math.Floor (dutyCycle * pulse.Length);
			for (int i = 0; i < highUntilIndex; i++)
				pulse [i] = 0xff;
			return pulse;
		}
        
        public bool Ready { get { return true; } }

        private static void AddNoise(float[] output, double noiseAmplitude)
        {
            Random r = new Random();
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (float)(output[i] + (r.NextDouble() - 0.5) * noiseAmplitude);
            }

        }
        
    }
}
