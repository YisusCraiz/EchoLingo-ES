using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CsTranslate.App.Interop;
using CsTranslate.App.ViewModels;

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

    private bool _editMode;
    private HwndSource? _hwndSource;

    public OverlayViewModel ViewModel { get; }

    public OverlayWindow(OverlayViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) =>
        {
            if (_hwndSource is not null)
            {
                UnregisterHotKey(_hwndSource.Handle, EditModeHotkeyId);
                _hwndSource.RemoveHook(WndProc);
            }
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ClickThrough.SetEnabled(this, enabled: true);

        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _hwndSource?.AddHook(WndProc);
        RegisterHotKey(_hwndSource!.Handle, EditModeHotkeyId, 0, VkF9);
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
