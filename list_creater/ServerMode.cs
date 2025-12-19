using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace list_creater
{
	internal class ServerMode
	{
		private List<string> _t = new List<string>();
		internal ServerMode(string songs_path, string key, string page, string server_path, string download_link)
		{
			string[] songs = App.ScanFolder(songs_path);
			foreach (var song in songs)
			{
				Console.WriteLine($"Adding a song: {Path.GetFileName(song)}");
				Tag tag = GetTag(song);
				Console.WriteLine($"\t\tArtist: {tag.Artist}");
				Console.WriteLine($"\t\tTitle: {tag.Title}");
				SendSong(song, tag, key, page, server_path, download_link);
				Console.WriteLine();
				File.WriteAllLines("159.txt", _t.ToArray());
			}
		}

		private Tag GetTag(string song)
		{
			if (!File.Exists(song))
				return null!;
			string title = String.Empty;
			string artist = String.Empty;
			TagLib.File audio = TagLib.File.Create(song);
			if (!string.IsNullOrEmpty(audio.Tag.FirstPerformer))
				artist = audio.Tag.FirstPerformer;
			else artist = "Unknown";
			if (!string.IsNullOrEmpty(audio.Tag.Title))
				title = audio.Tag.Title;
			else title = "Unknown";
			if (artist == "Unknown" && title == "Unknown")
				title = Path.GetFileNameWithoutExtension(song);
			return new Tag(artist, title);
		}
		private void SendSong(string song, Tag tag, string key, string page, string server_path, string download_link)
		{
			try
			{
				string new_path = CreateServerPath(song, server_path);
				string new_link = CreateDownloadLink(song, download_link);
				Console.WriteLine($"\t\tPath: {new_path}");
				Console.WriteLine($"\t\tLink: {new_link}");
				
				string result = Http.SendData(page, $"key={key}",
					$"artist={Uri.EscapeDataString(tag.Artist)}",
					$"title={Uri.EscapeDataString(tag.Title)}",
					$"path={new_path}",
					$"link={new_link}");
				Console.WriteLine($"\t\tResult: {result}");

				/*
				string t1 = Uri.EscapeDataString(tag.Artist);
				string t2 = Uri.EscapeDataString(tag.Title);
				string t3 = Path.GetFileName(song);
				string t4 = Uri.EscapeDataString(t3);
				if (tag.Artist != t1 || tag.Title != t2 || t3 != t4)
				{
					_t.Add($"Artist\t=\t{tag.Artist}");
					_t.Add($"NArtist\t=\t{t1}");
					_t.Add($"Title\t=\t{tag.Title}");
					_t.Add($"NTitle\t=\t{t2}");
					_t.Add($"File\t=\t{t3}");
					_t.Add($"NFile\t=\t{t4}");
				}*/

			}
			catch (Exception ex)
			{
				Thread.Sleep(3000);
				SendSong(song, tag, key, page, server_path, download_link);
			}
		}
		private string CreateServerPath(string song, string server_path)
		{
			string file = Path.GetFileName(song);
			return $"{server_path}{file}";
		}
		private string CreateDownloadLink(string song, string download_link)
		{
			string file = Path.GetFileName(song);
			return $"{download_link}{file}";
		}
	}
}
