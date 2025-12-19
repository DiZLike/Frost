using gainer.cs;

namespace gainer
{
	internal class Program
	{
		private static bool _k_filter = false;
		private static bool _custom = false;
		private static double _target = -23;
		private static string _folder = String.Empty;


		// gainer -gain n/k -tag n/c -target -23 folder
		static void Main(string[] args)
		{/*
			// -------------------------------------------------------------
			args = new string[7];
			args[0] = "-gain";
			args[1] = "k";
			args[2] = "-tag";
			args[3] = "c";
			args[4] = "-target";
			args[5] = "-23";
			args[6] = "E:\\Desktop\\new";
			// -------------------------------------------------------------*/
			if (args.Length < 7)
				return;

			_k_filter = GetKFilter(args);
			_custom = GetTagType(args);
			_target = GetTarget(args);
			_folder = GetPath(args);

			string[] files = GetFiles(_folder);
			App.CheckOS();

			AudioDevice device = new AudioDevice();
			Console.WriteLine($"Directory: {_folder}");
			foreach (string file in files)
			{
				if (!File.Exists(file)) continue;
				Console.WriteLine($"\tSong: {Path.GetFileName(file)}");
				double replay_gain = 0;
				AudioReader audio = new AudioReader(file);
				ReplayGain gain = new ReplayGain(44100, _target);
				float[] pcm_data = audio.GetPCMData32();
				if (_k_filter)
					replay_gain = gain.CalculateRaplayGainWithKFilter(pcm_data);
				else
					replay_gain = gain.CalculateRaplayGain(pcm_data);
				audio.SaveReplayGain(replay_gain, _custom);
				Console.WriteLine();
			}
			Console.ReadKey();
		}
		private static string[] GetFiles(string path)
		{
			return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
		}
		private static bool GetKFilter(string[] args)
		{
			if (args[1] == "k") return true;
			return false;
		}
		private static bool GetTagType(string[] args)
		{
			if (args[3] == "c") return true;
			return false;
		}
		private static double GetTarget(string[] args)
		{
			return args[5].ToDouble();
		}
		private static string GetPath(string[] args)
		{
			return args[6];
		}
	}
}