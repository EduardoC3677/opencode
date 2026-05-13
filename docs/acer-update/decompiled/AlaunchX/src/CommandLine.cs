using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AlaunchX;

public class CommandLine
{
	public int ExitCode;

	public List<string> CommandList;

	public List<string> OutputList;

	public List<string> ErrorList;

	private void OutputDataHandler(object process, DataReceivedEventArgs outputData)
	{
		if (outputData.Data != null)
		{
			OutputList.Add(outputData.Data);
			Console.WriteLine(outputData.Data);
			Utility.Logger(LogLevel.Trace, "\tOutput: " + outputData.Data);
		}
	}

	private void ErrorDataHandler(object process, DataReceivedEventArgs ErrorData)
	{
		if (ErrorData.Data != null)
		{
			ErrorList.Add(ErrorData.Data);
			Console.WriteLine("Error: " + ErrorData.Data);
			Utility.Logger(LogLevel.Error, "\tError: " + ErrorData.Data);
		}
	}

	public CommandLine(string command, bool redirectStandardError = true)
		: this(new List<string> { command }, redirectStandardError)
	{
	}

	public CommandLine(List<string> commandList, bool redirectError = true)
	{
		CommandList = commandList;
		OutputList = new List<string>();
		ErrorList = new List<string>();
		ExitCode = 0;
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = (File.Exists("C:\\Windows\\Sysnative\\cmd.exe") ? "C:\\Windows\\Sysnative\\cmd.exe" : "C:\\Windows\\System32\\cmd.exe"),
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = redirectError,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			Process process = new Process
			{
				StartInfo = startInfo
			};
			process.Start();
			foreach (string command in CommandList)
			{
				Utility.Logger(LogLevel.Trace, "\tCommand: " + command);
				process.StandardInput.WriteLine(command);
			}
			process.StandardInput.WriteLine("exit");
			process.OutputDataReceived += OutputDataHandler;
			process.BeginOutputReadLine();
			if (redirectError)
			{
				process.ErrorDataReceived += ErrorDataHandler;
				process.BeginErrorReadLine();
			}
			process.WaitForExit();
			ExitCode = process.ExitCode;
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (ExitCode == 0)
		{
			Utility.Logger(LogLevel.Trace, "\tCommandLine ExitCode=" + ExitCode);
		}
		else
		{
			Utility.Logger(LogLevel.Warn, "\tCommandLine ExitCode=" + ExitCode);
		}
	}

	public override string ToString()
	{
		string empty = string.Empty;
		empty = "[CommandList]\r\n";
		foreach (string command in CommandList)
		{
			empty = empty + command + "\r\n";
		}
		empty += "[OutputList]\r\n";
		foreach (string output in OutputList)
		{
			empty = empty + output + "\r\n";
		}
		empty += "[ErrorList]\r\n";
		foreach (string error in ErrorList)
		{
			empty = empty + error + "\r\n";
		}
		return empty + "[ExitCode]\r\n" + ExitCode + "\r\n";
	}
}
