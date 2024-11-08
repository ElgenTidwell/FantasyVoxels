using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Saves
{
    struct EntitySaveData
    {
        public Vector3 position, velocity, rotation;
    }
    internal static class Save
    {
        public static string WorldName = "";
        public static string savePath = $"{Environment.GetEnvironmentVariable("profilePath")}/user/saves/";
        public static void SaveToFile(string _savename)
        {
            string savename = $"{savePath}{_savename}";

            Chunk[] chunks = MGame.Instance.loadedChunks.Values.ToArray();

            if(Directory.Exists(savename)) Directory.Delete(savename, true);

            Directory.CreateDirectory(savename);
            Directory.CreateDirectory($"{savename}/chunk");
            Directory.CreateDirectory($"{savename}/entity");

            foreach (Chunk chunk in chunks)
            {
                if (!chunk.modified) continue;

                File.WriteAllBytes($"{savename}/chunk/{chunk.chunkPos.x}_{chunk.chunkPos.y}_{chunk.chunkPos.z}.chunk",chunk.voxels);
            }

            EntitySaveData playerSaveData = new EntitySaveData
            {
                position = MGame.Instance.player.position,
                velocity = MGame.Instance.player.velocity,
                rotation = MGame.Instance.player.rotation,
            };

            string playerJson = JsonConvert.SerializeObject(playerSaveData);
            File.WriteAllText($"{savename}/entity/player.json", playerJson);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"seed:{MGame.Instance.seed}");
            sb.AppendLine($"time:{(int)WorldTimeManager.WorldTime}");

            File.WriteAllText($"{savename}/overworld.txt",sb.ToString());
        }
        public static string[] GetAllSavedWorlds()
        {
            if (!Directory.Exists(savePath)) return null;

            List<string> savenames = new List<string>();

            foreach(var file in Directory.GetDirectories(savePath,"*",SearchOption.TopDirectoryOnly))
            {
                savenames.Add(Path.GetFileNameWithoutExtension(file));
            }

            return savenames.ToArray();
        }
        public static void LoadSave(string _savename)
        {
            string savename = $"{savePath}{_savename}";
            if (!Directory.Exists(savename)) return;

            WorldName = _savename;

            string overworlddata = File.ReadAllText($"{savename}/overworld.txt");
            var lines = overworlddata.Split('\n');
            MGame.Instance.seed = int.Parse(Array.Find(lines, e => e.StartsWith("seed:")).Split(':')[1]);
            WorldTimeManager.SetWorldTime(int.Parse((Array.Find(lines, e => e.StartsWith("time:")) ?? "time:0").Substring(5)));

            foreach(var chunkfile in Directory.GetFiles($"{savename}/chunk"))
            {
                string chunkfilename = Path.GetFileNameWithoutExtension(chunkfile);

                string[] coords = chunkfilename.Split('_');
                int chunkx = int.Parse(coords[0]);
                int chunky = int.Parse(coords[1]);
                int chunkz = int.Parse(coords[2]);

                Chunk chunk = new Chunk();
                chunk.chunkPos = (chunkx, chunky, chunkz);

                chunk.generated = true;

                chunk.voxels = File.ReadAllBytes(chunkfile);
                chunk.MaxY = Chunk.Size-1;
                chunk.modified = true;

                Array.Fill(chunk.meshLayer,true);

                chunk.meshUpdated = [false, false, false, false];

                chunk.GenerateVisibility();

                MGame.Instance.loadedChunks.TryAdd(MGame.CCPos(chunk.chunkPos),chunk);
            }

            MGame.Instance.LoadWorld();

            EntitySaveData playerData = JsonConvert.DeserializeObject<EntitySaveData>(File.ReadAllText($"{savename}/entity/player.json"));

            MGame.Instance.player.position = playerData.position;
            MGame.Instance.player.velocity = playerData.velocity;
            MGame.Instance.player.rotation = playerData.rotation;
        }
        public static void DeleteSave(string _savename)
        {
            string savename = $"{savePath}{_savename}";
            if (!Directory.Exists(savename)) return;
            Directory.Delete(savename,true);
        }
    }
}
