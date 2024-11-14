using GeonBit.UI.Entities;
using GeonBit.UI;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FantasyVoxels.MGame;
using Newtonsoft.Json.Linq;
using System.Threading;
using Newtonsoft.Json.Bson;

namespace FantasyVoxels.Saves
{
    struct EntitySaveData
    {
        public Vector3 position, velocity, rotation;
        public byte health, maxHealth;
        public object customSaveData;
    }
    internal static class Save
    {
        public static string WorldName = "";
        public static string savePath = $"{Environment.GetEnvironmentVariable("profilePath")}/user/saves/";
        public static string backupPath = $"{Environment.GetEnvironmentVariable("profilePath")}/user/backup_saves/";

        public static async Task SaveToFile(string _savename)
        {
            string savename = $"{savePath}{_savename}";

            Chunk[] chunks = [.. MGame.Instance.loadedChunks.Values];

            if(Directory.Exists(savename)) Directory.Delete(savename, true);

            Directory.CreateDirectory(savename);
            Directory.CreateDirectory($"{savename}/chunk");
            Directory.CreateDirectory($"{savename}/entity");
            IList<Task> writeTaskList = new List<Task>();

            foreach (Chunk chunk in chunks)
            {
                int distance = int.Max(int.Max(int.Abs(Instance.playerChunkPos.x - chunk.chunkPos.x), int.Abs(Instance.playerChunkPos.y - chunk.chunkPos.y)), int.Abs(Instance.playerChunkPos.z - chunk.chunkPos.z));
                if ((!chunk.modified && distance > 2) || chunk.CompletelyEmpty) continue;

                //writeTaskList.Add(File.WriteAllBytesAsync($"{savename}/chunk/{chunk.chunkPos.x}_{chunk.chunkPos.y}_{chunk.chunkPos.z}" + (chunk.modified ? "" : "_unmodified") + ".chunk", chunk.voxels));
                writeTaskList.Add(File.WriteAllTextAsync($"{savename}/chunk/{chunk.chunkPos.x}_{chunk.chunkPos.y}_{chunk.chunkPos.z}" + (chunk.modified ? "" : "_unmodified") + ".json", JsonConvert.SerializeObject(chunk)));
            }
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
            writeTaskList.Add(File.WriteAllTextAsync($"{savename}/entity/chunkbound.json", JsonConvert.SerializeObject(EntityManager.loadedEntities, jsonSerializerSettings)));

            await Task.WhenAll(writeTaskList);

            EntitySaveData playerSaveData = new EntitySaveData
            {
                position = (Vector3)MGame.Instance.player.position,
                velocity = MGame.Instance.player.velocity,
                rotation = MGame.Instance.player.rotation,
                health = MGame.Instance.player.health,
                maxHealth = MGame.Instance.player.maxHealth,
                customSaveData = MGame.Instance.player.CaptureCustomSaveData()
            };

            string playerJson = JsonConvert.SerializeObject(playerSaveData);
            await File.WriteAllTextAsync($"{savename}/entity/player.json", playerJson);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"seed:{MGame.Instance.seed}");
            sb.AppendLine($"time:{(int)WorldTimeManager.WorldTime}");

            await File.WriteAllTextAsync($"{savename}/overworld.txt",sb.ToString());

            await Task.Delay(4000);
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
        public static async void LoadSave(string _savename)
        {
            Instance.currentPlayState = PlayState.LoadingWorld;

            string savename = $"{savePath}{_savename}";
            string backupname = $"{backupPath}{_savename}";
            if (!Directory.Exists(savename)) return;

            var label = new Label("Loading World...", Anchor.Center);
            UserInterface.Active.AddEntity(label);

            if (Directory.Exists(backupname)) Directory.Delete(backupname, true);
            CopyFilesRecursively(savename, backupname);

            WorldName = _savename;

            string overworlddata = await File.ReadAllTextAsync($"{savename}/overworld.txt");
            var lines = overworlddata.Split('\n');
            MGame.Instance.seed = int.Parse((Array.Find(lines, e => e.StartsWith("seed:")) ?? "seed:0").Split(':')[1]);
            WorldTimeManager.SetWorldTime(int.Parse((Array.Find(lines, e => e.StartsWith("time:")) ?? "time:0").Substring(5)));

            Instance.worldRandom = new Random(Instance.seed);

            MGame.Instance.loadedChunks = new ConcurrentDictionary<long, Chunk>();

            await MGame.Instance.LoadWorld(true);

            await Parallel.ForEachAsync(Directory.GetFiles($"{savename}/chunk"), async (string chunkfile, CancellationToken token) =>
            {
                string chunkfilename = Path.GetFileNameWithoutExtension(chunkfile);

                string[] coords = chunkfilename.Split('_');
                int chunkx = int.Parse(coords[0]);
                int chunky = int.Parse(coords[1]);
                int chunkz = int.Parse(coords[2]);

                Chunk chunk = JsonConvert.DeserializeObject<Chunk>(await File.ReadAllTextAsync(chunkfile));

                chunk.chunkPos = (chunkx, chunky, chunkz);

                //Dont just forget about the chunk (like old versions did), just modify it instead. Easier, no?
                if (!MGame.Instance.loadedChunks.TryAdd(MGame.CCPos(chunk.chunkPos), chunk))
                {
                    MGame.Instance.loadedChunks[MGame.CCPos(chunk.chunkPos)] = chunk;
                }

                chunk.generated = true;

                //chunk.modified = !chunkfilename.Contains("unmodified");

                Array.Fill(chunk.meshLayer, true);

                chunk.GenerateVisibility();
                chunk.Remesh();

                chunk.meshUpdated = [false, false, false, false];
            });

            EntityManager.loadedEntities.Clear();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
            if (File.Exists($"{savename}/entity/chunkbound.json"))
            {
                EntityManager.loadedEntities = JsonConvert.DeserializeObject<Dictionary<long,List<Entity>>>(await File.ReadAllTextAsync($"{savename}/entity/chunkbound.json"), jsonSerializerSettings);
                foreach(var lst in EntityManager.loadedEntities)
                {
                    foreach(var entity in lst.Value)
                    {
                        entity.Start();
                    }
                }
            }

            EntitySaveData playerData = JsonConvert.DeserializeObject<EntitySaveData>(File.ReadAllText($"{savename}/entity/player.json"));

            RestoreEntity(MGame.Instance.player,playerData);

            UserInterface.Active.RemoveEntity(label);

            Instance.currentPlayState = PlayState.World;
        }
        static void RestoreEntity(Entity entity, EntitySaveData data)
        {
            entity.position = data.position;
            entity.velocity = data.velocity;
            entity.rotation = data.rotation;
            entity.maxHealth = data.maxHealth;
            entity.health = data.health;
            if (data.customSaveData != null) entity.RestoreCustomSaveData((JObject)data.customSaveData);
        }
        public static void DeleteSave(string _savename)
        {
            string savename = $"{savePath}{_savename}";
            if (!Directory.Exists(savename)) return;
            Directory.Delete(savename,true);
        }
        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
        public static string ToBson<T>(T value)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BsonDataWriter datawriter = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(datawriter, value);
                return Convert.ToBase64String(ms.ToArray());
            }

        }

        public static T FromBson<T>(string base64data)
        {
            byte[] data = Convert.FromBase64String(base64data);

            using (MemoryStream ms = new MemoryStream(data))
            using (BsonDataReader reader = new BsonDataReader(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }
    }
}
