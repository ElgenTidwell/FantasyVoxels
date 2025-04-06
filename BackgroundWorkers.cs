using FantasyVoxels.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public static class BackgroundWorkers
    {
        public static void BackgroundChunks(CancellationToken cancel)
        {
            List<long> deleteChunks = new List<long>();
            try
            {
                while (!cancel.IsCancellationRequested)
                {

                    //var span = MGame.Instance.loadedChunks.ToArray().AsSpan();
                    //span.Sort((KeyValuePair<long, Chunk> a, KeyValuePair<long, Chunk> b) =>
                    //{
                    //    var apos = a.Value.chunkPos;
                    //    var bpos = b.Value.chunkPos;

                    //    float aDist = Vector3.DistanceSquared(new Vector3(apos.x, apos.y, apos.z), new Vector3(MGame.Instance.playerChunkPos.x, MGame.Instance.playerChunkPos.y, MGame.Instance.playerChunkPos.z));
                    //    float bDist = Vector3.DistanceSquared(new Vector3(bpos.x, bpos.y, bpos.z), new Vector3(MGame.Instance.playerChunkPos.x, MGame.Instance.playerChunkPos.y, MGame.Instance.playerChunkPos.z));

                    //    return aDist.CompareTo(bDist);
                    //});

                    foreach (var c in MGame.Instance.loadedChunks)
                    {
                        Chunk currentChunk = c.Value;
                        BoundingBox chunkbounds = new BoundingBox(new Vector3(currentChunk.chunkPos.x, currentChunk.chunkPos.y, currentChunk.chunkPos.z) * Chunk.Size,
                                                                 (new Vector3(currentChunk.chunkPos.x + 1, currentChunk.chunkPos.y + 1, currentChunk.chunkPos.z + 1) * Chunk.Size));

                        float cDist = MathF.Abs(currentChunk.chunkPos.x - MGame.Instance.playerChunkPos.x) + MathF.Abs(currentChunk.chunkPos.y - MGame.Instance.playerChunkPos.y) + MathF.Abs(currentChunk.chunkPos.z - MGame.Instance.playerChunkPos.z);
                        if (cDist >= MGame.Instance.RenderDistance + 4 && !currentChunk.modified)
                        {
                            deleteChunks.Add(c.Key);
                            continue;
                        }
                        if (cDist >= MGame.Instance.RenderDistance) continue;

                        if (currentChunk.queueModified)
                        {
                            currentChunk.queueModified = false;
                            currentChunk.CheckQueue(false);
                        }

                        if (MGame.Instance.frustum.Contains(chunkbounds) == ContainmentType.Disjoint) continue;

                        if (currentChunk.visOutOfDate)
                        {
                            currentChunk.visOutOfDate = false;
                            currentChunk.GenerateVisibility();
                        }

                        if (currentChunk.lightOutOfDate)
                        {
                            if (currentChunk.CompletelyEmpty)
                                currentChunk.ReLight(false);
                            else
                                currentChunk.Remesh();
                        }

                        if (!currentChunk.meshUpdated[currentChunk.GetLOD()])
                        {
                            currentChunk.Remesh();
                        }

                        WorldPopulator.CheckChunk(c.Key);
                    }

                    if (deleteChunks.Count == 0) continue;

                    foreach (long id in deleteChunks)
                    {
                        MGame.Instance.loadedChunks.TryRemove(id, out _);
                    }
                    deleteChunks.Clear();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }
        }
        public static void BackgroundGenerate(CancellationToken cancel)
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    if(!MGame.Instance.toGenerate.IsEmpty)
                    {
                        MGame.Instance.ProcessGeneration();
                    }
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }
        }
    }
}
