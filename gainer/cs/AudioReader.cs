using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Un4seen.Bass;
using static Un4seen.Bass.Misc.BaseEncoder;

namespace gainer.cs
{
	public class AudioReader
	{
		private int _sample_rate = 44100;
		private int _channels = 2;
		private int _stream = 0;
		private Tags _tags = null!;
		public AudioReader(string audio_file) 
		{
			_tags = new Tags(audio_file);
			LoadAudio(audio_file);
		}
		private bool LoadAudio(string audio_file)
		{
			_stream = Bass.BASS_StreamCreateFile(audio_file, 0, 0, BASSFlag.BASS_STREAM_DECODE);
			Console.WriteLine($"Stream: {_stream}");
			return _stream > 0;
		}
		public short[] GetPCMData16()
		{
			List<short> pcmData = new List<short>();
			short[] buffer = new short[_sample_rate * _channels];
			int samplesRead;
			while ((samplesRead = Bass.BASS_ChannelGetData(_stream, buffer, buffer.Length)) > 0)
			{
				pcmData.AddRange(buffer);
				Console.Write($"\r\tReading PCM Data. Samples:\t{pcmData.Count}");
			}
			ClearStream();
			Console.WriteLine();
			return pcmData.ToArray();
		}
		public float[] GetPCMData32()
		{
			return PCM16to32(GetPCMData16());
		}
		public void SaveReplayGain(double gain, bool custom)
		{
			_tags.SaveRaplayGain(gain, custom);
		}
		private float[] PCM16to32(short[] pcmData16)
		{
			float[] floatSamples = new float[pcmData16.Length];
			for (int i = 0; i < pcmData16.Length; i++)
			{
				floatSamples[i] = pcmData16[i] / 32768.0f;
				if (i % 5000 == 0 || i == pcmData16.Length - 1)
					Console.Write($"\r\tConverting PCM Data. Samples:\t{i + 1}");
			}
			Console.WriteLine();
			return floatSamples;
		}
		private void ClearStream()
		{
			Bass.BASS_StreamFree(_stream);
		}
	}
}
