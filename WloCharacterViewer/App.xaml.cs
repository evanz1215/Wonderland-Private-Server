using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WloCharacterViewer.Network;

namespace WloCharacterViewer;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            ClientLogger.Initialize();

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }
        catch (Exception ex)
        {
            WriteCrash($"[OnStartup] {ex}");
            MessageBox.Show(ex.ToString(), "Startup Error");
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = $"[CRASH-UI] {e.Exception.GetType().Name}: {e.Exception.Message}\n{e.Exception.StackTrace}";
        if (e.Exception.InnerException != null)
            msg += $"\nInner: {e.Exception.InnerException.Message}\n{e.Exception.InnerException.StackTrace}";

        ClientLogger.Log(msg);
        WriteCrash(msg);
        MessageBox.Show(msg, "Unhandled Error");
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var msg = $"[CRASH-DOMAIN] {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}";
        ClientLogger.Log(msg);
        WriteCrash(msg);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ClientLogger.Log($"[CRASH-TASK] {e.Exception?.GetType().Name}: {e.Exception?.Message}");
        e.SetObserved();
    }

    private static void WriteCrash(string msg)
    {
        try { File.AppendAllText(CrashLogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); }
        catch { }
    }
}
