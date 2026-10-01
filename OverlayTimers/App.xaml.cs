using H.NotifyIcon;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace OverlayTimers
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private OverlayWindow _overlay;
        private SettingsWindow _settingsWin;
        private AppConfig _config;
        private HwndSource _source;
        private IntPtr _overlayHwnd;

        private TaskbarIcon _trayIcon;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const int CONFIG_HOTKEY_ID = 9999;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _config = AppConfig.Load();

            _overlay = new OverlayWindow();
            _overlay.Show();
            _overlay.SetupTimers(_config);

            _overlayHwnd = new WindowInteropHelper(_overlay).Handle;
            _source = HwndSource.FromHwnd(_overlayHwnd);
            _source.AddHook(HwndHook);

            _proc = HookCallback;
            using(Process curProcess = Process.GetCurrentProcess())
            using(ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            RegisterConfigHotkey();

            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

            var menu = new ContextMenu();
            var itemSettings = new MenuItem { Header = "Настройки", FontWeight = FontWeights.Bold };
            itemSettings.Click += (s, a) => ToggleSettingsWindow();

            var itemExit = new MenuItem { Header = "Выход" };
            itemExit.Click += (s, a) => Shutdown();

            menu.Items.Add(itemSettings);
            menu.Items.Add(new Separator());
            menu.Items.Add(itemExit);

            _trayIcon.ContextMenu = menu;
            _trayIcon.TrayMouseDoubleClick += (s, a) => ToggleSettingsWindow();

            _trayIcon.ForceCreate();
        }

        private void RegisterConfigHotkey()
        {
            UnregisterHotKey(_overlayHwnd, CONFIG_HOTKEY_ID);
            if(!string.IsNullOrEmpty(_config.ConfigHotkey))
            {
                uint configVk = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), _config.ConfigHotkey));
                RegisterHotKey(_overlayHwnd, CONFIG_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, configVk);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if(nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                int id = 1;
                foreach(var timer in _config.Timers)
                {
                    if(timer.VirtualKey == (uint)vkCode)
                    {
                        _overlay.TriggerTimerByHotkeyId(id);
                    }
                    id++;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if(msg == WM_HOTKEY && wp.ToInt32() == CONFIG_HOTKEY_ID)
            {
                ToggleSettingsWindow();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            ToggleSettingsWindow();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Shutdown();
        }

        private void ToggleSettingsWindow()
        {
            if(_settingsWin != null && _settingsWin.IsLoaded)
            {
                _settingsWin.Close();
                _settingsWin = null;
                return;
            }

            _settingsWin = new SettingsWindow(_config);
            _settingsWin.Closed += (s, e) => {
                _config = AppConfig.Load();
                _overlay.SetupTimers(_config);
                RegisterConfigHotkey();
                _settingsWin = null;
            };

            _settingsWin.Show();
            _settingsWin.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();

            UnhookWindowsHookEx(_hookId);
            UnregisterHotKey(_overlayHwnd, CONFIG_HOTKEY_ID);
            _source.RemoveHook(HwndHook);
            base.OnExit(e);
        }
    }

}
