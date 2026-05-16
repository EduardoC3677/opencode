using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;

namespace AlaunchX;

public class Window_BarMode : Window, IComponentConnector
{
	private int _shiftOffset = 73;

	internal Viewbox Viewbox_BarModeArea;

	internal Grid Grid_BarModeArea;

	private bool _contentLoaded;

	public Window_BarMode()
	{
		InitializeComponent();
		SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
		UpdateUI();
	}

	private void Loaded_Window_BarMode(object sender, RoutedEventArgs e)
	{
		Utility.Logger(LogLevel.Info, "Window style: BarMode");
		Utility.LockKeyBoardMouse();
		if (Data.CurrentInstallPeriod == InstallPeriod.AuditAlaunch || Data.CurrentInstallPeriod == InstallPeriod.UserAlaunch)
		{
			Utility.DisableS3();
		}
		Utility.MonitorShutdownEvent();
		UserControl_BarMode userControl_BarMode = (UserControl_BarMode)(Data.MainControl = new UserControl_BarMode());
		((Panel)Grid_BarModeArea).Children.Add((UIElement)(object)userControl_BarMode);
	}

	private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
	{
		Utility.Logger(LogLevel.Info, "Display settings changed");
		UpdateUI();
	}

	private void UpdateUI()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		Utility.Logger(LogLevel.Info, "Current resolution: " + SystemParameters.PrimaryScreenWidth + "*" + SystemParameters.PrimaryScreenHeight);
		Rect workArea = SystemParameters.WorkArea;
		((Window)this).Left = (((Rect)(ref workArea)).Width - ((FrameworkElement)this).Width) / 2.0;
		workArea = SystemParameters.WorkArea;
		((Window)this).Top = ((Rect)(ref workArea)).Height - ((FrameworkElement)this).Height;
	}

	private void Closing_Window_BarMode(object sender, CancelEventArgs e)
	{
		e.Cancel = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("/AlaunchX;component/window_barmode.xaml", UriKind.Relative);
			Application.LoadComponent((object)this, uri);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		switch (connectionId)
		{
		case 1:
			((FrameworkElement)(Window_BarMode)target).Loaded += new RoutedEventHandler(Loaded_Window_BarMode);
			((Window)(Window_BarMode)target).Closing += Closing_Window_BarMode;
			break;
		case 2:
			Viewbox_BarModeArea = (Viewbox)target;
			break;
		case 3:
			Grid_BarModeArea = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
