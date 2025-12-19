using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace list_creater
{
	internal static class Http
	{
		public static string SendData(string page, string key, params string[] par)
		{
			HttpClient client = new();
			string url = $"{page}?{key}";
			foreach (var param in par)
				url += $"&{param}";
			HttpResponseMessage response = UseGET(client, url);
			if (response.IsSuccessStatusCode)
			{
				string response_data = response.Content.ReadAsStringAsync().Result;
			//	Console.WriteLine($"MySRV: {response_data}");
				return response_data;
			}
			//else
			//	Console.WriteLine($"MySRV: {response.StatusCode}");
			return response.StatusCode.ToString();
		}
		private static HttpResponseMessage UseGET(HttpClient client, string url)
		{
			return client.GetAsync(url).Result;
		}
	}
}
