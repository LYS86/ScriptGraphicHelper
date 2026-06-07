using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Newtonsoft.Json;
using ScriptGraphicHelper.Models;
using ScriptGraphicHelper.Models.UnmanagedMethods;
using ScriptGraphicHelper.ViewModels;
using System;
using System.ComponentModel;
using System.IO;

namespace ScriptGraphicHelper.Views
{
    public class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public IntPtr Handle { get; private set; }

        public MainWindow()
        {
            this.ExtendClientAreaToDecorationsHint = true;
            this.ExtendClientAreaTitleBarHeightHint = -1;
            this.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            Instance = this;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FontWeight = Avalonia.Media.FontWeight.Medium;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private DispatcherTimer Timer = new();

        private double currentPixelDensity = 1.0;

        /// <summary>
        /// 根据屏幕工作区和 DPI 缩放因子计算窗口安全尺寸。
        /// Avalonia 0.10.x 在某些 DPI 模式下 Bounds/WorkingArea 返回物理像素，
        /// 需通过 PixelDensity 换算为有效 DIP 后再做限制。
        /// </summary>
        private Size ClampToScreen(double desiredWidth, double desiredHeight, PixelRect workingArea)
        {
            var effectiveWidth = workingArea.Width / this.currentPixelDensity;
            var effectiveHeight = workingArea.Height / this.currentPixelDensity;

            const double margin = 40;
            var maxWidth = Math.Max(800, effectiveWidth - margin);
            var maxHeight = Math.Max(500, effectiveHeight - margin);

            const double minWidth = 1024;
            const double minHeight = 600;

            var clampedWidth = Math.Max(minWidth, Math.Min(desiredWidth, maxWidth));
            var clampedHeight = Math.Max(minHeight, Math.Min(desiredHeight, maxHeight));

            return new Size(clampedWidth, clampedHeight);
        }

        /// <summary>
        /// 修正窗口在屏幕中的位置。
        /// WindowStartupLocation.CenterScreen 使用物理像素坐标计算但窗口尺寸为 DIP，
        /// 在高 DPI 缩放下会导致位置偏移。此处用物理像素重算确保四边可见。
        /// </summary>
        private void EnsureWindowPositionInScreen(PixelRect workingArea, Size dipSize)
        {
            var physicalW = dipSize.Width * this.currentPixelDensity;
            var physicalH = dipSize.Height * this.currentPixelDensity;

            var targetX = workingArea.X + (workingArea.Width - physicalW) / 2;
            var targetY = workingArea.Y + (workingArea.Height - physicalH) / 2;

            targetX = Math.Max(workingArea.X, targetX);
            targetY = Math.Max(workingArea.Y, targetY);

            this.Position = new PixelPoint((int)targetX, (int)targetY);
        }

        private void Window_Opened(object sender, EventArgs e)
        {
            // 拖放事件 (拖动图片到窗口,可以快速打开图片)
            AddHandler(DragDrop.DropEvent, (this.DataContext as MainWindowViewModel).DropImage_Event);
            
            this.Handle = this.PlatformImpl.Handle.Handle;

            // 获取 DPI 缩放因子 (Avalonia 0.10.x 通过反射获取)
            try
            {
                var prop = this.Screens.Primary.GetType().GetProperty("PixelDensity");
                if (prop != null)
                    this.currentPixelDensity = (double)prop.GetValue(this.Screens.Primary);
            }
            catch { }

            // 根据屏幕工作区动态适配窗口尺寸
            var workingArea = this.Screens.Primary.WorkingArea;
            var clampedSize = ClampToScreen(Settings.Instance.Width, Settings.Instance.Height, workingArea);
            this.ClientSize = clampedSize;

            // 同步 ViewModel 和 Settings
            var vm = this.DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.WindowWidth = clampedSize.Width;
                vm.WindowHeight = clampedSize.Height;
            }
            Settings.Instance.Width = clampedSize.Width;
            Settings.Instance.Height = clampedSize.Height;

            // 修正窗口位置 (解决 DPI 缩放时 CenterScreen 坐标系不匹配)
            EnsureWindowPositionInScreen(workingArea, clampedSize);

            // 设置顶部提示的关闭倒计时
            this.Timer.Tick += new EventHandler(HintMessage_Closed);
            this.Timer.Interval = new TimeSpan(0, 0, 3);
            this.Timer.Start();
        }

        private void HintMessage_Closed(object? sender, EventArgs e)
        {
            var hint = this.FindControl<Border>("HintMessage");
            hint.IsVisible = false;
            this.Timer.IsEnabled = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.WindowState != WindowState.FullScreen)
            {
                Settings.Instance.Width = this.Width;
                Settings.Instance.Height = this.Height;
            }
            var settingStr = JsonConvert.SerializeObject(Settings.Instance, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"assets\settings.json", settingStr);

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            switch (key)
            {
                case Key.Left: NativeApi.Move2Left(); break;
                case Key.Up: NativeApi.Move2Top(); break;
                case Key.Right: NativeApi.Move2Right(); break;
                case Key.Down: NativeApi.Move2Bottom(); break;
                default: return;
            }
            e.Handled = true;
        }

        private void TitleBar_DragMove(object sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void Minsize_Tapped(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Info_Tapped(object sender, RoutedEventArgs e)
        {
            var info = new Info();
            info.ShowDialog(this);
            // this.WindowState = WindowState.Minimized;
        }

        private double defaultWidth;
        private double defaultHeight;
        private void WindowStateChange_Tapped(object sender, RoutedEventArgs e)
        {
            this.CanResize = true;
            var default_btn = this.FindControl<Button>("Default_btn");
            var fullScreen_btn = this.FindControl<Button>("FullScreen_btn");
            if (this.WindowState == WindowState.FullScreen)
            {
                default_btn.IsVisible = false;
                fullScreen_btn.IsVisible = true;
                this.WindowState = WindowState.Normal;

                this.Width = this.defaultWidth;
                this.Height = this.defaultHeight;
                var workingAreaSize = this.Screens.Primary.WorkingArea.Size;
                this.Position = new PixelPoint((int)((workingAreaSize.Width - this.Width) / 2), (int)((workingAreaSize.Height - this.Height) / 2));
            }
            else
            {
                this.defaultWidth = this.Width;
                this.defaultHeight = this.Height;
                default_btn.IsVisible = true;
                fullScreen_btn.IsVisible = false;
                this.WindowState = WindowState.FullScreen;
            }
        }

        private void Close_Tapped(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
