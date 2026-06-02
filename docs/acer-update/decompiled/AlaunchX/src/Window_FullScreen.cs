using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AlaunchX;

public class Window_FullScreen : Window, IComponentConnector
{
	internal Grid Grid_FullScreen;

	internal Viewbox Viewbox_MainArea;

	internal Grid Grid_MainArea;

	private bool _contentLoaded;

	public Window_FullScreen()
	{
		InitializeComponent();
		((Window)this).WindowStartupLocation = (WindowStartupLocation)0;
		((Window)this).Left = 0.0;
		((Window)this).Top = 0.0;
		SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
		UpdateUI();
	}

	private void Loaded_Window_FullScreen(object sender, RoutedEventArgs e)
	{
		Utility.Logger(LogLevel.Info, "Window style: FullScreen");
		Utility.LockKeyBoardMouse();
		if (Data.CurrentInstallPeriod == InstallPeriod.UserAlaunch)
		{
			Utility.DisableS3();
		}
		Utility.MonitorShutdownEvent();
		UserControl_MainArea userControl_MainArea = (UserControl_MainArea)(Data.MainControl = new UserControl_MainArea());
		((Panel)Grid_MainArea).Children.Add((UIElement)(object)userControl_MainArea);
		((UIElement)Grid_MainArea).Visibility = (Visibility)0;
	}

	private void PreviewKeyDown_Window_FullScreen(object sender, KeyEventArgs e)
	{
		if ((Keyboard.IsKeyDown((Key)120) || Keyboard.IsKeyDown((Key)121)) && Keyboard.IsKeyDown((Key)22))
		{
			((Window)this).WindowState = (WindowState)1;
		}
	}

	private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
	{
		Utility.Logger(LogLevel.Info, "Display settings changed");
		UpdateUI();
	}

	private void UpdateUI()
	{
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		((FrameworkElement)this).Width = SystemParameters.PrimaryScreenWidth;
		((FrameworkElement)this).Height = SystemParameters.PrimaryScreenHeight;
		Utility.Logger(LogLevel.Info, "Current resolution: " + SystemParameters.PrimaryScreenWidth + "*" + SystemParameters.PrimaryScreenHeight);
		string text = "Pictures/" + Brand.Acer.ToString() + "/Backgrounds/Background" + SystemParameters.PrimaryScreenWidth + "x" + SystemParameters.PrimaryScreenHeight + ".jpg";
		Utility.Logger(LogLevel.Info, "Default resource: " + text);
		try
		{
			BitmapImage val;
			if (Utility.IsResourceExist(text))
			{
				val = new BitmapImage(new Uri("pack://application:,,,/AlaunchX;component/" + text));
			}
			else
			{
				text = "Pictures/" + Data.ImageBrand.ToString() + "/Backgrounds/BackgroundDefault.jpg";
				val = new BitmapImage(new Uri("pack://application:,,,/AlaunchX;component/" + text));
			}
			((Panel)Grid_FullScreen).Background = (Brush)new ImageBrush((ImageSource)(object)val);
			Utility.Logger(LogLevel.Info, "Use resource: " + text);
		}
		catch (Exception ex)
		{
			Utility.Logger(LogLevel.Error, ex.ToString());
		}
		((FrameworkElement)Viewbox_MainArea).Margin = new Thickness((((FrameworkElement)this).Width - ((FrameworkElement)Viewbox_MainArea).Width) / 2.0, (((FrameworkElement)this).Height - ((FrameworkElement)Viewbox_MainArea).Height) / 2.0, 0.0, 0.0);
	}

	private void Closing_Window_FullScreen(object sender, CancelEventArgs e)
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
			Uri uri = new Uri("/AlaunchX;component/window_fullscreen.xaml", UriKind.Relative);
			Application.LoadComponent((object)this, uri);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		switch (connectionId)
		{
		case 1:
			((FrameworkElement)(Window_FullScreen)target).Loaded += new RoutedEventHandler(Loaded_Window_FullScreen);
			((UIElement)(Window_FullScreen)target).PreviewKeyDown += new KeyEventHandler(PreviewKeyDown_Window_FullScreen);
			((Window)(Window_FullScreen)target).Closing += Closing_Window_FullScreen;
			break;
		case 2:
			Grid_FullScreen = (Grid)target;
			break;
		case 3:
			Viewbox_MainArea = (Viewbox)target;
			break;
		case 4:
			Grid_MainArea = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
