using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TagLib.Id3v2;
using Un4seen.Bass;

namespace gainer.cs
{
	public static class App
	{
		public static string OS = String.Empty;
		public static string Arc = String.Empty;


		public static string linux_arm = AppDomain.CurrentDomain.BaseDirectory;
		public static string linux_arm64 = AppDomain.CurrentDomain.BaseDirectory;
		public static string linux_x86 = AppDomain.CurrentDomain.BaseDirectory;
		public static string linux_x64 = AppDomain.CurrentDomain.BaseDirectory;
		public static string windows_x86 = AppDomain.CurrentDomain.BaseDirectory;
		public static string windows_x64 = AppDomain.CurrentDomain.BaseDirectory;
		public static string pref_lib = String.Empty;
		public static string dot_lib = ".dll";
		public static string app_dir = AppDomain.CurrentDomain.BaseDirectory;

		public static string current_os_folder = String.Empty;

		public static Dictionary<string, string> os_sel_folder = new Dictionary<string, string>();

		public static bool is_error = false;
		private static readonly Dictionary<BASSError, string> errors = new()
		{
			{ BASSError.BASS_OK ,  "All is OK " },
			{ BASSError.BASS_ERROR_MEM ,  "Memory error" },
			{ BASSError.BASS_ERROR_FILEOPEN ,  "Can't open the file" },
			{ BASSError.BASS_ERROR_DRIVER ,  "Can't find a free/valid driver" },
			{ BASSError.BASS_ERROR_BUFLOST ,  "The sample buffer was lost" },
			{ BASSError.BASS_ERROR_HANDLE ,  "Invalid handle" },
			{ BASSError.BASS_ERROR_FORMAT ,  "Unsupported sample format" },
			{ BASSError.BASS_ERROR_POSITION ,  "Invalid playback position" },
			{ BASSError.BASS_ERROR_INIT ,  "BASS_Init has not been successfully called" },
			{ BASSError.BASS_ERROR_START ,  "BASS_Start has not been successfully called" },
			{ BASSError.BASS_ERROR_SSL ,  "SSL/HTTPS support isn't available" },
			{ BASSError.BASS_ERROR_REINIT ,  "Device needs to be reinitialized" },
			{ BASSError.BASS_ERROR_NOCD ,  "No CD in drive" },
			{ BASSError.BASS_ERROR_CDTRACK ,  "Invalid track number" },
			{ BASSError.BASS_ERROR_ALREADY ,  "Already initialized/paused/whatever" },
			{ BASSError.BASS_ERROR_NOPAUSE ,  "Not paused" },
			{ BASSError.BASS_ERROR_NOTAUDIO ,  "Not an audio track" },
			{ BASSError.BASS_ERROR_NOCHAN ,  "Can't get a free channel" },
			{ BASSError.BASS_ERROR_ILLTYPE ,  "An illegal type was specified" },
			{ BASSError.BASS_ERROR_ILLPARAM ,  "An illegal parameter was specified" },
			{ BASSError.BASS_ERROR_NO3D ,  "No 3D support" },
			{ BASSError.BASS_ERROR_NOEAX ,  "No EAX support" },
			{ BASSError.BASS_ERROR_DEVICE ,  "Illegal device number" },
			{ BASSError.BASS_ERROR_NOPLAY ,  "Not playing" },
			{ BASSError.BASS_ERROR_FREQ ,  "Illegal sample rate" },
			{ BASSError.BASS_ERROR_NOTFILE ,  "The stream is not a file stream" },
			{ BASSError.BASS_ERROR_NOHW ,  "No hardware voices available" },
			{ BASSError.BASS_ERROR_EMPTY ,  "The MOD music has no sequence data" },
			{ BASSError.BASS_ERROR_NONET ,  "No internet connection could be opened" },
			{ BASSError.BASS_ERROR_CREATE ,  "Couldn't create the file" },
			{ BASSError.BASS_ERROR_NOFX ,  "Effects are not available" },
			{ BASSError.BASS_ERROR_PLAYING ,  "The channel is playing" },
			{ BASSError.BASS_ERROR_NOTAVAIL ,  "Requested data is not available" },
			{ BASSError.BASS_ERROR_DECODE ,  "The channel is a 'decoding channel'" },
			{ BASSError.BASS_ERROR_DX ,  "A sufficient DirectX version is not installed" },
			{ BASSError.BASS_ERROR_TIMEOUT ,  "Connection timedout" },
			{ BASSError.BASS_ERROR_FILEFORM ,  "Unsupported file format" },
			{ BASSError.BASS_ERROR_SPEAKER ,  "Unavailable speaker" },
			{ BASSError.BASS_ERROR_VERSION ,  "Invalid BASS version (used by add-ons)" },
			{ BASSError.BASS_ERROR_CODEC ,  "Codec is not available/supported" },
			{ BASSError.BASS_ERROR_ENDED ,  "The channel/file has ended" },
			{ BASSError.BASS_ERROR_BUSY ,  "The device is busy (eg. in \"exclusive\" use by another process)" },
			{ BASSError.BASS_ERROR_UNSTREAMABLE ,  "Unstreamable file" },
			{ BASSError.BASS_ERROR_PROTOCOL ,  "Unsupported protocol" },
			{ BASSError.BASS_ERROR_DENIED ,  "Access denied" },
			{ BASSError.BASS_ERROR_UNKNOWN ,  "Some other mystery error" },
			{ BASSError.BASS_ERROR_WMA_LICENSE ,  "BassWma: the file is protected" },
			{ BASSError.BASS_ERROR_WMA_WM9 ,  "BassWma: WM9 is required" },
			{ BASSError.BASS_ERROR_WMA_DENIED ,  "BassWma: access denied (user/pass is invalid)" },
			{ BASSError.BASS_ERROR_WMA_CODEC ,  "BassWma: no appropriate codec is installed" },
			{ BASSError.BASS_ERROR_WMA_INDIVIDUAL ,  "BassWma: individualization is needed" },
			{ BASSError.BASS_ERROR_ACM_CANCEL ,  "BassEnc: ACM codec selection cancelled" },
			{ BASSError.BASS_ERROR_CAST_DENIED ,  "BassEnc: Access denied (invalid password)" },
			{ BASSError.BASS_ERROR_SERVER_CERT ,  "Missing/invalid certificate" },
			{ BASSError.BASS_VST_ERROR_NOINPUTS ,  "BassVst: the given effect has no inputs and is probably a VST instrument and no effect" },
			{ BASSError.BASS_VST_ERROR_NOOUTPUTS ,  "BassVst: the given effect has no outputs" },
			{ BASSError.BASS_VST_ERROR_NOREALTIME ,  "BassVst: the given effect does not support realtime processing" },
			{ BASSError.BASS_ERROR_WASAPI ,  "BASSWASAPI: no WASAPI available" },
			{ BASSError.BASS_ERROR_WASAPI_BUFFER ,  "BASSWASAPI: buffer size is invalid" },
			{ BASSError.BASS_ERROR_WASAPI_CATEGORY ,  "BASSWASAPI: can't set category" },
			{ BASSError.BASS_ERROR_MP4_NOSTREAM ,  "BASS_AAC: non-streamable due to MP4 atom order ('mdat' before 'moov')" },
			{ BASSError.BASS_ERROR_MIDI_INCLUDE ,  "BASSMIDI: SFZ include file could not be opened" },
			{ BASSError.BASS_ERROR_WEBM_NOTAUDIO ,  "BASSWEBM: non-streamable WebM audio" },
			{ BASSError.BASS_ERROR_WEBM_TRACK ,  "BASSWEBM: invalid track number" }
		};

		public static string test_opus_file = @"C:\Users\Evgeny\Desktop\Axiom Verge Cover1.opus";
		static App()
		{
			linux_arm = Path.Combine(linux_arm, "bass_dll", "linux", "armhf");
			linux_arm64 = Path.Combine(linux_arm64, "bass_dll", "linux", "aarch64");
			linux_x86 = Path.Combine(linux_x86, "bass_dll", "linux", "x86");
			linux_x64 = Path.Combine(linux_x64, "bass_dll", "linux", "x86_64");

			windows_x86 = Path.Combine(windows_x86, "bass_dll", "win", "x86");
			windows_x64 = Path.Combine(windows_x64, "bass_dll", "win", "x64");

			os_sel_folder.Add("windowsx86", windows_x86);
			os_sel_folder.Add("windowsx64", windows_x64);

			os_sel_folder.Add("linuxx86", linux_x86);
			os_sel_folder.Add("linuxx64", linux_x64);
			os_sel_folder.Add("linuxarm", linux_arm);
			os_sel_folder.Add("linuxarm64", linux_arm64);
		}

		public static void IsError()
		{
			if (is_error)
			{
				var error = GetError();
				//Console.ReadKey();
				if (error != BASSError.BASS_ERROR_HANDLE)
					Environment.Exit(0xFF);
			}
		}
		public static BASSError GetError()
		{
			BASSError error = Bass.BASS_ErrorGetCode();
			Console.WriteLine("Error: " + errors[error]);
			//Helper.Println("pr_cl");
			return error;
		}
		public static string GetErrorMessage()
		{
			BASSError error = Bass.BASS_ErrorGetCode();
			return errors[error];
		}
		public static void CheckOS()
		{
			bool is_win = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			Architecture arc = RuntimeInformation.ProcessArchitecture;
			if (is_win)
			{
				App.OS = "Windows";
				App.pref_lib = String.Empty;
				App.dot_lib = ".dll";
			}
			else
			{
				App.OS = "Linux";
				App.pref_lib = "lib";
				App.dot_lib = ".so";
			}
			App.Arc = arc.ToString();

			Console.WriteLine(App.OS);
			Console.WriteLine(App.Arc);
			Console.WriteLine();
		}
		public static int ToInt(this string value)
		{
			return int.Parse(value);
		}
		public static double ToDouble(this string value)
		{
			return double.Parse(value);
		}
	}
}