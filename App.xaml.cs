using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace VoicevoxEnterPlayer;

public partial class App : Application
{
    private const string MutexName = "VoicevoxEnterPlayer_SingleInstance";
    private Mutex? _singleInstanceMutex;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            ActivateExistingWindow();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (s, args) =>
        {
            args.Handled = true;
            MessageBox.Show($"未処理例外:\n{args.Exception}\n\nスタックトレース:\n{args.Exception.StackTrace}",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static void ActivateExistingWindow()
    {
        var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
        {
            if (process.Id == current.Id || process.MainWindowHandle == IntPtr.Zero)
                continue;

            ShowWindowAsync(process.MainWindowHandle, 9); // SW_RESTORE
            SetForegroundWindow(process.MainWindowHandle);
            break;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}