using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FuckMLA;

// We are manually checking if the window is focused because Unity is cool:
// Application.isFocused is true on startup, even when launched in background
// Application.focusChanged & MonoBehaviour.OnApplicationFocus is only called on second focus
// :)))))))))))))))

public static class WindowFocusManager
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // [DllImport("user32.dll", SetLastError = true)] // detected melon console, that is stinky >:(
    // private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static bool lastFocusState;
    private static IntPtr mainWindowHandle;
    public static Action<bool> OnFocusStateChanged;

    static WindowFocusManager()
    {
        Initialize();
    }

    private static async void Initialize()
    {
        //await Task.Delay(1000); // delay to ensure the main window handle is available
        Process process = Process.GetCurrentProcess();
        mainWindowHandle = process.MainWindowHandle;
        lastFocusState = IsWindowFocused();
    }

    private static bool IsWindowFocused()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        return foregroundWindow == mainWindowHandle;
    }

    public static void CheckWindowFocusedState()
    {
        bool currentFocusState = IsWindowFocused();
        if (currentFocusState == lastFocusState) 
            return;
        
        lastFocusState = currentFocusState;
        OnFocusStateChanged?.Invoke(currentFocusState);
    }
}