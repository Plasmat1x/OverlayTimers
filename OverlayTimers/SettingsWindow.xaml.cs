using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OverlayTimers;
/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow: Window
{
    private AppConfig _config;

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;

        ConfigKeyTextBox.Text = _config.ConfigHotkey;
        TimersGrid.ItemsSource = new ObservableCollection<TimerSettings>(_config.Timers);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _config.ConfigHotkey = ConfigKeyTextBox.Text.ToUpper();

        var updatedList = TimersGrid.ItemsSource as ObservableCollection<TimerSettings>;
        if(updatedList != null)
        {
            foreach(var timer in updatedList)
            {
                if(!string.IsNullOrEmpty(timer.HotkeyStr))
                {
                    timer.HotkeyStr = timer.HotkeyStr.ToUpper();
                    timer.VirtualKey = (uint)KeyInterop.VirtualKeyFromKey((System.Windows.Input.Key)Enum.Parse(typeof(System.Windows.Input.Key), timer.HotkeyStr));
                }
            }
            _config.Timers = new System.Collections.Generic.List<TimerSettings>(updatedList);
        }
        _config.Save();
        this.Close();
    }
}
