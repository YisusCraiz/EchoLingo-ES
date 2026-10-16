using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CsTranslate.App.Interop;

/// <summary>
/// Toggles WS_EX_TRANSPARENT on a window so mouse input passes through to
/// whatever is behind it (the game). Edit mode removes the flag temporarily so
/// the overlay can be dragged to reposition it.
/// </summary>
internal static class ClickThrough
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public static void SetEnabled(Window window, bool enabled)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        int style = GetWindowLong(handle, GwlExstyle);

        style = enabled
            ? style | WsExTransparent | WsExLayered
            : style & ~WsExTransparent;

        SetWindowLong(handle, GwlExstyle, style);
    }
}
