using System;
using System.IO;
using System.Threading.Tasks;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new FantasyVoxels.MGame();

        if (!Directory.Exists($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/"))
            Directory.CreateDirectory($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/");

		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			Exception ex = args.ExceptionObject as Exception;
			File.WriteAllText($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/log{Guid.NewGuid()}.txt",ex.ToString());
		}; 
		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			File.WriteAllText($"{Environment.GetEnvironmentVariable("profilePath")}/user/logs/log{Guid.NewGuid()}.txt", args.Exception.ToString());
			args.SetObserved();
		};

		game.Run();
    }
}