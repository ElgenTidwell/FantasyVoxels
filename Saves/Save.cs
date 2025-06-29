﻿using GeonBit.UI.Entities;
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
using MessagePack;
using FantasyVoxels.Blocks;

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

        public static void SaveToFile(string _savename)
        {
            string savename = $"{savePath}{_savename}";

            Chunk[] chunks = [.. MGame.Instance.loadedChunks.Values];

            if(Directory.Exists(savename)) Directory.Delete(savename, true);

            Directory.CreateDirectory(savename);
            Directory.CreateDirectory($"{savename}/chunk");
            Directory.CreateDirectory($"{savename}/block");
            Directory.CreateDirectory($"{savename}/entity");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"seed:{MGame.Instance.seed}");
            sb.AppendLine($"time:{(int)WorldTimeManager.WorldTime}");

            File.WriteAllText($"{savename}/overworld.txt", sb.ToString());

            int cx = (int)double.Floor(Instance.player.position.X / Chunk.Size);
            int cz = (int)double.Floor(Instance.player.position.Z / Chunk.Size);

            var array = MGame.Instance.loadedChunks.ToArray();

            IList<Task> writeTaskList = new List<Task>();
            List<KeyValuePair<long,Chunk>> chunksToSave =
            [
                .. Array.FindAll(array, chunk => chunk.Value.modified || (int.Abs(chunk.Value.chunkPos.x - cx) < 4 &&
                                                                          int.Abs(chunk.Value.chunkPos.z - cz) < 4 &&
                                                                          chunk.Value.generated && !chunk.Value.CompletelyEmpty))
            ];

            //Parallel.ForEach(chunks, (chunk)=>
            //{
            //    int distance = int.Max(int.Max(int.Abs(Instance.playerChunkPos.x - chunk.chunkPos.x), int.Abs(Instance.playerChunkPos.y - chunk.chunkPos.y)), int.Abs(Instance.playerChunkPos.z - chunk.chunkPos.z));
            //    if ((!chunk.modified && distance > 6) || chunk.CompletelyEmpty || !chunk.generated) return;

            //    //writeTaskList.Add(File.WriteAllBytesAsync($"{savename}/chunk/{chunk.chunkPos.x}_{chunk.chunkPos.y}_{chunk.chunkPos.z}" + (chunk.modified ? "" : "_unmodified") + ".chunk", chunk.voxels));
            //    writeTaskList.Add(File.WriteAllTextAsync($"{savename}/chunk/{chunk.chunkPos.x}_{chunk.chunkPos.y}_{chunk.chunkPos.z}" + (chunk.modified ? "" : "_unmodified") + ".json", JsonConvert.SerializeObject(chunk)));
            //});
            //writeTaskList.Add(File.WriteAllTextAsync($"{savename}/chunk/chunks.json", JsonConvert.SerializeObject(chunksToSave)));

            var data = MessagePackSerializer.Typeless.Serialize(chunksToSave);
            File.WriteAllBytes($"{savename}/chunk/chunks.cnk",data);

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                MaxDepth = null,
            };

            File.WriteAllText($"{savename}/block/blockdata.json", JsonConvert.SerializeObject(Block.blockCustomData.ToArray(), jsonSerializerSettings));

            File.WriteAllText($"{savename}/entity/chunkbound.json", JsonConvert.SerializeObject(EntityManager.loadedEntities, jsonSerializerSettings));

            EntitySaveData playerSaveData = new EntitySaveData
            {
                position = (Vector3)MGame.Instance.player.position,
                velocity = MGame.Instance.player.velocity,
                rotation = MGame.Instance.player.rotation,
                health = MGame.Instance.player.health,
                maxHealth = MGame.Instance.player.maxHealth,
                customSaveData = MGame.Instance.player.CaptureCustomSaveData()
            };

            string playerJson = JsonConvert.SerializeObject(playerSaveData,jsonSerializerSettings);
            File.WriteAllText($"{savename}/entity/player.json", playerJson);
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
            var prog = new ProgressBar(0, 100, new Vector2(800, 70), Anchor.AutoCenter);
            UserInterface.Active.AddEntity(label);
            UserInterface.Active.AddEntity(prog);
            prog.Value = 0;

            //if (Directory.Exists(backupname)) Directory.Delete(backupname, true);
            //CopyFilesRecursively(savename, backupname);

            WorldName = _savename;

            string overworlddata = File.ReadAllText($"{savename}/overworld.txt");
            var lines = overworlddata.Split('\n');
            MGame.Instance.seed = int.Parse((Array.Find(lines, e => e.StartsWith("seed:")) ?? "seed:0").Split(':')[1]);
            WorldTimeManager.SetWorldTime(int.Parse((Array.Find(lines, e => e.StartsWith("time:")) ?? "time:0").Substring(5)));

            Instance.worldRandom = new Random(Instance.seed);

            MGame.Instance.loadedChunks = new ConcurrentDictionary<long, Chunk>();

            await Task.Run(() => MGame.Instance.LoadWorld(true));

            var files = Directory.GetFiles($"{savename}/chunk");

            int max = files.Length;
            int cur = 0;

            if (!File.Exists($"{savename}/chunk/chunks.cnk"))
            {
                foreach(var chunkfile in files)
                {
                    string chunkfilename = Path.GetFileNameWithoutExtension(chunkfile);

                    string[] coords = chunkfilename.Split('_');
                    int chunkx = int.Parse(coords[0]);
                    int chunky = int.Parse(coords[1]);
                    int chunkz = int.Parse(coords[2]);

                    Chunk chunk = JsonConvert.DeserializeObject<Chunk>(File.ReadAllText(chunkfile));

                    chunk.chunkPos = (chunkx, chunky, chunkz);

                    chunk.generated = true;

                    Array.Fill(chunk.meshLayer, true);

                    chunk.GenerateVisibility();

                    chunk.meshUpdated = [false, false, false, false];

                    chunk.Remesh(useOldLight: true);

                    //Dont just forget about the chunk (like old versions did), just modify it instead. Easier, no?
                    if (!MGame.Instance.loadedChunks.TryAdd(MGame.CCPos(chunk.chunkPos), chunk))
                    {
                        MGame.Instance.loadedChunks[MGame.CCPos(chunk.chunkPos)] = chunk;
                    }
                    //chunk.modified = !chunkfilename.Contains("unmodified");

                    cur++;
                    prog.Value = (int)((cur / (float)max) * 100);
                }
            }
            else
            {
                KeyValuePair<long, Chunk>[] chunks = (MessagePackSerializer.Typeless.Deserialize(File.ReadAllBytes($"{savename}/chunk/chunks.cnk")) as List<KeyValuePair<long, Chunk>>).ToArray();
                max = chunks.Length;
                foreach(var chunk in chunks)
                {
                    if (chunk.Value.CompletelyEmpty) continue;
                    if (!chunk.Value.generated) continue;

					Array.Fill(chunk.Value.meshLayer, true);
                    chunk.Value.GenerateVisibility();
                    chunk.Value.meshUpdated = [false, false, false, false];

                    //Dont just forget about the chunk (like old versions did), just modify it instead. Easier, no?
                    if (!MGame.Instance.loadedChunks.TryAdd(chunk.Key, chunk.Value))
                    {
                        MGame.Instance.loadedChunks[chunk.Key] = chunk.Value;
                    }
                    cur++;
                    prog.Value = (int)((cur / (float)max) * 100);
                }
            }
            

            EntityManager.loadedEntities.Clear();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                MaxDepth = null,
            };
            if (File.Exists($"{savename}/entity/chunkbound.json"))
            {
                var ent = JsonConvert.DeserializeObject<Dictionary<long,List<Entity>>>(File.ReadAllText($"{savename}/entity/chunkbound.json"), jsonSerializerSettings);
                foreach(var lst in ent)
                {
                    foreach(var entity in lst.Value)
                    {
                        entity.Start();
                    }
                }
                EntityManager.loadedEntities = ent;
            }
            Block.blockCustomData = JsonConvert.DeserializeObject<KeyValuePair<CompactBlockPos, object>[]>(File.ReadAllText($"{savename}/block/blockdata.json"), jsonSerializerSettings).ToDictionary();

            EntitySaveData playerData = JsonConvert.DeserializeObject<EntitySaveData>(File.ReadAllText($"{savename}/entity/player.json"),jsonSerializerSettings);

            RestoreEntity(MGame.Instance.player,playerData);

            UserInterface.Active.RemoveEntity(label);
            UserInterface.Active.RemoveEntity(prog);

            Instance.currentPlayState = PlayState.World;
        }
        static void RestoreEntity(Entity entity, EntitySaveData data)
        {
            entity.position = data.position;
            entity.velocity = data.velocity;
            entity.rotation = data.rotation;
            entity.maxHealth = data.maxHealth;
            entity.health = data.health;
            if (data.customSaveData != null) entity.RestoreCustomSaveData(data.customSaveData);
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
