using log4net;
using System;
using System.IO;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new FantasyVoxels.MGame();

        if (!Directory.Exists($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/"))
            Directory.CreateDirectory($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/");

        AppDomain currentDomain = default(AppDomain);
        currentDomain = AppDomain.CurrentDomain;
        // Handler for unhandled exceptions.
        currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

        game.Run();
    }
    private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = default(Exception);
        ex = (Exception)e.ExceptionObject;
        File.WriteAllText($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/crash{Environment.TickCount.GetHashCode()}.txt", ex.Message + "\n" + ex.StackTrace);
    }
}