using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CsTranslate.App.Interop;
using CsTranslate.App.ViewModels;
using CsTranslate.Core.Models;
using CsTranslate.Core.Settings;

namespace CsTranslate.App.Views;

public partial class OverlayWindow : Window
{
    private const int WmHotkey = 0x0312;
    private const int EditModeHotkeyId = 1;
    private const uint VkF9 = 0x78;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _saveTimer;
    private bool _editMode;
    private HwndSource? _hwndSource;

    public OverlayViewModel ViewModel { get; }

    public OverlayWindow(OverlayViewModel viewModel, AppSettings settings, SettingsService settingsService)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;

        InitializeComponent();
        DataContext = ViewModel;
        SourceInitialized += OnSourceInitialized;

        Width = Clamp(_settings.Overlay.Width, MinWidth, MaxWidth);
        Height = Clamp(_settings.Overlay.Height, MinHeight, MaxHeight);
        Opacity = Math.Clamp(_settings.Overlay.Opacity, 0.25, 1.0);
        Left = ClampPosition(_settings.Overlay.X, SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
        Top = ClampPosition(_settings.Overlay.Y, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);

        OverlayPlaceholder.Visibility = ViewModel.Lines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ViewModel.Lines.CollectionChanged += OnLinesChanged;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveOverlaySettings();
        };

        SizeChanged += (_, _) => QueueSettingsSave();
        LocationChanged += (_, _) => QueueSettingsSave();
        Closed += (_, _) =>
        {
            _saveTimer.Stop();
            SaveOverlaySettings();
            ViewModel.Lines.CollectionChanged -= OnLinesChanged;
            if (_hwndSource is not null)
            {
                UnregisterHotKey(_hwndSource.Handle, EditModeHotkeyId);
                _hwndSource.RemoveHook(WndProc);
            }
        };

        Loaded += (_, _) =>
        {
            if (ViewModel.Lines.Count > 0)
            {
                Visibility = Visibility.Visible;
            }
        };
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : minimum;

    private static double ClampPosition(double value, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, Math.Max(minimum, maximum)) : minimum;

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Visibility = ViewModel.Lines.Count == 0 ? Visibility.Hidden : Visibility.Visible;
    }

    private void QueueSettingsSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveOverlaySettings()
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        _settings.Overlay.Width = Width;
        _settings.Overlay.Height = Height;
        _settings.Overlay.X = Left;
        _settings.Overlay.Y = Top;
        _settings.Overlay.Opacity = Opacity;
        try
        {
            _settingsService.Save(_settings);
        }
        catch (IOException)
        {
            // Keep the subtitle overlay responsive if settings cannot be written temporarily.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the subtitle overlay responsive if settings cannot be written temporarily.
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ClickThrough.SetEnabled(this, enabled: true);

        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _hwndSource?.AddHook(WndProc);
        if (_hwndSource is not null)
        {
            RegisterHotKey(_hwndSource.Handle, EditModeHotkeyId, 0, VkF9);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == EditModeHotkeyId)
        {
            ToggleEditMode();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void ShowPreview()
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width, Math.Max(MinWidth, area.Width - 32));
        Height = Math.Min(Height, Math.Max(MinHeight, area.Height - 32));
        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, area.Height - Height - 32);
        Show();
        if (!_editMode)
        {
            ToggleEditMode();
        }
    }

    public void ToggleEditMode()
    {
        _editMode = !_editMode;
        ClickThrough.SetEnabled(this, enabled: !_editMode);
        EditModeHint.Visibility = _editMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_editMode && e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
