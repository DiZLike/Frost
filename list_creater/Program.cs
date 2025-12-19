namespace list_creater
{
    internal class Program
    {
        //private static string[] _new_tracks = null!;
        //private static List<string> _tracks = new();
        static void Main(string[] args)
        {
			//args = new string[] { @ "-a", "E:\Desktop\test_dir", "/mnt/sd/radio/main/",  @"E:\Desktop\test.pls" }; // -a
			//args = new string[] { "-s", @"C:\Users\Evgeny\Desktop\s_t", "/mnt/sd/radio/main/", "up6jlo4bj6e8yy96w6w3iq84", "http://pub.dlike.ru/add-track", "http://rpi.dlike.ru:82/download/main/" }; // -s

			string mode = args[0];              // Режим работы
            if (mode == "-a")                   // Добавление в плейлист
            {
                string tracks_path = args[1];
                string server_path = args[2];
                string playlist = args[3];

				string[] songs = App.ScanFolder(tracks_path);
                PlaylistMode list = new();
				list.AddTo(playlist, server_path, songs);
				list.Save(playlist);
			}
            else if (mode == "-s")              // Добавление в базу на сервер
            {
				string tracks_path = args[1];
				string server_path = args[2];
                string server_key = args[3];
                string server_uri = args[4];
                string download_link = args[5];

                ServerMode server = new(tracks_path, server_key, server_uri, server_path, download_link);
			}
            Console.ReadKey();
        }

		// --------------------------------------------------------------------------------------------------------------------------------


		
	}
}
