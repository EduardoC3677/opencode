using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace AlaunchX;

internal static class Data
{
	public static InstallPeriod CurrentInstallPeriod = InstallPeriod.Unknown;

	public static InstallationProcess MainControl = null;

	public static Thread CurrentWorkThread = null;

	public static string AlaunchXVersion = Assembly.GetExecutingAssembly().GetName().Version.Major + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor + "." + Assembly.GetExecutingAssembly().GetName().Version.Build + "." + Assembly.GetExecutingAssembly().GetName().Version.Revision;

	public const string AlaunchXLogsFolderPath = "C:\\OEM\\AcerLogs\\AlaunchXLogs";

	public const string PAPFolderPath = "C:\\OEM\\Preload\\Command\\PAP";

	public const string APBundlePolicyPath = "C:\\OEM\\Preload\\Command\\APBundlePolicy.xml";

	public const string AnswerFileName_AuditAlaunch = "AuditAlaunchX.ini";

	public const string AnswerFileName_BeforeOOBE = "BeforeOOBE.ini";

	public const string AnswerFileName_UserAlaunch = "UserAlaunchX.ini";

	public const string AnswerFileName_FirstBoot = "FirstBoot.ini";

	public const string BackupLinksFolderPath = "C:\\OEM\\Preload\\Command\\AlaunchX\\BackupLinks";

	public const string PublicDesktopFolderPath = "C:\\Users\\Public\\Desktop";

	public const string NoMeetSpecTagFileName = "NoMeetSPEC_NeedRemoveProgramID.tag";

	public const string InstallAppsFolderPath = "C:\\OEM\\Preload\\InstalledApps";

	public const string PreDebugCmdPath = "C:\\OEM\\Preload\\ModuleDebug\\BeforeModule.cmd";

	public const string PostDebugCmdPath = "C:\\OEM\\Preload\\ModuleDebug\\AfterModule.cmd";

	public static XmlDefinition.Lang LangDef = null;

	public static XmlDefinition.WOS WOSDef = null;

	public static XmlDefinition.APBundlePolicy APBundlePolicyDef = null;

	public static XmlDefinition.ProductInformation ProductInfo = null;

	public static Architecture ProcessArchitecture = Architecture.Unknown;

	public static Architecture OSArchitecture = Architecture.Unknown;

	private static string _currentProcessFolder = string.Empty;

	private static string _shutdownTagPath = string.Empty;

	private static Brand _imageBrand = Brand.Unknown;

	public static bool AfterOOBE = false;

	private const int LOGPIXELSX = 88;

	private const int LOGPIXELSY = 90;

	private static double _dpiX_Ratio = 0.0;

	private static double _dpiY_Ratio = 0.0;

	private static string _imageOSPN = string.Empty;

	private static string _nappFlow = string.Empty;

	private static int _rebootTimeout = -1;

	private static int _globalModuleTimeout = -1;

	private static string _gcmWhiteListPath = string.Empty;

	private static List<string> _gcmIMWhiteList = new List<string>();

	private static string _windowsMode = string.Empty;

	private static string _stringResourceCulture = string.Empty;

	public static string CurrentProcessFolder
	{
		get
		{
			if (!string.IsNullOrEmpty(_currentProcessFolder))
			{
				return _currentProcessFolder;
			}
			if (Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName).Equals(Path.GetPathRoot(Process.GetCurrentProcess().MainModule.FileName)))
			{
				_currentProcessFolder = Path.GetPathRoot(Process.GetCurrentProcess().MainModule.FileName).Substring(0, 2);
			}
			else
			{
				_currentProcessFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			}
			return _currentProcessFolder;
		}
	}

	public static string ShutdownTagPath
	{
		get
		{
			if (string.IsNullOrEmpty(_shutdownTagPath))
			{
				_shutdownTagPath = CurrentProcessFolder + "\\Shutdown.tag";
			}
			return _shutdownTagPath;
		}
	}

	public static Brand ImageBrand
	{
		get
		{
			if (_imageBrand == Brand.Unknown)
			{
				if (Directory.Exists("C:\\OEM\\Preload\\Command\\PAP"))
				{
					string[] files = Directory.GetFiles("C:\\OEM\\Preload\\Command\\PAP");
					if (files.Length >= 1)
					{
						string iniKeyValue = Utility.GetIniKeyValue(files[0], "Main", "Brand");
						if (iniKeyValue.StartsWith("Acer", StringComparison.OrdinalIgnoreCase))
						{
							_imageBrand = Brand.Acer;
						}
						if (iniKeyValue.StartsWith("Gateway", StringComparison.OrdinalIgnoreCase))
						{
							_imageBrand = Brand.Gateway;
						}
						if (iniKeyValue.StartsWith("PackardBell", StringComparison.OrdinalIgnoreCase))
						{
							_imageBrand = Brand.PackardBell;
						}
					}
				}
				if (_imageBrand == Brand.Unknown)
				{
					_imageBrand = Brand.Acer;
				}
			}
			return _imageBrand;
		}
	}

	public static double DpiX_Ratio
	{
		get
		{
			if (_dpiX_Ratio == 0.0)
			{
				IntPtr dC = GetDC(IntPtr.Zero);
				int deviceCaps = GetDeviceCaps(dC, 88);
				ReleaseDC(IntPtr.Zero, dC);
				Utility.Logger(LogLevel.Info, "DPI_X: " + deviceCaps);
				deviceCaps = (((double)deviceCaps <= 96.0) ? 96 : (((double)deviceCaps <= 120.0 && (double)deviceCaps > 96.0) ? 120 : ((!((double)deviceCaps <= 144.0) || !((double)deviceCaps > 120.0)) ? 192 : 144)));
				_dpiX_Ratio = (double)deviceCaps / 96.0;
				Utility.Logger(LogLevel.Info, "DPI_X Ratio: " + _dpiX_Ratio);
			}
			return _dpiX_Ratio;
		}
	}

	public static double DpiY_Ratio
	{
		get
		{
			if (_dpiY_Ratio == 0.0)
			{
				IntPtr dC = GetDC(IntPtr.Zero);
				int deviceCaps = GetDeviceCaps(dC, 90);
				ReleaseDC(IntPtr.Zero, dC);
				Utility.Logger(LogLevel.Info, "DPI_Y: " + deviceCaps);
				deviceCaps = (((double)deviceCaps <= 96.0) ? 96 : (((double)deviceCaps <= 120.0 && (double)deviceCaps > 96.0) ? 120 : ((!((double)deviceCaps <= 144.0) || !((double)deviceCaps > 120.0)) ? 192 : 144)));
				_dpiY_Ratio = (double)deviceCaps / 96.0;
				Utility.Logger(LogLevel.Info, "DPI_Y Ratio: " + _dpiY_Ratio);
			}
			return _dpiY_Ratio;
		}
	}

	public static string ImageOSPN
	{
		get
		{
			if (string.IsNullOrEmpty(_imageOSPN) && Directory.Exists("C:\\OEM\\Preload\\Command"))
			{
				string[] files = Directory.GetFiles("C:\\OEM\\Preload\\Command", "POP*.ini");
				if (files.Length >= 1)
				{
					_imageOSPN = Utility.GetIniKeyValue(files[0], "Main", "OS");
				}
			}
			return _imageOSPN;
		}
	}

	public static string NAPPFlow
	{
		get
		{
			if (string.IsNullOrEmpty(_nappFlow))
			{
				_nappFlow = Utility.GetIniKeyValue(CurrentProcessFolder + "\\NAPP.ini", "Main", "Flow");
			}
			return _nappFlow;
		}
	}

	public static int RebootTimeout
	{
		get
		{
			if (_rebootTimeout == -1)
			{
				try
				{
					_rebootTimeout = Convert.ToInt32(Utility.GetIniKeyValue(CurrentProcessFolder + "\\Settings.ini", "Main", "Reboot_Timeout"));
					if (_rebootTimeout < 0)
					{
						_rebootTimeout = 5;
					}
				}
				catch (Exception)
				{
					_rebootTimeout = 5;
				}
			}
			return _rebootTimeout;
		}
	}

	public static int GlobalModuleTimeout
	{
		get
		{
			if (_globalModuleTimeout == -1)
			{
				try
				{
					_globalModuleTimeout = Convert.ToInt32(Utility.GetIniKeyValue(CurrentProcessFolder + "\\Settings.ini", "Main", "Module_Timeout"));
					if (_globalModuleTimeout < 0)
					{
						_globalModuleTimeout = 0;
					}
				}
				catch (Exception)
				{
					_globalModuleTimeout = 0;
				}
			}
			return _globalModuleTimeout;
		}
	}

	public static string GCMWhiteListPath
	{
		get
		{
			if (string.IsNullOrEmpty(_gcmWhiteListPath))
			{
				_gcmWhiteListPath = Utility.GetIniKeyValue(CurrentProcessFolder + "\\Settings.ini", "Main", "GCM_WhiteList_Path");
			}
			return _gcmWhiteListPath;
		}
	}

	public static List<string> GCMIMWhiteList
	{
		get
		{
			if (_gcmIMWhiteList.Count == 0 && File.Exists(GCMWhiteListPath))
			{
				string[] array = File.ReadAllLines(GCMWhiteListPath);
				foreach (string text in array)
				{
					if (!_gcmIMWhiteList.Contains(text.Trim()))
					{
						_gcmIMWhiteList.Add(text.Trim());
					}
				}
			}
			return _gcmIMWhiteList;
		}
	}

	public static string WindowsMode
	{
		get
		{
			if (string.IsNullOrEmpty(_windowsMode))
			{
				_windowsMode = Utility.GetIniKeyValue(CurrentProcessFolder + "\\Settings.ini", "Main", "WindowsMode");
			}
			return _windowsMode;
		}
	}

	public static string StringResourceCulture
	{
		get
		{
			if (string.IsNullOrEmpty(_stringResourceCulture))
			{
				string text = CultureInfo.CurrentUICulture.ToString().ToUpper(new CultureInfo("en-US"));
				if (text.Equals("NO") || text.Equals("NB-NO") || text.Equals("NN-NO"))
				{
					_stringResourceCulture = "no";
				}
				else
				{
					_stringResourceCulture = CultureInfo.CurrentUICulture.Parent.Name;
				}
			}
			return _stringResourceCulture;
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr ptr);

	[DllImport("gdi32.dll")]
	private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

	[DllImport("user32.dll")]
	private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
}
