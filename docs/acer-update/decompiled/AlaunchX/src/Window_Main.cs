using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace AlaunchX;

public class Window_Main : Window, IComponentConnector
{
	internal Viewbox Viewbox_MainArea;

	internal Grid Grid_MainArea;

	private bool _contentLoaded;

	public Window_Main()
	{
		InitializeComponent();
	}

	private void Loaded_Window_Main(object sender, RoutedEventArgs e)
	{
		Utility.Logger(LogLevel.Info, "Window style: Main");
		UserControl_MainArea userControl_MainArea = (UserControl_MainArea)(Data.MainControl = new UserControl_MainArea());
		((Panel)Grid_MainArea).Children.Add((UIElement)(object)userControl_MainArea);
	}

	private void Closing_Window_Main(object sender, CancelEventArgs e)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Invalid comparison between Unknown and I4
		ResourceDictionary val = new ResourceDictionary();
		try
		{
			val.Source = new Uri("..\\StringResources\\StringResources." + Data.StringResourceCulture + ".xaml", UriKind.Relative);
		}
		catch (IOException)
		{
			Utility.Logger(LogLevel.Info, "Cannot find string resource for " + Data.StringResourceCulture + ", use en instead");
			val.Source = new Uri("..\\StringResources\\StringResources.en.xaml", UriKind.Relative);
		}
		((FrameworkElement)this).Resources.MergedDictionaries.Add(val);
		if ((int)MessageBox.Show(((FrameworkElement)this).FindResource((object)"IDS_WARN_CLOSE").ToString(), ((FrameworkElement)this).FindResource((object)"IDS_WARNING").ToString(), (MessageBoxButton)4, (MessageBoxImage)48) == 6)
		{
			Utility.Logger(LogLevel.Warn, "User forces to close AlaunchX");
			if (Data.CurrentWorkThread != null && Data.CurrentWorkThread.IsAlive)
			{
				Utility.Logger(LogLevel.Info, "Thread is still working, abort the working thread");
				Data.CurrentWorkThread.Abort();
			}
		}
		else
		{
			e.Cancel = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("/AlaunchX;component/window_main.xaml", UriKind.Relative);
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
			((FrameworkElement)(Window_Main)target).Loaded += new RoutedEventHandler(Loaded_Window_Main);
			((Window)(Window_Main)target).Closing += Closing_Window_Main;
			break;
		case 2:
			Viewbox_MainArea = (Viewbox)target;
			break;
		case 3:
			Grid_MainArea = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
