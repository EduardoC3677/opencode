using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Threading;

namespace AlaunchX;

public class UserControl_BarMode : UserControl, InstallationProcess, IComponentConnector
{
	private double _shiftOffset = 73.0;

	private int _textChangeSeconds = 1;

	private int _rebootRemainingSeconds = 5;

	private Timer textChangeTimer = new Timer();

	private Timer rebootTimer = new Timer();

	private string _rebootString = "After %d seconds, system will reboot automatically.";

	internal Grid Grid_BarModeArea;

	internal Image Image_BarModeImage;

	internal AnimatedGIFControl GIF_Progressing;

	internal TextBlock TextBlock_ProgressText;

	internal TextBlock TextBlock_ProgressNum;

	internal TextBlock TextBlock_InstallModule;

	internal TextBlock TextBlock_Prompt;

	internal TextBlock TextBlock_TextTail;

	internal TextBlock TextBlock_RebootMessage;

	private bool _contentLoaded;

	public UserControl_BarMode()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		InitializeComponent();
		SetupTickTimer();
		textChangeTimer.Start();
		Data.CurrentWorkThread = new Thread(Utility.ProcessModulesInstallation);
		Data.CurrentWorkThread.Start();
	}

	public void UpdateUIforModuleInfo(int totalModuleNum, int currentModuleNum, string moduleName)
	{
		((DispatcherObject)this).Dispatcher.Invoke((DispatcherPriority)9, (Delegate)(Action)delegate
		{
			TextBlock_ProgressNum.Text = currentModuleNum + "/" + totalModuleNum;
			TextBlock_InstallModule.Text = "Install " + moduleName;
		});
	}

	public void TriggerReboot()
	{
		((DispatcherObject)this).Dispatcher.Invoke((DispatcherPriority)9, (Delegate)(Action)delegate
		{
			((UIElement)GIF_Progressing).Visibility = (Visibility)1;
			((UIElement)TextBlock_ProgressText).Visibility = (Visibility)1;
			((UIElement)TextBlock_ProgressNum).Visibility = (Visibility)1;
			((UIElement)TextBlock_InstallModule).Visibility = (Visibility)1;
			((UIElement)TextBlock_Prompt).Visibility = (Visibility)1;
			((UIElement)TextBlock_TextTail).Visibility = (Visibility)1;
			TextBlock_RebootMessage.Text = _rebootString.ToString().Replace("%d", _rebootRemainingSeconds.ToString());
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
			TextBlock_RebootMessage.Text = _rebootString.ToString().Replace("%d", _rebootRemainingSeconds.ToString());
		}
		else
		{
			rebootTimer.Stop();
			Utility.Reboot();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("/AlaunchX;component/usercontrol_barmode.xaml", UriKind.Relative);
			Application.LoadComponent((object)this, uri);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		switch (connectionId)
		{
		case 1:
			Grid_BarModeArea = (Grid)target;
			break;
		case 2:
			Image_BarModeImage = (Image)target;
			break;
		case 3:
			GIF_Progressing = (AnimatedGIFControl)target;
			break;
		case 4:
			TextBlock_ProgressText = (TextBlock)target;
			break;
		case 5:
			TextBlock_ProgressNum = (TextBlock)target;
			break;
		case 6:
			TextBlock_InstallModule = (TextBlock)target;
			break;
		case 7:
			TextBlock_Prompt = (TextBlock)target;
			break;
		case 8:
			TextBlock_TextTail = (TextBlock)target;
			break;
		case 9:
			TextBlock_RebootMessage = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
