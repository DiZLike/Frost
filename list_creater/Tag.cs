using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace list_creater
{
	internal class Tag
	{
		public string Artist { get; }
		public string Title { get; }
		internal Tag(string artist, string title)
		{
			Artist = artist;
			Title = title;
		}
		public override string ToString()
		{
			return $"{Artist} - {Title}";
		}
	}
}
