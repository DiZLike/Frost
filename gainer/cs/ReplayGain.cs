using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Un4seen.Bass;
using static Un4seen.Bass.Misc.BaseEncoder;

namespace gainer.cs
{
	public class ReplayGain
	{
		private int _sample_rate;
		private double _target_LUFS;
		public ReplayGain(int sample_rate, double target_lufs)
		{
			_sample_rate = sample_rate;
			_target_LUFS = target_lufs;
		}

		public double CalculateRaplayGainWithKFilter(float[] pcm_data32)
		{
			KFilter k_filter = new KFilter();
			pcm_data32 = k_filter.ApplyFilter(pcm_data32);
			double integtatedLoadness = CalculateIntegratedLoudness(pcm_data32, _sample_rate);
			double replay_gain = _target_LUFS - integtatedLoadness;
			Console.WriteLine($"\t[K-Filter] LUFS:\t\t{Math.Round(integtatedLoadness, 2)}\tdB");
			Console.WriteLine($"\t[K-Filter] Replay Gain:\t\t{Math.Round(replay_gain, 2)}\tdB");
			return Math.Round(replay_gain, 2);
		}
		public double CalculateRaplayGain(float[] pcm_data32)
		{
			double integtatedLoadness = CalculateIntegratedLoudness(pcm_data32, _sample_rate);
			double replay_gain = _target_LUFS - integtatedLoadness;
			Console.WriteLine($"\tLUFS:\t\t\t\t{Math.Round(integtatedLoadness, 2)}\tdB");
			Console.WriteLine($"\tReplay Gain:\t\t\t{Math.Round(replay_gain, 2)}\tdB");
			return Math.Round(replay_gain, 2);
		}


		private double CalculateIntegratedLoudness(float[] pcm_data32, int sampleRate)
		{
			int blockSize = sampleRate * 400 / 1000;
			int numBlock = pcm_data32.Length / blockSize;
			int cor_numBlock = 0;
			double sumLoudness = 0;
			for (int i = 0; i < numBlock; i++)
			{
				Console.Write($"\r\tCalculating LUFS [bloсk]:\t{i + 1}");
				float[] block = new ArraySegment<float>(pcm_data32, i * blockSize, blockSize).ToArray();
				double rms = CalculateRMS(block);
				double momentaryLoudness = -0.691 + 20 * Math.Log10(rms);
				if (momentaryLoudness <= -120)
					cor_numBlock++;
				else
					sumLoudness += Math.Pow(10, momentaryLoudness / 10);
			}
			Console.WriteLine();
			double integratedLoudness = -0.691 + 10 * Math.Log10(sumLoudness / (numBlock - cor_numBlock));
			return integratedLoudness;
		}

		private double CalculateRMS(float[] samples)
		{
			double sumOfSquares = 0;
			for (int i = 0; i < samples.Length; i++)
				sumOfSquares += samples[i] * samples[i];
			return Math.Sqrt(sumOfSquares / samples.Length);
		}
	}
}
