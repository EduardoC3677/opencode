using System.Diagnostics;
using System.IO;

namespace LaunchALaunchX;

internal class Program
{
	private static int Main(string[] args)
	{
		string text = "C:\\OEM\\Preload\\Command\\AlaunchX\\ALaunchX.exe";
		if (!File.Exists(text))
		{
			return -2;
		}
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = text,
			Arguments = "/User",
			UseShellExecute = false,
			CreateNoWindow = true
		};
		if (new Process
		{
			StartInfo = startInfo
		}.Start())
		{
			return 0;
		}
		return -1;
	}
}
