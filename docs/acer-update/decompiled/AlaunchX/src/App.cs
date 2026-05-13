using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace AlaunchX;

public class App : Application
{
	private void Application_Startup(object sender, StartupEventArgs e)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		Process proc = Process.GetCurrentProcess();
		if ((from p in Process.GetProcesses()
			where p.ProcessName == proc.ProcessName
			select p).Count() > 1)
		{
			MessageBox.Show("Already an instance is running...");
			Application.Current.Shutdown();
		}
		if (e.Args.Length != 1)
		{
			MessageBox.Show("Only accept 1 argument", "Error", (MessageBoxButton)0, (MessageBoxImage)16);
			Application.Current.Shutdown();
		}
		string[] args = e.Args;
		foreach (string a in args)
		{
			if (string.Equals(a, "/Audit", StringComparison.OrdinalIgnoreCase))
			{
				Data.CurrentInstallPeriod = InstallPeriod.AuditAlaunch;
			}
			if (string.Equals(a, "/BeforeOOBE", StringComparison.OrdinalIgnoreCase))
			{
				Data.CurrentInstallPeriod = InstallPeriod.BeforeOOBE;
			}
			if (string.Equals(a, "/User", StringComparison.OrdinalIgnoreCase))
			{
				Data.CurrentInstallPeriod = InstallPeriod.UserAlaunch;
			}
			if (string.Equals(a, "/FirstBoot", StringComparison.OrdinalIgnoreCase))
			{
				Data.CurrentInstallPeriod = InstallPeriod.FirstBoot;
			}
		}
		if (Data.CurrentInstallPeriod == InstallPeriod.Unknown)
		{
			MessageBox.Show("Cannot determine install period, please make sure you input correct argument", "Error", (MessageBoxButton)0, (MessageBoxImage)16);
			Application.Current.Shutdown();
		}
		if (!Directory.Exists("C:\\OEM\\AcerLogs\\AlaunchXLogs"))
		{
			Directory.CreateDirectory("C:\\OEM\\AcerLogs\\AlaunchXLogs");
		}
		Utility.CurrentLogPath = "C:\\OEM\\AcerLogs\\AlaunchXLogs\\AlaunchX_" + Data.CurrentInstallPeriod.ToString() + "_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ".log";
		Utility.Logger(LogLevel.Info, "AlaunchX v" + Data.AlaunchXVersion + " starts");
		Utility.Logger(LogLevel.Info, "Current phase: " + Data.CurrentInstallPeriod);
		if (File.Exists(Data.ShutdownTagPath))
		{
			Utility.Logger(LogLevel.Info, "Delete " + Data.ShutdownTagPath);
			if (!Utility.DeleteFile(Data.ShutdownTagPath))
			{
				Utility.Logger(LogLevel.Error, "Fail to delete " + Data.ShutdownTagPath);
			}
		}
		Utility.Logger(LogLevel.Info, "Load " + Data.CurrentProcessFolder + "\\WOSList.xml");
		Data.WOSDef = new XmlDefinition.WOS(Data.CurrentProcessFolder + "\\WOSList.xml");
		if (Data.WOSDef.WOSInfo == null)
		{
			Utility.Logger(LogLevel.Error, "Failed to load " + Data.CurrentProcessFolder + "\\WOSList.xml");
		}
		Utility.Logger(LogLevel.Info, "Load " + Data.CurrentProcessFolder + "\\Lang.xml");
		Data.LangDef = new XmlDefinition.Lang(Data.CurrentProcessFolder + "\\Lang.xml");
		if (Data.LangDef.LangInfo == null)
		{
			Utility.Logger(LogLevel.Error, "Failed to load " + Data.CurrentProcessFolder + "\\Lang.xml");
		}
		Utility.Logger(LogLevel.Info, "Load C:\\OEM\\Preload\\Command\\APBundlePolicy.xml");
		Data.APBundlePolicyDef = new XmlDefinition.APBundlePolicy("C:\\OEM\\Preload\\Command\\APBundlePolicy.xml");
		if (Data.APBundlePolicyDef.APBundlePolicyInfo == null)
		{
			Utility.Logger(LogLevel.Error, "Failed to load C:\\OEM\\Preload\\Command\\APBundlePolicy.xml");
		}
		Data.AfterOOBE = Utility.IsAfterOOBE();
		Utility.Logger(LogLevel.Info, "IsAfterOOBE: " + Data.AfterOOBE);
		Utility.GetArchitecture(ref Data.ProcessArchitecture, ref Data.OSArchitecture);
		Utility.Logger(LogLevel.Info, "Process architecture: " + Data.ProcessArchitecture.ToString() + ", OS architecture: " + Data.OSArchitecture);
		Window val = (Window)((Data.CurrentInstallPeriod == InstallPeriod.AuditAlaunch) ? new Window_BarMode() : ((Data.CurrentInstallPeriod == InstallPeriod.BeforeOOBE || Data.CurrentInstallPeriod == InstallPeriod.FirstBoot) ? new Window_Hidden() : (Data.AfterOOBE ? new Window_Main() : ((!string.Equals(Data.NAPPFlow, "NAPP2P", StringComparison.OrdinalIgnoreCase) && !string.Equals(Data.NAPPFlow, "NAPP3P", StringComparison.OrdinalIgnoreCase)) ? ((object)new Window_FullScreen()) : ((object)new Window_BarMode())))));
		val.Show();
	}

	private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
	{
		Utility.Logger(LogLevel.Info, "System session end event is raised");
		if (!File.Exists(Data.CurrentProcessFolder + "\\Shutdown.tag"))
		{
			Utility.Logger(LogLevel.Warn, "Shutdown.tag not exists, module or someone trigger shutdown");
			if (Data.CurrentWorkThread != null && Data.CurrentWorkThread.IsAlive)
			{
				Utility.Logger(LogLevel.Info, "Thread is still working, abort the working thread");
				Data.CurrentWorkThread.Abort();
				Utility.Logger(LogLevel.Info, "Create " + Data.ShutdownTagPath);
				using StreamWriter streamWriter = new StreamWriter(Data.ShutdownTagPath, append: false);
				streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}" + "\tCreate by AlaunchX v" + Data.AlaunchXVersion + "(session ending)");
				streamWriter.Close();
			}
		}
		((CancelEventArgs)(object)e).Cancel = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		((Application)this).Startup += new StartupEventHandler(Application_Startup);
		((Application)this).SessionEnding += new SessionEndingCancelEventHandler(Application_SessionEnding);
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		((Application)app).Run();
	}
}
