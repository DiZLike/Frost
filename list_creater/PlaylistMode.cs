using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace list_creater
{
	internal class PlaylistMode
	{
		private List<string> _tracks = new();
		internal PlaylistMode()
		{

		}
		public void AddTo(string list, string target_folder, string[] songs)
		{
			_tracks = File.ReadAllLines(list).ToList();
			foreach (var item in songs)
			{
				string file = Path.GetFileName(item);
				string format_file = $"track={target_folder}{file}?;";
				_tracks.Add(format_file);
			}
		}
		public void Save(string list)
		{
			File.WriteAllLines(list, _tracks.ToArray());
		}
	}
}
