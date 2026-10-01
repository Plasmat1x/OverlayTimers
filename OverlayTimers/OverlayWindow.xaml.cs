using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OverlayTimers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private DispatcherTimer _timer;
        private int _secondsEleapsed = 0;

        private Dictionary<int, ActiveTimerData> _activeTimers = new();

        private class ActiveTimerData
        {
            public TimerSettings Settings { get; set; }
            public TextBlock UIElement { get; set; }
            public DispatcherTimer Timer { get; set; }
            public DateTime EndTime { get; set; }
            public bool IsRunning { get; set; }
        }

        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OverlayWindow_Loaded;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        public void SetupTimers(AppConfig config)
        {
            int idCounter = 1;

            foreach(var tSetting in config.Timers)
            {
                if(string.IsNullOrEmpty(tSetting.HotkeyStr))
                    continue;

                int currentId = idCounter++;

                if(_activeTimers.ContainsKey(currentId))
                {
                    var existing = _activeTimers[currentId];
                    existing.Settings = tSetting;

                    Canvas.SetLeft(existing.UIElement, tSetting.X);
                    Canvas.SetTop(existing.UIElement, tSetting.Y);

                    continue;
                }

                TextBlock txt = new TextBlock
                {
                    Text = "",
                    Foreground = System.Windows.Media.Brushes.Lime,
                    FontSize = 26,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(5),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0, 0, 0))
                };

                Canvas.SetLeft(txt, tSetting.X);
                Canvas.SetTop(txt, tSetting.Y);
                OverlayCanvas.Children.Add(txt);

                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };

                var timerData = new ActiveTimerData
                {
                    Settings = tSetting,
                    UIElement = txt,
                    Timer = timer,
                };

                int capturedId = currentId;
                timer.Tick += (s, e) => {
                    var data = _activeTimers[capturedId];
                    double secondsLeft = (data.EndTime - DateTime.Now).TotalSeconds;


                    if(secondsLeft <= 0)
                    {
                        data.Timer.Stop();
                        data.IsRunning = false;
                        data.UIElement.Text = "";
                    }
                    else
                    {
                        data.UIElement.Text = $"{data.Settings.Name}: {secondsLeft.ToString("F1")} с";
                    }
                };

                _activeTimers.Add(currentId, timerData);
            }
        }

        public void TriggerTimerByHotkeyId(int id)
        {
            if(_activeTimers.ContainsKey(id))
            {
                var data = _activeTimers[id];
                if(data.IsRunning)
                    return;

                data.EndTime = DateTime.Now.AddSeconds(data.Settings.DurationSeconds);
                data.IsRunning = true;
                data.UIElement.Text = $"{data.Settings.Name}: {data.Settings.DurationSeconds.ToString("F1")} с";

                data.Timer.Start();
            }
        }
    }
}