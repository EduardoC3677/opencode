using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace AlaunchX;

public static class Utility
{
	private struct KBDLLHOOKSTRUCT
	{
		public int vkCode;

		private int scanCode;

		public int flags;

		private int time;

		private int dwExtraInfo;
	}

	private delegate int LowLevelProcDelegate(int nCode, int wParam, IntPtr lParam);

	public enum EXECUTION_STATE : uint
	{
		ES_SYSTEM_REQUIRED = 1u,
		ES_DISPLAY_REQUIRED = 2u,
		ES_CONTINUOUS = 0x80000000u
	}

	private static object _toLockLogFile = new object();

	public static string CurrentLogPath = Data.CurrentProcessFolder + "\\AlaunchX.log";

	private const int LOGPIXELSX = 88;

	private const int LOGPIXELSY = 90;

	private static LowLevelProcDelegate keyBoardProc;

	private static LowLevelProcDelegate mouseProc;

	private static IntPtr hMouseHook = IntPtr.Zero;

	private static IntPtr hKeyboardHook = IntPtr.Zero;

	private const int WH_MOUSE_LL = 14;

	private const int WH_KEYBOARD_LL = 13;

	private const int WM_KEYDOWN = 256;

	private const int WM_SYSKEYDOWN = 260;

	private static bool _isLockKeyboardMouse = false;

	public static BackgroundWorker BackgroundWorker_MonitorLockKeyBoardMouse = null;

	private static uint _eventPermissions_EVENT_ALL_ACCESS = 2031619u;

	public static BackgroundWorker BackgroundWorker_MonitorShutdownEvent = null;

	[DllImport("kernel32")]
	private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

	[DllImport("kernel32")]
	private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

	public static string GetIniKeyValue(string filePath, string section, string key, string defaultValue = "")
	{
		StringBuilder stringBuilder = null;
		try
		{
			stringBuilder = new StringBuilder(255);
			GetPrivateProfileString(section, key, "", stringBuilder, 255, filePath);
			return (stringBuilder.Length > 0) ? stringBuilder.ToString() : defaultValue;
		}
		catch
		{
			return string.Empty;
		}
	}

	public static void SetIniKeyValue(string filePath, string section, string key, string IN_Value)
	{
		try
		{
			WritePrivateProfileString(section, key, IN_Value, filePath);
		}
		catch (Exception)
		{
		}
	}

	public static void Logger(LogLevel loglevel, string logdescription)
	{
		string text = "UnknownMethod";
		StackTrace stackTrace = new StackTrace(fNeedFileInfo: false);
		StackFrame stackFrame = null;
		stackFrame = stackTrace.GetFrame(1);
		if (stackFrame != null)
		{
			text = stackFrame.GetMethod().DeclaringType.Name + "." + stackFrame.GetMethod().Name + "()";
		}
		lock (_toLockLogFile)
		{
			using StreamWriter streamWriter = new StreamWriter(CurrentLogPath, append: true);
			streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}" + "\t[" + loglevel.ToString() + "]\t[" + text + "]\t" + logdescription);
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr ptr);

	[DllImport("gdi32.dll")]
	private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

	[DllImport("user32.dll")]
	private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

	public static void GetDPI(out double dpiX_Ratio, out double dpiY_Ratio)
	{
		IntPtr dC = GetDC(IntPtr.Zero);
		int deviceCaps = GetDeviceCaps(dC, 88);
		int deviceCaps2 = GetDeviceCaps(dC, 90);
		Logger(LogLevel.Info, "DPI_X: " + deviceCaps + ", DPI_Y: " + deviceCaps2);
		deviceCaps = (((double)deviceCaps <= 96.0) ? 96 : (((double)deviceCaps <= 120.0 && (double)deviceCaps > 96.0) ? 120 : ((!((double)deviceCaps <= 144.0) || !((double)deviceCaps > 120.0)) ? 192 : 144)));
		dpiX_Ratio = deviceCaps / 96;
		deviceCaps2 = (((double)deviceCaps2 <= 96.0) ? 96 : (((double)deviceCaps2 <= 120.0 && (double)deviceCaps2 > 96.0) ? 120 : ((!((double)deviceCaps2 <= 144.0) || !((double)deviceCaps2 > 120.0)) ? 192 : 144)));
		dpiY_Ratio = deviceCaps2 / 96;
		ReleaseDC(IntPtr.Zero, dC);
	}

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProcDelegate lpfn, IntPtr hMod, int dwThreadId);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(IntPtr hHook);

	[DllImport("user32.dll")]
	private static extern int CallNextHookEx(IntPtr hHook, int nCode, int wParam, IntPtr lParam);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetModuleHandle(string moduleName);

	private static int LowLevelKeyboardProc(int nCode, int wParam, IntPtr lParam)
	{
		if (nCode >= 0)
		{
			if (wParam == 256 || wParam == 260)
			{
				KBDLLHOOKSTRUCT kBDLLHOOKSTRUCT = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
				if ((kBDLLHOOKSTRUCT.flags & 0x20) != 0 && kBDLLHOOKSTRUCT.vkCode == 36)
				{
					_isLockKeyboardMouse = false;
				}
			}
			if (!_isLockKeyboardMouse)
			{
				return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
			}
			return 1;
		}
		return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
	}

	private static int LowLevelMouseProc(int nCode, int wParam, IntPtr lParam)
	{
		if (nCode >= 0)
		{
			if (!_isLockKeyboardMouse)
			{
				return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
			}
			return 1;
		}
		return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
	}

	private static void backgroundWorker_MonitorLockKeyBoardMouse(object sender, DoWorkEventArgs e)
	{
		while (_isLockKeyboardMouse)
		{
			Thread.Sleep(100);
		}
		Logger(LogLevel.Info, "Alt + home is pressed");
		((DispatcherObject)Application.Current).Dispatcher.Invoke((DispatcherPriority)9, (Delegate)(Action)delegate
		{
			RemoveKeyBoardMouseHookEvent();
		});
	}

	public static void LockKeyBoardMouse()
	{
		_isLockKeyboardMouse = true;
		keyBoardProc = LowLevelKeyboardProc;
		mouseProc = LowLevelMouseProc;
		using (Process process = Process.GetCurrentProcess())
		{
			hKeyboardHook = SetWindowsHookEx(13, keyBoardProc, GetModuleHandle(process.MainModule.ModuleName), 0);
			hMouseHook = SetWindowsHookEx(14, mouseProc, GetModuleHandle(process.MainModule.ModuleName), 0);
		}
		BackgroundWorker_MonitorLockKeyBoardMouse = new BackgroundWorker();
		BackgroundWorker_MonitorLockKeyBoardMouse.DoWork += backgroundWorker_MonitorLockKeyBoardMouse;
		BackgroundWorker_MonitorLockKeyBoardMouse.RunWorkerAsync();
	}

	public static void RemoveKeyBoardMouseHookEvent()
	{
		if (!_isLockKeyboardMouse)
		{
			_ = hKeyboardHook;
			UnhookWindowsHookEx(hKeyboardHook);
			_ = hMouseHook;
			UnhookWindowsHookEx(hMouseHook);
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

	public static void DisableS3()
	{
		Logger(LogLevel.Info, "Success to disable S3. (Previous state: " + SetThreadExecutionState((EXECUTION_STATE)2147483651u).ToString() + ")");
	}

	public static void EnableS3()
	{
		Logger(LogLevel.Info, "Success to enable S3. (Previous state: " + SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS).ToString() + ")");
	}

	[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);

	private static void backgroundWorker_MonitorShutdownEvent(object sender, DoWorkEventArgs e)
	{
		while (true)
		{
			try
			{
				if (OpenEvent(_eventPermissions_EVENT_ALL_ACCESS, bInheritHandle: false, "ACERREBOOT_SHUTDOWN_EVENT") != IntPtr.Zero)
				{
					break;
				}
			}
			catch (Exception ex)
			{
				Logger(LogLevel.Info, ex.ToString());
			}
		}
		Logger(LogLevel.Info, "Get shutdown event from AcerReboot");
		if (Data.CurrentWorkThread != null && Data.CurrentWorkThread.IsAlive)
		{
			Logger(LogLevel.Info, "Thread is still working, abort the working thread");
			Data.CurrentWorkThread.Abort();
			if (Data.MainControl != null)
			{
				Data.MainControl.TriggerReboot();
			}
			else
			{
				Reboot();
			}
		}
	}

	public static void MonitorShutdownEvent()
	{
		BackgroundWorker_MonitorShutdownEvent = new BackgroundWorker();
		BackgroundWorker_MonitorShutdownEvent.DoWork += backgroundWorker_MonitorShutdownEvent;
		BackgroundWorker_MonitorShutdownEvent.RunWorkerAsync();
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

	public static void GetArchitecture(ref Architecture processAchitecture, ref Architecture osArchitecture)
	{
		if (IsWow64Process2(Process.GetCurrentProcess().Handle, out var processMachine, out var nativeMachine))
		{
			Logger(LogLevel.Info, "processMachine: 0x" + processMachine.ToString("X") + ", nativeMachine: 0x" + nativeMachine.ToString("X"));
			switch (nativeMachine)
			{
			case 332:
				osArchitecture = Architecture.x86;
				break;
			case 34404:
				osArchitecture = Architecture.AMD64;
				break;
			case 43620:
				osArchitecture = Architecture.ARM64;
				break;
			default:
				osArchitecture = Architecture.Unknown;
				break;
			}
			switch (RuntimeInformation.ProcessArchitecture.ToString().ToUpper(new CultureInfo("en-US")))
			{
			case "ARM":
				processAchitecture = Architecture.ARM;
				break;
			case "X86":
				processAchitecture = Architecture.x86;
				break;
			case "X64":
				processAchitecture = Architecture.AMD64;
				break;
			case "ARM64":
				processAchitecture = Architecture.ARM64;
				break;
			default:
				processAchitecture = Architecture.Unknown;
				break;
			}
		}
	}

	public static bool CopyFile(string from, string to)
	{
		if (!Directory.Exists(Path.GetDirectoryName(to)))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(to));
		}
		if (new CommandLine("copy \"" + from + "\" \"" + to + "\" /y").ExitCode != 0)
		{
			return false;
		}
		return true;
	}

	public static bool CopyFolder(string from, string to)
	{
		CommandLine commandLine = new CommandLine("robocopy \"" + from + "\" \"" + to + "\" /e /a-:R /NP");
		if (commandLine.ExitCode == 8 || commandLine.ExitCode == 16)
		{
			return false;
		}
		return true;
	}

	public static bool DeleteFile(string path)
	{
		if (new CommandLine("del /q /f \"" + path + "\"").ExitCode == 0 && !File.Exists(path))
		{
			return true;
		}
		return false;
	}

	public static bool DeleteFolder(string path)
	{
		CommandLine commandLine = new CommandLine("rd /q /s \"" + path + "\" || rem");
		if (commandLine.ExitCode != 0 || Directory.Exists(path))
		{
			foreach (string error in commandLine.ErrorList)
			{
				if (error.Contains("Access is denied"))
				{
					string[] array = error.Split(new string[1] { " - " }, StringSplitOptions.RemoveEmptyEntries);
					Logger(LogLevel.Trace, "Use icacls to get permission of " + array[0]);
					new CommandLine("icacls \"" + array[0] + "\" /reset");
				}
			}
			Thread.Sleep(2000);
			commandLine = new CommandLine("rd /q /s \"" + path + "\" || rem");
		}
		if (commandLine.ExitCode == 0 && !Directory.Exists(path))
		{
			return true;
		}
		return false;
	}

	public static bool IsResourceExist(string resourceUri)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string[] manifestResourceNames = executingAssembly.GetManifestResourceNames();
		foreach (string name in manifestResourceNames)
		{
			using Stream stream = executingAssembly.GetManifestResourceStream(name);
			using ResourceReader source = new ResourceReader(stream);
			string[] array = (from DictionaryEntry entry in source
				select (string)entry.Key).ToArray();
			foreach (string b in array)
			{
				if (string.Equals(resourceUri, b, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsAfterOOBE()
	{
		string iniKeyValue = GetIniKeyValue("C:\\Windows\\Setup\\State\\State.ini", "State", "ImageState");
		Logger(LogLevel.Info, "Image state is: " + iniKeyValue);
		if (string.Equals(iniKeyValue, "IMAGE_STATE_COMPLETE", StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

	public static void ProcessModulesInstallation()
	{
		string text = string.Empty;
		switch (Data.CurrentInstallPeriod)
		{
		case InstallPeriod.AuditAlaunch:
			text = Data.CurrentProcessFolder + "\\AuditAlaunchX.ini";
			break;
		case InstallPeriod.BeforeOOBE:
			text = Data.CurrentProcessFolder + "\\BeforeOOBE.ini";
			break;
		case InstallPeriod.UserAlaunch:
			text = Data.CurrentProcessFolder + "\\UserAlaunchX.ini";
			break;
		case InstallPeriod.FirstBoot:
			text = Data.CurrentProcessFolder + "\\FirstBoot.ini";
			break;
		}
		Logger(LogLevel.Info, "Read answer file: " + text);
		AlaunchXAnswerFile alaunchXAnswerFile = new AlaunchXAnswerFile(text);
		if (alaunchXAnswerFile.Done == 0 && (Data.CurrentInstallPeriod == InstallPeriod.AuditAlaunch || (Data.CurrentInstallPeriod == InstallPeriod.UserAlaunch && !Data.AfterOOBE)))
		{
			RefreshAnswerFile(alaunchXAnswerFile);
		}
		string text2 = Data.CurrentProcessFolder + "\\ProductInfo.enc";
		Data.ProductInfo = new XmlDefinition.ProductInformation(text2);
		if (!File.Exists(text2))
		{
			Data.ProductInfo.PrdInfo.GetMainInfo();
			Data.ProductInfo.SaveToFile();
		}
		for (int i = alaunchXAnswerFile.Done; i < alaunchXAnswerFile.TotalAction; i++)
		{
			Logger(LogLevel.Info, "---------------------------------------------------------------------------");
			if (i == alaunchXAnswerFile.TotalAction - 1 && (Data.CurrentInstallPeriod == InstallPeriod.AuditAlaunch || (Data.CurrentInstallPeriod == InstallPeriod.UserAlaunch && !Data.AfterOOBE)))
			{
				Logger(LogLevel.Info, "Before installing the last module, delete Run registry");
				DeleteRunRegistry();
				Logger(LogLevel.Info, "Before installing the last module, clean module source");
				CleanModuleSource(alaunchXAnswerFile);
				Logger(LogLevel.Info, "Before installing the last module, refresh product info");
				Data.ProductInfo.PrdInfo.GetMainInfo();
				Data.ProductInfo.SaveToFile();
			}
			Logger(LogLevel.Info, "Now processing [Action" + (i + 1) + "]");
			if (Data.MainControl != null)
			{
				Data.MainControl.UpdateUIforModuleInfo(alaunchXAnswerFile.TotalAction, i + 1, "");
			}
			if (File.Exists("C:\\OEM\\Preload\\ModuleDebug\\BeforeModule.cmd"))
			{
				int exitcode = -2;
				if (RunCmdWithoutOutputRedirection("C:\\OEM\\Preload\\ModuleDebug\\BeforeModule.cmd", string.Empty, ref exitcode))
				{
					Logger(LogLevel.Info, "ExitCode is: " + exitcode);
				}
				else
				{
					Logger(LogLevel.Error, "Fail to execute this process");
				}
			}
			InstallSingleModule(alaunchXAnswerFile, i);
			if (File.Exists("C:\\OEM\\Preload\\ModuleDebug\\AfterModule.cmd"))
			{
				int exitcode2 = -2;
				if (RunCmdWithoutOutputRedirection("C:\\OEM\\Preload\\ModuleDebug\\AfterModule.cmd", string.Empty, ref exitcode2))
				{
					Logger(LogLevel.Info, "ExitCode is: " + exitcode2);
				}
				else
				{
					Logger(LogLevel.Error, "Fail to execute this process");
				}
			}
			if (string.Equals(GetIniKeyValue(text, "Action" + (i + 1), "Delete"), "True", StringComparison.OrdinalIgnoreCase))
			{
				Logger(LogLevel.Info, "Delete " + alaunchXAnswerFile.Actions[i].Path + " for Delete=True");
				if (Directory.Exists(alaunchXAnswerFile.Actions[i].Path))
				{
					if (DeleteFolder(alaunchXAnswerFile.Actions[i].Path))
					{
						Logger(LogLevel.Info, "Delete " + alaunchXAnswerFile.Actions[i].Path + " successfully");
					}
					else
					{
						Logger(LogLevel.Error, "Failed to delete " + alaunchXAnswerFile.Actions[i].Path);
					}
				}
			}
			Logger(LogLevel.Info, "Write Done=" + (i + 1) + " into " + alaunchXAnswerFile.FilePath);
			SetIniKeyValue(alaunchXAnswerFile.FilePath, "Main", "Done", (i + 1).ToString());
			if (string.Equals(GetIniKeyValue(text, "Action" + (i + 1), "MustReboot"), "True", StringComparison.OrdinalIgnoreCase))
			{
				Logger(LogLevel.Info, "Reboot for MustReboot=True");
				if (Data.MainControl != null)
				{
					Data.MainControl.TriggerReboot();
				}
				else
				{
					Reboot();
				}
				return;
			}
			if (string.Equals(GetIniKeyValue(text, "Action" + (i + 1), "Reboot"), "True", StringComparison.OrdinalIgnoreCase))
			{
				Logger(LogLevel.Info, "Reboot for Reboot=True");
				if (Data.MainControl != null)
				{
					Data.MainControl.TriggerReboot();
				}
				else
				{
					Reboot();
				}
				return;
			}
		}
		Logger(LogLevel.Info, "All modules are processed");
		((DispatcherObject)Application.Current).Dispatcher.Invoke((DispatcherPriority)9, (Delegate)(Action)delegate
		{
			if (Data.CurrentInstallPeriod == InstallPeriod.AuditAlaunch || (Data.CurrentInstallPeriod == InstallPeriod.UserAlaunch && !Data.AfterOOBE))
			{
				EnableS3();
			}
			Environment.Exit(0);
		});
	}

	public static void RefreshAnswerFile(AlaunchXAnswerFile alaunchXAnswerFile)
	{
		Logger(LogLevel.Info, "Refresh answer file to set MustReboot=True for the last hotfix and driver");
		int num = alaunchXAnswerFile.Actions.FindLastIndex((AlaunchXAnswerFile.Action x) => string.Equals(x.ModulePN.Substring(5, 1), "H", StringComparison.OrdinalIgnoreCase));
		if (num >= 0)
		{
			Logger(LogLevel.Info, "Action" + (num + 1) + " " + alaunchXAnswerFile.Actions[num].ModulePN + " is the last hotfix, set MustReboot=True");
			SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (num + 1), "MustReboot", "True");
		}
		int num2 = alaunchXAnswerFile.Actions.FindLastIndex((AlaunchXAnswerFile.Action x) => string.Equals(x.ModulePN.Substring(5, 1), "D", StringComparison.OrdinalIgnoreCase));
		if (num2 >= 0)
		{
			Logger(LogLevel.Info, "Action" + (num2 + 1) + " " + alaunchXAnswerFile.Actions[num2].ModulePN + " is the last driver, set MustReboot=True");
			SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (num2 + 1), "MustReboot", "True");
		}
	}

	private static void DeleteRunRegistry()
	{
		string text = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
		string text2 = "New Acer AlaunchX";
		string text3 = "SwitchToDesktop";
		if (Data.CurrentInstallPeriod == InstallPeriod.AuditAlaunch)
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(text, writable: true);
			if (registryKey == null)
			{
				Logger(LogLevel.Error, "Fail to open regstry key: HKLM\\" + text);
			}
			else
			{
				try
				{
					registryKey.DeleteValue(text2);
				}
				catch (Exception ex)
				{
					Logger(LogLevel.Error, "Catch exception: " + ex.Message);
					Logger(LogLevel.Error, "Fail to delete \"" + text2 + "\" in HKLM\\" + text);
				}
			}
		}
		if (Data.CurrentInstallPeriod != InstallPeriod.UserAlaunch || Data.AfterOOBE)
		{
			return;
		}
		using RegistryKey registryKey2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(text, writable: true);
		if (registryKey2 == null)
		{
			Logger(LogLevel.Error, "Fail to open regstry key: HKLM\\" + text);
			return;
		}
		try
		{
			registryKey2.DeleteValue(text2);
		}
		catch (Exception ex2)
		{
			Logger(LogLevel.Error, "Catch exception: " + ex2.Message);
			Logger(LogLevel.Error, "Fail to delete \"" + text2 + "\" in HKLM\\" + text);
		}
		try
		{
			registryKey2.DeleteValue(text3);
		}
		catch (Exception ex3)
		{
			Logger(LogLevel.Error, "Catch exception: " + ex3.Message);
			Logger(LogLevel.Error, "Fail to delete \"" + text3 + "\" in HKLM\\" + text);
		}
	}

	private static void CleanModuleSource(AlaunchXAnswerFile alaunchXAnswerFile)
	{
		for (int i = 0; i < alaunchXAnswerFile.TotalAction - 1; i++)
		{
			if (!string.Equals(GetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (i + 1), "Delete"), "True", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			Logger(LogLevel.Info, "Delete=True in Action" + (i + 1) + " " + alaunchXAnswerFile.Actions[i].ModulePN + ", delete " + alaunchXAnswerFile.Actions[i].Path);
			if (Directory.Exists(alaunchXAnswerFile.Actions[i].Path))
			{
				if (DeleteFolder(alaunchXAnswerFile.Actions[i].Path))
				{
					Logger(LogLevel.Info, "Delete " + alaunchXAnswerFile.Actions[i].Path + " successfully");
				}
				else
				{
					Logger(LogLevel.Error, "Failed to delete " + alaunchXAnswerFile.Actions[i].Path);
				}
			}
		}
	}

	private static bool RunCmdWithoutOutputRedirection(string file, string arguments, ref int exitcode)
	{
		string text = "\"" + file + (string.IsNullOrEmpty(arguments) ? "\"" : ("\" " + arguments));
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = (File.Exists("C:\\Windows\\Sysnative\\cmd.exe") ? "C:\\Windows\\Sysnative\\cmd.exe" : "C:\\Windows\\System32\\cmd.exe"),
			Arguments = "/c \"" + text + "\"",
			UseShellExecute = false,
			CreateNoWindow = true
		};
		Process process = new Process
		{
			StartInfo = startInfo
		};
		Logger(LogLevel.Info, "*** Execute " + text);
		for (int i = 0; i < 5; i++)
		{
			if (process.Start())
			{
				process.WaitForExit();
				exitcode = process.ExitCode;
				return true;
			}
			Logger(LogLevel.Warn, "Fail to start this process, retry");
		}
		return false;
	}

	private static void InstallSingleModule(AlaunchXAnswerFile alaunchXAnswerFile, int actionIndex)
	{
		try
		{
			AlaunchXAnswerFile.Action action = alaunchXAnswerFile.Actions[actionIndex];
			Logger(LogLevel.Info, "Module PN: " + action.ModulePN);
			Logger(LogLevel.Info, "Module path: " + action.Path);
			string iniKeyValue = GetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Result", string.Empty);
			if (string.IsNullOrEmpty(iniKeyValue))
			{
				Logger(LogLevel.Info, "Set Result=-1 before installation");
				SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Result", "-1");
			}
			else if (!string.Equals(iniKeyValue, "-1", StringComparison.OrdinalIgnoreCase))
			{
				Logger(LogLevel.Warn, "Result=" + iniKeyValue + ", there might be some error for Action" + (actionIndex + 1));
			}
			XmlDefinition.ModuleEnc moduleEnc = null;
			string text = action.Path + "\\" + action.ModulePN + ".enc";
			if (!File.Exists(text))
			{
				Logger(LogLevel.Error, text + " not exists");
				return;
			}
			moduleEnc = new XmlDefinition.ModuleEnc(text);
			if (moduleEnc.ModuleInfo != null)
			{
				Logger(LogLevel.Info, "Read " + text + " successfully");
				string name = moduleEnc.ModuleInfo.Module.Name;
				Logger(LogLevel.Info, "Module name: " + name);
				if (Data.MainControl != null)
				{
					Data.MainControl.UpdateUIforModuleInfo(alaunchXAnswerFile.TotalAction, actionIndex + 1, name);
				}
				bool flag = true;
				XmlDefinition.APBundlePolicy.APBundlePolicyTemplate.ProgramTemplate programTemplate = null;
				if (string.Equals(action.ModulePN.Substring(5, 1), "A", StringComparison.OrdinalIgnoreCase))
				{
					Logger(LogLevel.Info, "For application module, check with APBundlePolicy");
					string[] files = Directory.GetFiles("C:\\OEM\\Preload\\Command", "Patch*.xml");
					foreach (string text2 in files)
					{
						XmlDefinition.APBundlePolicy aPBundlePolicy = new XmlDefinition.APBundlePolicy(text2);
						if (aPBundlePolicy.APBundlePolicyInfo != null)
						{
							programTemplate = aPBundlePolicy.APBundlePolicyInfo.Programs.FirstOrDefault((XmlDefinition.APBundlePolicy.APBundlePolicyTemplate.ProgramTemplate x) => string.Equals(x.GetProduct(), moduleEnc.ModuleInfo.Module.Name, StringComparison.OrdinalIgnoreCase));
							if (programTemplate != null)
							{
								Logger(LogLevel.Info, "Find " + moduleEnc.ModuleInfo.Module.Name + " in " + text2);
								break;
							}
						}
					}
					if (programTemplate == null && Data.APBundlePolicyDef.APBundlePolicyInfo != null)
					{
						programTemplate = Data.APBundlePolicyDef.APBundlePolicyInfo.Programs.FirstOrDefault((XmlDefinition.APBundlePolicy.APBundlePolicyTemplate.ProgramTemplate x) => string.Equals(x.GetProduct(), moduleEnc.ModuleInfo.Module.Name, StringComparison.OrdinalIgnoreCase));
						if (programTemplate != null)
						{
							Logger(LogLevel.Info, "Find " + moduleEnc.ModuleInfo.Module.Name + " in " + Data.APBundlePolicyDef.FilePath);
						}
					}
					if (programTemplate == null)
					{
						Logger(LogLevel.Info, "Cannot find " + moduleEnc.ModuleInfo.Module.Name + " in any APBundlePolicy file, this app will not be installed");
						flag = false;
					}
					else if (programTemplate.IsValidForInstalltion())
					{
						Logger(LogLevel.Info, "This app is valid for this image");
					}
					else
					{
						Logger(LogLevel.Info, "This app is invalid for this image, it will not be installed");
						flag = false;
					}
				}
				if (string.Equals(action.ModulePN.Substring(5, 1), "D", StringComparison.OrdinalIgnoreCase))
				{
					if (PnPDevice.GetAllDevices(out var deviceInformationList))
					{
						Logger(LogLevel.Info, "Get device info from WMI successfully");
					}
					else
					{
						Logger(LogLevel.Error, "Failed to get device info from WMI");
						deviceInformationList = null;
					}
					List<string> list = new List<string>();
					if (string.Equals(moduleEnc.ModuleInfo.AutoCheckHWID, "True", StringComparison.OrdinalIgnoreCase))
					{
						if (deviceInformationList != null)
						{
							Logger(LogLevel.Info, "For driver module with AutoCheckHWID = True, check HWID first");
							if (string.Equals(moduleEnc.ModuleInfo.HWIDs, "MUST", StringComparison.OrdinalIgnoreCase))
							{
								Logger(LogLevel.Info, "HWID is \"MUST\", this driver will be installed");
								list.Add("MUST");
							}
							else
							{
								foreach (string moduleHWID in moduleEnc.ModuleInfo.HWIDs.Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList())
								{
									Logger(LogLevel.Info, "Scan module HWID: " + moduleHWID);
									PnPDevice.DeviceInformation deviceInformation = deviceInformationList.FirstOrDefault((PnPDevice.DeviceInformation x) => x.HardwareIDList != null && string.Join(";", x.HardwareIDList).IndexOf(moduleHWID, StringComparison.OrdinalIgnoreCase) >= 0);
									if (deviceInformation != null)
									{
										Logger(LogLevel.Info, "Get matched HWID from local machine, HWID: " + string.Join(";", deviceInformation.HardwareIDList) + ", DeviceID: " + deviceInformation.DeviceID);
										list.Add(string.Join(";", deviceInformation.HardwareIDList));
									}
								}
							}
							if (list.Count == 0)
							{
								Logger(LogLevel.Info, "Cannot get any matched HWID from local machine, this driver will not be installed");
								flag = false;
							}
						}
						else
						{
							Logger(LogLevel.Error, "Since cannot get device info from WMI, this driver will not be installed");
							flag = false;
						}
					}
					else
					{
						Logger(LogLevel.Info, "For driver module with AutoCheckHWID != True, it will be always installed");
					}
					if (flag)
					{
						string text3 = Data.CurrentProcessFolder + "\\InstalledDriverInfo.ini";
						string filePath = Data.CurrentProcessFolder + "\\MachineHWID_History.ini";
						Logger(LogLevel.Info, "Write matched HWID to " + text3);
						SetIniKeyValue(text3, "Action" + (actionIndex + 1), "ModulePN", moduleEnc.ModuleInfo.ModulePN);
						SetIniKeyValue(text3, "Action" + (actionIndex + 1), "Name", moduleEnc.ModuleInfo.Module.Name);
						SetIniKeyValue(text3, "Action" + (actionIndex + 1), "AutoCheckHWID", moduleEnc.ModuleInfo.AutoCheckHWID);
						for (int num = 0; num < list.Count; num++)
						{
							SetIniKeyValue(text3, "Action" + (actionIndex + 1), "MatchedDeviceHWID_" + (num + 1).ToString("D3"), list[num]);
						}
						if (deviceInformationList != null)
						{
							int num2 = 0;
							int num3 = 1;
							for (; num2 < deviceInformationList.Length; num2++)
							{
								if (deviceInformationList[num2].HardwareIDList != null)
								{
									SetIniKeyValue(filePath, "Action" + (actionIndex + 1), num3.ToString("D3"), string.Join(";", deviceInformationList[num2].HardwareIDList));
									num3++;
								}
							}
						}
					}
				}
				if (!flag)
				{
					if (string.Equals(action.ModulePN.Substring(5, 1), "A", StringComparison.OrdinalIgnoreCase))
					{
						Logger(LogLevel.Info, "Write Reboot=False and Delete=True for this Application Action");
						SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Reboot", "False");
						SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Delete", "True");
					}
					else if (string.Equals(action.ModulePN.Substring(5, 1), "D", StringComparison.OrdinalIgnoreCase))
					{
						Logger(LogLevel.Info, "Write Reboot=False for this Driver Action");
						SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Reboot", "False");
						if (string.Equals(moduleEnc.ModuleInfo.CheckRemove, "True", StringComparison.OrdinalIgnoreCase))
						{
							Logger(LogLevel.Info, "CheckRemove=True, write Delete=True for this Driver Action");
							SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Delete", "True");
						}
					}
					return;
				}
				if (string.Equals(action.ModulePN.Substring(5, 1), "A", StringComparison.OrdinalIgnoreCase) && Data.ProductInfo.FindAppByName(moduleEnc.ModuleInfo.Module.Name) == null)
				{
					XmlDefinition.ProductInformation.PrdInfoTemplate.InstalledAppTemplate item = new XmlDefinition.ProductInformation.PrdInfoTemplate.InstalledAppTemplate
					{
						Name = moduleEnc.ModuleInfo.Module.Name,
						Version = moduleEnc.ModuleInfo.ModuleVersion,
						StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
					};
					Data.ProductInfo.PrdInfo.InstalledApps.Add(item);
					Data.ProductInfo.SaveToFile();
				}
				if (string.Equals(action.ModulePN.Substring(5, 1), "D", StringComparison.OrdinalIgnoreCase) && Data.ProductInfo.FindDriverByName(moduleEnc.ModuleInfo.Module.Name) == null)
				{
					XmlDefinition.ProductInformation.PrdInfoTemplate.InstalledDriverTemplate item2 = new XmlDefinition.ProductInformation.PrdInfoTemplate.InstalledDriverTemplate
					{
						Name = moduleEnc.ModuleInfo.Module.Name,
						DriverVersion = moduleEnc.ModuleInfo.DriverVersion,
						StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
					};
					Data.ProductInfo.PrdInfo.InstalledDrivers.Add(item2);
					Data.ProductInfo.SaveToFile();
				}
				if (string.Equals(action.ModulePN.Substring(5, 1), "A", StringComparison.OrdinalIgnoreCase))
				{
					Logger(LogLevel.Info, "Backup all links from C:\\Users\\Public\\Desktop to C:\\OEM\\Preload\\Command\\AlaunchX\\BackupLinks for application module");
					CommandLine commandLine = new CommandLine("robocopy.exe \"C:\\Users\\Public\\Desktop\" \"C:\\OEM\\Preload\\Command\\AlaunchX\\BackupLinks\" *.* /R:3");
					if (commandLine.ExitCode == 8 || commandLine.ExitCode == 16)
					{
						Logger(LogLevel.Error, "Backup links failed");
					}
					else
					{
						Logger(LogLevel.Info, "Backup all links successfully");
					}
				}
				int num4 = -1;
				bool flag2 = false;
				for (int num5 = 0; num5 < moduleEnc.ModuleInfo.ProcessList.Count; num5++)
				{
					Logger(LogLevel.Info, "===== Now handling process " + (num5 + 1) + " =====");
					if (string.Equals(moduleEnc.ModuleInfo.InstallPeriod, InstallPeriod.Multiple.ToString(), StringComparison.OrdinalIgnoreCase))
					{
						if (!string.Equals(moduleEnc.ModuleInfo.ProcessList[num5].Period, Data.CurrentInstallPeriod.ToString(), StringComparison.OrdinalIgnoreCase))
						{
							Logger(LogLevel.Info, "Skip. Process install period is not match(" + moduleEnc.ModuleInfo.ProcessList[num5].Period + ")");
							continue;
						}
						Logger(LogLevel.Info, "Pass. Process install period is match(" + moduleEnc.ModuleInfo.ProcessList[num5].Period + ")");
					}
					if (string.Equals(action.ModulePN.Substring(5, 1), "A", StringComparison.OrdinalIgnoreCase))
					{
						Logger(LogLevel.Info, "Check OS-Sku limitation for application module(POP WOS PN: " + Data.ImageOSPN + ", process OS-Sku: " + moduleEnc.ModuleInfo.ProcessList[num5].OSSku + ")");
						if (string.Equals(moduleEnc.ModuleInfo.ProcessList[num5].OSSku, Data.ImageOSPN, StringComparison.OrdinalIgnoreCase))
						{
							Logger(LogLevel.Info, "Pass. POP WOS PN is the same as process OS-Sku");
						}
						else
						{
							if (!Data.WOSDef.GetChildPNList(moduleEnc.ModuleInfo.ProcessList[num5].OSSku).Contains(Data.ImageOSPN))
							{
								Logger(LogLevel.Info, "Skip. This process will not be executed due to OS-Sku limitation.");
								continue;
							}
							Logger(LogLevel.Info, "Pass. POP WOS PN is contained in the child WOS PN list of process OS-Sku");
						}
					}
					Directory.SetCurrentDirectory(action.Path.TrimEnd(new char[1] { '\\' }) + "\\");
					int exitcode = -2;
					if (RunCmdWithoutOutputRedirection(moduleEnc.ModuleInfo.ProcessList[num5].Exec.Replace("%RELATIVE_PATH%", action.Path.TrimEnd(new char[1] { '\\' })), moduleEnc.ModuleInfo.ProcessList[num5].Params, ref exitcode))
					{
						flag2 = true;
						Logger(LogLevel.Info, "ExitCode is: " + exitcode);
						if (exitcode == 0 || exitcode == 3010)
						{
							if (num4 == -1 || num4 == 0)
							{
								num4 = 0;
							}
						}
						else
						{
							num4 = 1;
						}
					}
					else
					{
						Logger(LogLevel.Error, "Fail to execute this process");
					}
					Directory.SetCurrentDirectory(Data.CurrentProcessFolder + "\\");
					int num6 = 0;
					try
					{
						num6 = Convert.ToInt32(moduleEnc.ModuleInfo.ProcessList[num5].Timeout);
					}
					catch (Exception)
					{
						num6 = 0;
					}
					if (num6 > 0)
					{
						Logger(LogLevel.Info, "Sleep " + num6 + " seconds for process " + (num5 + 1) + " timeout");
						Thread.Sleep(num6 * 1000);
					}
				}
				SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Result", num4.ToString());
				if (string.Equals(action.ModulePN.Substring(5, 1), "D", StringComparison.OrdinalIgnoreCase))
				{
					XmlDefinition.ProductInformation.PrdInfoTemplate.InstalledDriverTemplate installedDriverTemplate = Data.ProductInfo.FindDriverByName(moduleEnc.ModuleInfo.Module.Name);
					DateTime value = DateTime.Parse(installedDriverTemplate.StartTime);
					DateTime now = DateTime.Now;
					installedDriverTemplate.EndTime = now.ToString("yyyy/MM/dd HH:mm:ss");
					installedDriverTemplate.SpentTime = now.Subtract(value).ToString("hh\\:mm\\:ss\\.fff");
					Data.ProductInfo.SaveToFile();
				}
				if (string.Equals(action.ModulePN.Substring(5, 1), "A", StringComparison.OrdinalIgnoreCase))
				{
					XmlDefinition.ProductInformation.PrdInfoTemplate.InstalledAppTemplate installedAppTemplate = Data.ProductInfo.FindAppByName(moduleEnc.ModuleInfo.Module.Name);
					DateTime value2 = DateTime.Parse(installedAppTemplate.StartTime);
					DateTime now2 = DateTime.Now;
					installedAppTemplate.EndTime = now2.ToString("yyyy/MM/dd HH:mm:ss");
					installedAppTemplate.SpentTime = now2.Subtract(value2).ToString("hh\\:mm\\:ss\\.fff");
					Data.ProductInfo.SaveToFile();
					if (!flag2)
					{
						Logger(LogLevel.Info, "Write Delete=True for this Action since all processes are failed to be executed");
						SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Delete", "True");
						Data.ProductInfo.RemoveInstalledApp(moduleEnc.ModuleInfo.Module.Name);
						Data.ProductInfo.SaveToFile();
					}
					else
					{
						string text4 = action.Path + "\\NoMeetSPEC_NeedRemoveProgramID.tag";
						if (File.Exists(text4))
						{
							Logger(LogLevel.Info, "Write Delete=True for this Action since " + text4 + " exists(this program has its special rule)");
							SetIniKeyValue(alaunchXAnswerFile.FilePath, "Action" + (actionIndex + 1), "Delete", "True");
							Data.ProductInfo.RemoveInstalledApp(moduleEnc.ModuleInfo.Module.Name);
							Data.ProductInfo.SaveToFile();
						}
						else if (Data.APBundlePolicyDef.APBundlePolicyInfo != null)
						{
							if (!Directory.Exists("C:\\OEM\\Preload\\InstalledApps"))
							{
								Logger(LogLevel.Info, "Create directory: C:\\OEM\\Preload\\InstalledApps");
								Directory.CreateDirectory("C:\\OEM\\Preload\\InstalledApps");
							}
							using (StreamWriter streamWriter = new StreamWriter("C:\\OEM\\Preload\\InstalledApps\\" + programTemplate.pid + ".tag", append: false))
							{
								streamWriter.Write(moduleEnc.ModuleInfo.ModuleVersion);
								streamWriter.Close();
							}
							string text5 = string.Empty;
							string text6 = string.Empty;
							string text7 = "Software\\OEM\\GCMReadiness";
							string text8 = "IR";
							string text9 = "OSSKU";
							using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(text7, writable: true);
							if (registryKey == null)
							{
								Logger(LogLevel.Error, "Fail to open registry key: HKLM\\" + text7);
							}
							else
							{
								try
								{
									object value3 = registryKey.GetValue(text8);
									if (value3 == null)
									{
										Logger(LogLevel.Error, "Cannot find \"" + text8 + "\" in HKLM\\" + text7);
									}
									else
									{
										text5 = (string)value3;
										Logger(LogLevel.Info, "Get " + text8 + "=" + text5 + " (in HKLM\\" + text7 + ")");
									}
								}
								catch (Exception ex2)
								{
									Logger(LogLevel.Error, "Catch exception: " + ex2.Message);
									Logger(LogLevel.Error, "Fail to get \"" + text8 + "\" in HKLM\\" + text7);
								}
								try
								{
									object value4 = registryKey.GetValue(text9);
									if (value4 == null)
									{
										Logger(LogLevel.Error, "Cannot find \"" + text9 + "\" in HKLM\\" + text7);
									}
									else
									{
										text6 = (string)value4;
										Logger(LogLevel.Info, "Get " + text9 + "=" + text6 + " (in HKLM\\" + text7 + ")");
									}
								}
								catch (Exception ex3)
								{
									Logger(LogLevel.Error, "Catch exception: " + ex3.Message);
									Logger(LogLevel.Error, "Fail to get \"" + text9 + "\" in HKLM\\" + text7);
								}
								bool flag3 = false;
								if (string.Equals(text6, "Windows 10", StringComparison.OrdinalIgnoreCase))
								{
									Logger(LogLevel.Info, "OSSKU=Windows 10, check placement type \"DT\"");
									flag3 = Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "DT", text5);
								}
								else
								{
									Logger(LogLevel.Info, "Check placement type \"DS\"");
									flag3 = Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "DS", text5);
								}
								if (flag3)
								{
									Logger(LogLevel.Info, "(Required)This program should be pinned on Desktop");
								}
								else
								{
									Logger(LogLevel.Info, "This program should not be pinned on Desktop");
									Logger(LogLevel.Info, "Delete all files in C:\\Users\\Public\\Desktop");
									string[] files = Directory.GetFiles("C:\\Users\\Public\\Desktop");
									foreach (string text10 in files)
									{
										if (!string.Equals(Path.GetFileName(text10), "desktop.ini", StringComparison.OrdinalIgnoreCase))
										{
											if (DeleteFile(text10))
											{
												Logger(LogLevel.Info, "Delete " + text10 + " successfully");
											}
											else
											{
												Logger(LogLevel.Error, "Fail to delete " + text10);
											}
										}
									}
									Logger(LogLevel.Info, "Restore all links from C:\\OEM\\Preload\\Command\\AlaunchX\\BackupLinks to C:\\Users\\Public\\Desktop");
									CommandLine commandLine2 = new CommandLine("robocopy.exe \"C:\\OEM\\Preload\\Command\\AlaunchX\\BackupLinks\" \"C:\\Users\\Public\\Desktop\" *.* /R:3");
									if (commandLine2.ExitCode == 8 || commandLine2.ExitCode == 16)
									{
										Logger(LogLevel.Error, "Restore links failed");
									}
									else
									{
										Logger(LogLevel.Info, "Restore all links successfully");
									}
								}
								bool flag4 = false;
								if (string.Equals(text6, "Windows 10", StringComparison.OrdinalIgnoreCase))
								{
									Logger(LogLevel.Info, "OSSKU=Windows 10, check placement type \"SM\"");
									flag4 = Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "SM", text5);
								}
								else
								{
									Logger(LogLevel.Info, "Check placement type \"NS\"");
									flag4 = Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "NS", text5);
								}
								if (flag4)
								{
									Logger(LogLevel.Info, "(Required)This program should be pinned on Start Menu");
									string text11 = "C:\\OEM\\Preload\\InstalledApps\\SS";
									if (!Directory.Exists(text11))
									{
										Logger(LogLevel.Info, "Create folder: " + text11);
										Directory.CreateDirectory(text11);
									}
									string[] files2 = Directory.GetFiles(action.Path, "AUMID*.txt");
									if (files2.Length != 0)
									{
										string text12 = files2[0];
										string text13 = text11 + "\\" + programTemplate.pid + ".tag";
										for (int num7 = 0; num7 < 3; num7++)
										{
											if (CopyFile(text12, text13))
											{
												Logger(LogLevel.Info, "Copy " + text12 + " to " + text13 + " successfully");
												Logger(LogLevel.Info, "Check the content correctness of " + text13);
												string text14 = File.ReadAllText(text12);
												Logger(LogLevel.Info, "Content of " + text12 + ":");
												Logger(LogLevel.Info, text14);
												string text15 = File.ReadAllText(text13);
												Logger(LogLevel.Info, "Content of " + text13 + ":");
												Logger(LogLevel.Info, text15);
												if (string.Equals(text14.Trim(), text15.Trim(), StringComparison.OrdinalIgnoreCase))
												{
													Logger(LogLevel.Info, "Content is the same");
													break;
												}
												Logger(LogLevel.Error, "Content is not the same, retry");
											}
											else
											{
												Logger(LogLevel.Error, "Failed to copy " + text12 + " to " + text13 + ", retry");
											}
										}
									}
									else
									{
										Logger(LogLevel.Error, "Cannot find any AUMID*.txt in " + action.Path);
									}
								}
								else
								{
									Logger(LogLevel.Info, "This program should not be pinned on Start Menu");
								}
								Logger(LogLevel.Info, "Check placement type \"TS\"");
								if (Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "TS", text5))
								{
									Logger(LogLevel.Info, "(Required)This program should be pinned on Taskbar");
									string text16 = "C:\\OEM\\Preload\\InstalledApps\\TB";
									if (!Directory.Exists(text16))
									{
										Logger(LogLevel.Info, "Create folder: " + text16);
										Directory.CreateDirectory(text16);
									}
									string[] files3 = Directory.GetFiles(action.Path, "TaskbarLink*.txt");
									if (files3.Length != 0)
									{
										string text17 = files3[0];
										string text18 = text16 + "\\" + programTemplate.pid + ".tag";
										for (int num8 = 0; num8 < 3; num8++)
										{
											if (CopyFile(text17, text18))
											{
												Logger(LogLevel.Info, "Copy " + text17 + " to " + text18 + " successfully");
												Logger(LogLevel.Info, "Check the content correctness of " + text18);
												string text19 = File.ReadAllText(text17);
												Logger(LogLevel.Info, "Content of " + text17 + ":");
												Logger(LogLevel.Info, text19);
												string text20 = File.ReadAllText(text18);
												Logger(LogLevel.Info, "Content of " + text18 + ":");
												Logger(LogLevel.Info, text20);
												if (string.Equals(text19.Trim(), text20.Trim(), StringComparison.OrdinalIgnoreCase))
												{
													Logger(LogLevel.Info, "Content is the same");
													break;
												}
												Logger(LogLevel.Error, "Content is not the same, retry");
											}
											else
											{
												Logger(LogLevel.Error, "Failed to copy " + text17 + " to " + text18 + ", retry");
											}
										}
									}
									else
									{
										Logger(LogLevel.Error, "Cannot find any TaskbarLink*.txt.txt in " + action.Path);
									}
								}
								else
								{
									Logger(LogLevel.Info, "This program should not be pinned on Taskbar");
								}
								Logger(LogLevel.Info, "Check placement type \"NA\"");
								if (Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "NA", text5))
								{
									Logger(LogLevel.Info, "(Required)This program should be pinned on Notification Area");
									string text21 = "C:\\OEM\\Preload\\InstalledApps\\NA";
									if (!Directory.Exists(text21))
									{
										Logger(LogLevel.Info, "Create folder: " + text21);
										Directory.CreateDirectory(text21);
									}
									string[] files4 = Directory.GetFiles(action.Path, "NotificationArea*.txt");
									if (files4.Length != 0)
									{
										string text22 = files4[0];
										string text23 = text21 + "\\" + programTemplate.pid + ".tag";
										for (int num9 = 0; num9 < 3; num9++)
										{
											if (CopyFile(text22, text23))
											{
												Logger(LogLevel.Info, "Copy " + text22 + " to " + text23 + " successfully");
												Logger(LogLevel.Info, "Check the content correctness of " + text23);
												string text24 = File.ReadAllText(text22);
												Logger(LogLevel.Info, "Content of " + text22 + ":");
												Logger(LogLevel.Info, text24);
												string text25 = File.ReadAllText(text23);
												Logger(LogLevel.Info, "Content of " + text23 + ":");
												Logger(LogLevel.Info, text25);
												if (string.Equals(text24.Trim(), text25.Trim(), StringComparison.OrdinalIgnoreCase))
												{
													Logger(LogLevel.Info, "Content is the same");
													break;
												}
												Logger(LogLevel.Error, "Content is not the same, retry");
											}
											else
											{
												Logger(LogLevel.Error, "Failed to copy " + text22 + " to " + text23 + ", retry");
											}
										}
									}
									else
									{
										Logger(LogLevel.Error, "Cannot find any NotificationArea*.txt in " + action.Path);
									}
								}
								else
								{
									Logger(LogLevel.Info, "This program should not be pinned on Notification Area");
								}
								Logger(LogLevel.Info, "Check placement type \"MF\"");
								if (Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "MF", text5))
								{
									Logger(LogLevel.Info, "(Required)This program should be pinned on Most Frequently Used");
									string text26 = "C:\\OEM\\Preload\\InstalledApps\\MF";
									if (!Directory.Exists(text26))
									{
										Logger(LogLevel.Info, "Create folder: " + text26);
										Directory.CreateDirectory(text26);
									}
									string[] files5 = Directory.GetFiles(action.Path, "MFULink*.txt");
									if (files5.Length != 0)
									{
										string text27 = files5[0];
										string text28 = text26 + "\\" + programTemplate.pid + ".tag";
										for (int num10 = 0; num10 < 3; num10++)
										{
											if (CopyFile(text27, text28))
											{
												Logger(LogLevel.Info, "Copy " + text27 + " to " + text28 + " successfully");
												Logger(LogLevel.Info, "Check the content correctness of " + text28);
												string text29 = File.ReadAllText(text27);
												Logger(LogLevel.Info, "Content of " + text27 + ":");
												Logger(LogLevel.Info, text29);
												string text30 = File.ReadAllText(text28);
												Logger(LogLevel.Info, "Content of " + text28 + ":");
												Logger(LogLevel.Info, text30);
												if (string.Equals(text29.Trim(), text30.Trim(), StringComparison.OrdinalIgnoreCase))
												{
													Logger(LogLevel.Info, "Content is the same");
													break;
												}
												Logger(LogLevel.Error, "Content is not the same, retry");
											}
											else
											{
												Logger(LogLevel.Error, "Failed to copy " + text27 + " to " + text28 + ", retry");
											}
										}
									}
									else
									{
										Logger(LogLevel.Error, "Cannot find any MFULink*.txt in " + action.Path);
									}
								}
								else
								{
									Logger(LogLevel.Info, "This program should not be pinned on Most Frequently Used");
								}
								Logger(LogLevel.Info, "Check placement type \"RM\"");
								if (Data.APBundlePolicyDef.APBundlePolicyInfo.IsNeedToPin(programTemplate.pid, "RM", text5))
								{
									Logger(LogLevel.Info, "(Required)This program should be pinned on Recommended");
									string text31 = "C:\\OEM\\Preload\\InstalledApps\\RM";
									if (!Directory.Exists(text31))
									{
										Logger(LogLevel.Info, "Create folder: " + text31);
										Directory.CreateDirectory(text31);
									}
									string[] files6 = Directory.GetFiles(action.Path, "RecommandedLINK*.txt");
									if (files6.Length != 0)
									{
										string text32 = files6[0];
										string text33 = text31 + "\\" + programTemplate.pid + ".tag";
										for (int num11 = 0; num11 < 3; num11++)
										{
											if (CopyFile(text32, text33))
											{
												Logger(LogLevel.Info, "Copy " + text32 + " to " + text33 + " successfully");
												Logger(LogLevel.Info, "Check the content correctness of " + text33);
												string text34 = File.ReadAllText(text32);
												Logger(LogLevel.Info, "Content of " + text32 + ":");
												Logger(LogLevel.Info, text34);
												string text35 = File.ReadAllText(text33);
												Logger(LogLevel.Info, "Content of " + text33 + ":");
												Logger(LogLevel.Info, text35);
												if (string.Equals(text34.Trim(), text35.Trim(), StringComparison.OrdinalIgnoreCase))
												{
													Logger(LogLevel.Info, "Content is the same");
													break;
												}
												Logger(LogLevel.Error, "Content is not the same, retry");
											}
											else
											{
												Logger(LogLevel.Error, "Failed to copy " + text32 + " to " + text33 + ", retry");
											}
										}
									}
									else
									{
										Logger(LogLevel.Error, "Cannot find any RecommandedLINK*.txt in " + action.Path);
									}
								}
								else
								{
									Logger(LogLevel.Info, "This program should not be pinned on Recommended");
								}
							}
						}
					}
				}
				int num12 = 0;
				try
				{
					num12 = Convert.ToInt32(moduleEnc.ModuleInfo.ModuleTimeout);
				}
				catch (Exception)
				{
					num12 = 0;
				}
				if (num12 > 0)
				{
					Logger(LogLevel.Info, "Sleep " + num12 + " seconds for module timeout");
					Thread.Sleep(num12 * 1000);
				}
				if (Data.GlobalModuleTimeout > 0)
				{
					Logger(LogLevel.Info, "Sleep " + Data.GlobalModuleTimeout + " seconds for global module timeout");
					Thread.Sleep(Data.GlobalModuleTimeout * 1000);
				}
			}
			else
			{
				Logger(LogLevel.Error, "Failed to read " + text);
			}
		}
		catch (ThreadAbortException ex5)
		{
			Logger(LogLevel.Warn, ex5.Message);
		}
		catch (Exception ex6)
		{
			Logger(LogLevel.Error, ex6.ToString());
		}
	}

	public static void Reboot()
	{
		Logger(LogLevel.Info, "Create " + Data.ShutdownTagPath);
		using (StreamWriter streamWriter = new StreamWriter(Data.ShutdownTagPath, append: false))
		{
			streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}" + "\tCreated by AlaunchX v" + Data.AlaunchXVersion + "(Triggered by AlaunchX)");
			streamWriter.Close();
		}
		new CommandLine("shutdown -r -t 0");
	}
}
