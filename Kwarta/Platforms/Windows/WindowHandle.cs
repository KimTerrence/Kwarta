namespace Kwarta;

static class WindowHandle
{
    /// <summary>The main window's handle, which WinRT pickers need in an unpackaged app.</summary>
    public static IntPtr Current =>
        ((MauiWinUIWindow)Application.Current!.Windows[0].Handler!.PlatformView!).WindowHandle;

    public static T Initialize<T>(T picker) where T : class
    {
        WinRT.Interop.InitializeWithWindow.Initialize(picker, Current);
        return picker;
    }
}
