using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gainer.cs
{
	public class Tags
	{
		private readonly string _file = String.Empty;
		public Tags(string file) 
		{
			_file = file;
		}
		public void SaveRaplayGain(double gain, bool custom)
		{
			TagLib.File file = TagLib.File.Create(_file);
			if (custom)
				file.Tag.Comment = $"replay-gain={gain}";
			else
				file.Tag.ReplayGainTrackGain = gain;
			file.Save();
		}
	}
}
