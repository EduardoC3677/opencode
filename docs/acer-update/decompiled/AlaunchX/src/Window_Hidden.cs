using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace AlaunchX;

public class Window_Hidden : Window, IComponentConnector
{
	private bool _contentLoaded;

	public Window_Hidden()
	{
		InitializeComponent();
	}

	private void Loaded_Window_Hidden(object sender, RoutedEventArgs e)
	{
		Utility.Logger(LogLevel.Info, "Window style: Hidden");
		Data.CurrentWorkThread = new Thread(Utility.ProcessModulesInstallation);
		Data.CurrentWorkThread.Start();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("/AlaunchX;component/window_hidden.xaml", UriKind.Relative);
			Application.LoadComponent((object)this, uri);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		if (connectionId == 1)
		{
			((FrameworkElement)(Window_Hidden)target).Loaded += new RoutedEventHandler(Loaded_Window_Hidden);
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
