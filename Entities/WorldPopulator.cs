using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Entities
{
	public static class WorldPopulator
	{
		const int MAX_POPULATION = 2;
		static int totalPopulation;
		static ConcurrentDictionary<long, Chunk> chunks => MGame.Instance.loadedChunks;
		static ConcurrentDictionary<long, int> entityPopulations = new ConcurrentDictionary<long, int>();
		static float timer = 0;
		public static void UpdateTimer()
		{
			if(timer < -0.5f) timer = Random.Shared.NextSingle() * 10;
			timer -= MGame.dt;
		}
		public static void CheckChunk(long id)
		{
			if (!chunks.ContainsKey(id)) return;

			int population = 0;
			entityPopulations.TryGetValue(id, out population);
			totalPopulation += population;

            int cx = (int)double.Floor(MGame.Instance.player.position.X / Chunk.Size);
            int cy = (int)double.Floor(MGame.Instance.player.position.Y / Chunk.Size);
            int cz = (int)double.Floor(MGame.Instance.player.position.Z / Chunk.Size);

			var cpos = MGame.ReverseCCPos(id);

			if (int.Abs(cx - cpos.x) <= 1 && int.Abs(cz - cpos.z) <= 1 && int.Abs(cy - cpos.y) <= 1) return;

            if (timer <= 0 && population < MAX_POPULATION && totalPopulation < 75)
			{
				int spawnCount = Random.Shared.Next(-8, MAX_POPULATION - population);

				var spawnCandidate = Array.FindAll(chunks[id].voxeldata, t => t.spawnCandidate.pos);

				if (spawnCandidate == null || spawnCandidate.Length == 0 || spawnCount <= 0) return;

				for(int i = 0; i < spawnCount; i++)
				{
					int index = Random.Shared.Next(0, spawnCandidate.Length);
					Vector3Double pos = new Vector3Double(spawnCandidate[index].spawnCandidate.x, spawnCandidate[index].spawnCandidate.y+1, spawnCandidate[index].spawnCandidate.z);

					if (MGame.Instance.GrabVoxel((Vector3)pos) != 0) continue;
					if (!MGame.Instance.GrabVoxelData((Vector3)pos, out var data)) continue;

					Entity spawn = new Cow();
					spawn.position = pos;

					EntityManager.SpawnEntity(spawn);
				}

				if (entityPopulations.ContainsKey(id)) entityPopulations[id] += spawnCount;
				else entityPopulations.TryAdd(id, spawnCount);
			}
		}
	}
}
