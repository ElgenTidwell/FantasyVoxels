using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Saves
{
    internal static class Save
    {
        public static string WorldName = "";
        public static void SaveToFile(string savename)
        {
            Chunk[] chunks = MGame.Instance.loadedChunks.Values.ToArray();

            if(Directory.Exists(savename)) Directory.Delete(savename, true);

            Directory.CreateDirectory(savename);
            Directory.CreateDirectory($"{savename}/chunk");

            foreach (Chunk chunk in chunks)
            {
                if (!chunk.modified) continue;

                File.WriteAllBytes($"{savename}/chunk/{chunk.chunkPos.x}_{chunk.chunkPos.y}_{chunk.chunkPos.z}.chunk",chunk.voxels);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"seed:{MGame.Instance.seed}");

            File.WriteAllText($"{savename}/overworld.txt",sb.ToString());
        }
    }
}
