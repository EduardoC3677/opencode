using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AlaunchX.Properties;

namespace AlaunchX;

public class AnimatedGIFControl : Image
{
	public delegate void FrameUpdatedEventHandler();

	private Bitmap _bitmap;

	private BitmapSource _bitmapSource;

	[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool DeleteObject(IntPtr hObject);

	protected override void OnInitialized(EventArgs e)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		((FrameworkElement)this).OnInitialized(e);
		((FrameworkElement)this).Loaded += new RoutedEventHandler(AnimatedGIFControl_Loaded);
		((FrameworkElement)this).Unloaded += new RoutedEventHandler(AnimatedGIFControl_Unloaded);
	}

	private void AnimatedGIFControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (Resources.CD != null)
		{
			_bitmap = Resources.CD;
			((FrameworkElement)this).Width = 36.0;
			((FrameworkElement)this).Height = 36.0;
			_bitmapSource = GetBitmapSource();
			((Image)this).Source = (ImageSource)(object)_bitmapSource;
		}
		ImageAnimator.Animate((Image)(object)_bitmap, (EventHandler)OnFrameChanged);
	}

	private void AnimatedGIFControl_Unloaded(object sender, RoutedEventArgs e)
	{
		StopAnimate();
	}

	public void StartAnimate()
	{
		ImageAnimator.Animate((Image)(object)_bitmap, (EventHandler)OnFrameChanged);
	}

	public void StopAnimate()
	{
		ImageAnimator.StopAnimate((Image)(object)_bitmap, (EventHandler)OnFrameChanged);
	}

	private void OnFrameChanged(object sender, EventArgs e)
	{
		try
		{
			((DispatcherObject)this).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)new FrameUpdatedEventHandler(FrameUpdatedCallback));
		}
		catch (Exception)
		{
		}
	}

	private void FrameUpdatedCallback()
	{
		ImageAnimator.UpdateFrames();
		if (_bitmapSource != null)
		{
			((Freezable)_bitmapSource).Freeze();
		}
		_bitmapSource = GetBitmapSource();
		((Image)this).Source = (ImageSource)(object)_bitmapSource;
		((UIElement)this).InvalidateVisual();
	}

	private BitmapSource GetBitmapSource()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = _bitmap.GetHbitmap();
			_bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(intPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				DeleteObject(intPtr);
			}
		}
		return _bitmapSource;
	}
}
