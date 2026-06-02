using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AlaunchX;

public class UserControl_MainArea : UserControl, InstallationProcess, IComponentConnector
{
	private int _textChangeSeconds = 1;

	private int _rebootRemainingSeconds = 5;

	private Timer textChangeTimer = new Timer();

	private Timer rebootTimer = new Timer();

	private TimeSpan duration = TimeSpan.FromSeconds(1.0);

	internal Grid Grid_MainArea;

	internal TextBlock TextBlock_Caption;

	internal Image Image_InstallationIcon;

	internal StackPanel StackPanel_InstallModule;

	internal TextBlock TextBlock_Installing;

	internal TextBlock TextBlock_TextTail;

	internal ProgressBar ProgressBar_InstallationProgress;

	internal TextBlock TextBlock_ProgressNum;

	internal TextBlock TextBlock_Prompt;

	internal TextBlock TextBlock_RebootMessage;

	private bool _contentLoaded;

	public UserControl_MainArea()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		InitializeComponent();
		try
		{
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
			TextBlock_Caption.Text = ((FrameworkElement)this).FindResource((object)"IDS_MUI_CAPTION").ToString();
			TextBlock_Installing.Text = ((FrameworkElement)this).FindResource((object)"IDS_MUI_INSTALL").ToString();
			TextBlock_Prompt.Text = ((FrameworkElement)this).FindResource((object)"IDS_MUI_PROMPT").ToString();
			SetupTickTimer();
			textChangeTimer.Start();
			Data.CurrentWorkThread = new Thread(Utility.ProcessModulesInstallation);
			Data.CurrentWorkThread.Start();
		}
		catch (Exception ex2)
		{
			Utility.Logger(LogLevel.Error, ex2.ToString());
		}
	}

	public void UpdateUIforModuleInfo(int totalModuleNum, int currentModuleNum, string moduleName)
	{
		((DispatcherObject)this).Dispatcher.Invoke((DispatcherPriority)9, (Delegate)(Action)delegate
		{
			TextBlock_ProgressNum.Text = currentModuleNum + "/" + totalModuleNum;
			TextBlock_Installing.Text = ((FrameworkElement)this).FindResource((object)"IDS_MUI_INSTALL").ToString().Replace("%ModuleName%", moduleName);
			SetPercent(ProgressBar_InstallationProgress, (double)currentModuleNum / (double)totalModuleNum * 100.0);
		});
	}

	public void TriggerReboot()
	{
		((DispatcherObject)this).Dispatcher.Invoke((DispatcherPriority)9, (Delegate)(Action)delegate
		{
			TextBlock_RebootMessage.Text = ((FrameworkElement)this).FindResource((object)"IDS_MUI_REBOOT_MSG").ToString().Replace("%d", _rebootRemainingSeconds.ToString());
			((UIElement)TextBlock_RebootMessage).Visibility = (Visibility)0;
			rebootTimer.Start();
		});
	}

	private void SetupTickTimer()
	{
		textChangeTimer.Interval = 1000;
		textChangeTimer.Tick += TextChangeTimer_Tick;
		rebootTimer.Interval = 1000;
		rebootTimer.Tick += RebootTimer_Tick;
		_rebootRemainingSeconds = Data.RebootTimeout;
	}

	private void TextChangeTimer_Tick(object sender, EventArgs e)
	{
		_textChangeSeconds %= 4;
		switch (_textChangeSeconds)
		{
		case 1:
			TextBlock_TextTail.Text = ".";
			break;
		case 2:
			TextBlock_TextTail.Text = "..";
			break;
		case 3:
			TextBlock_TextTail.Text = "...";
			break;
		default:
			TextBlock_TextTail.Text = "";
			_textChangeSeconds = 0;
			break;
		}
		_textChangeSeconds++;
	}

	private void RebootTimer_Tick(object sender, EventArgs e)
	{
		if (_rebootRemainingSeconds > 0)
		{
			_rebootRemainingSeconds--;
			TextBlock_RebootMessage.Text = ((FrameworkElement)this).FindResource((object)"IDS_MUI_REBOOT_MSG").ToString().Replace("%d", _rebootRemainingSeconds.ToString());
		}
		else
		{
			rebootTimer.Stop();
			Utility.Reboot();
		}
	}

	public void SetPercent(ProgressBar progressBar, double percentage)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		DoubleAnimation val = new DoubleAnimation(percentage, Duration.op_Implicit(duration));
		((UIElement)progressBar).BeginAnimation(RangeBase.ValueProperty, (AnimationTimeline)(object)val);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("/AlaunchX;component/usercontrol_mainarea.xaml", UriKind.Relative);
			Application.LoadComponent((object)this, uri);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		switch (connectionId)
		{
		case 1:
			Grid_MainArea = (Grid)target;
			break;
		case 2:
			TextBlock_Caption = (TextBlock)target;
			break;
		case 3:
			Image_InstallationIcon = (Image)target;
			break;
		case 4:
			StackPanel_InstallModule = (StackPanel)target;
			break;
		case 5:
			TextBlock_Installing = (TextBlock)target;
			break;
		case 6:
			TextBlock_TextTail = (TextBlock)target;
			break;
		case 7:
			ProgressBar_InstallationProgress = (ProgressBar)target;
			break;
		case 8:
			TextBlock_ProgressNum = (TextBlock)target;
			break;
		case 9:
			TextBlock_Prompt = (TextBlock)target;
			break;
		case 10:
			TextBlock_RebootMessage = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
