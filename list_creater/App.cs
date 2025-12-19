using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace list_creater
{
	internal static class App
	{
		internal static string[] ScanFolder(string folder)
		{
			return Directory.GetFiles(folder);
		}
	}
}
