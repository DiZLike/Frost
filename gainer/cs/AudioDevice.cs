using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace gainer.cs
{
	public class AudioDevice
	{
		private List<bool> _plagins_ok = new List<bool>();
		public AudioDevice()
		{ 
			if (!AudioDeviceInit())
				Environment.Exit(0xFF);
		}
		private bool AudioDeviceInit()
		{
			bool ok = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
			_plagins_ok.Add(Bass.BASS_PluginLoad(Path.Combine(App.app_dir, App.pref_lib + "bassopus" + App.dot_lib)) > 0);
			_plagins_ok.Add(Bass.BASS_PluginLoad(Path.Combine(App.app_dir, App.pref_lib + "bass_aac" + App.dot_lib)) > 0);
			return ok;
		}
	}
}
