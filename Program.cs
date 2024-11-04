
using System;
using System.IO;

using var game = new FantasyVoxels.MGame();

if (!Directory.Exists(Environment.ExpandEnvironmentVariables($"%appdata%/FantasyVoxels/Logs")))
    Directory.CreateDirectory(Environment.ExpandEnvironmentVariables($"%appdata%/FantasyVoxels/Logs"));

game.Run();