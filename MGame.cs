using IslandGame.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IslandGame
{
    public class MGame : Game
    {
        public static MGame Instance { get; private set; }

        public static float dt = 0;
        public static float totalTime = 0;

        public int seed = Random.Shared.Next(0,int.MaxValue/2);
        public Random worldRandom;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public SpriteBatch spriteBatch => _spriteBatch;

        public Matrix world, view, projection;
        public Vector3 cameraPosition;
        public Vector3 cameraForward;

        private RenderTarget2D screenTexture, normalTexture, SSAOTarget;
        public Texture2D colors,noise,uiTextures;
        private Effect raymarcher;
        private Effect chunk;
        private Effect postProcessing;

        private int Width => GraphicsDevice.Viewport.Width;
        private int Height => GraphicsDevice.Viewport.Height;

        public Player player;
        public ConcurrentDictionary<(int x, int y, int z), Chunk> loadedChunks = new ConcurrentDictionary<(int x, int y, int z), Chunk>();
        ConcurrentQueue<(int x, int y, int z)> toGenerate = new ConcurrentQueue<(int x, int y, int z)>();
        ConcurrentQueue<(int x, int y, int z)> toMesh = new ConcurrentQueue<(int x, int y, int z)>();
        List<(int x, int y, int z)> toRender = new List<(int x, int y, int z)>();

        public int RenderDistance = 120;
        public bool enableAO = true;
        float _fov = 90f;
        public float FOV {
            get
            {
                return _fov;
            }
            set
            {
                if (_fov == value) return;

                _fov = value;

                projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_fov), GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);
            }
        }

        BoundingFrustum frustum;

        DepthStencilState stencilDraw;
        (int x, int y, int z) chunkPos;

        int threadsActive = 0;
        TaskPool generationPool;
        TaskPool chunkUpdatePool;

        public VertexPosition[] boxVertices =
        [
            new VertexPosition(new Vector3(0, 0, 0)),
            new VertexPosition(new Vector3(1, 0, 0)),

            new VertexPosition(new Vector3(0, 0, 0)),
            new VertexPosition(new Vector3(0, 1, 0)),

            new VertexPosition(new Vector3(0, 0, 0)),
            new VertexPosition(new Vector3(0, 0, 1)),


            new VertexPosition(new Vector3(0, 1, 0)),
            new VertexPosition(new Vector3(1, 1, 0)),

            new VertexPosition(new Vector3(0, 1, 0)),
            new VertexPosition(new Vector3(0, 1, 1)),

            new VertexPosition(new Vector3(0, 0, 1)),
            new VertexPosition(new Vector3(0, 1, 1)),

            new VertexPosition(new Vector3(1, 0, 0)),
            new VertexPosition(new Vector3(1, 1, 0)),


            new VertexPosition(new Vector3(1, 0, 1)),
            new VertexPosition(new Vector3(1, 0, 0)),

            new VertexPosition(new Vector3(1, 0, 1)),
            new VertexPosition(new Vector3(1, 1, 1)),

            new VertexPosition(new Vector3(1, 0, 1)),
            new VertexPosition(new Vector3(0, 0, 1)),


            new VertexPosition(new Vector3(1, 1, 1)),
            new VertexPosition(new Vector3(1, 1, 0)),

            new VertexPosition(new Vector3(1, 1, 1)),
            new VertexPosition(new Vector3(0, 1, 1)),
        ];
        BasicEffect effect;

        public MGame()
        {
            _graphics = new GraphicsDeviceManager(this);

            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.PreferMultiSampling = false;

            _graphics.GraphicsProfile = GraphicsProfile.HiDef;

            _graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            Instance = this;
            IsFixedTimeStep = false;

            player = new Player();
            worldRandom = new Random(seed);
            player.position = new Vector3(0, 256, 0);
        }

        protected override void Initialize()
        {
            world = Matrix.CreateWorld(Vector3.Zero,Vector3.Forward,Vector3.Up);
            view = Matrix.Identity;
            projection = Matrix.CreateOrthographic(MathHelper.ToRadians(FOV),GraphicsDevice.Viewport.AspectRatio,0.01f,10000f);

            Chunk.chunkBuffer = new VertexBuffer(GraphicsDevice,typeof(VertexPosition),Chunk.chunkVerts.Length,BufferUsage.WriteOnly);
            Chunk.chunkBuffer.SetData(Chunk.chunkVerts);

            //Chunk.indexBuffer = new IndexBuffer(GraphicsDevice,IndexElementSize.SixteenBits,Chunk.triangles.Length,BufferUsage.WriteOnly);
            //Chunk.indexBuffer.SetData(Chunk.triangles);

            player.Start();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            raymarcher = Content.Load<Effect>("Shaders/screencomp");
            chunk = Content.Load<Effect>("Shaders/chunk");
            postProcessing = Content.Load<Effect>("Shaders/pscreen");

            effect = new BasicEffect(MGame.Instance.GraphicsDevice);
            effect.TextureEnabled = false;
            effect.LightingEnabled = false;
            effect.FogEnabled = false;
            effect.VertexColorEnabled = false;

            postProcessing.Parameters["AOSampleOffsets"]?.SetValue(new Vector2[8]
            {
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
                new Vector2((float)Random.Shared.NextDouble(),(float)Random.Shared.NextDouble()),
            });
            screenTexture = new RenderTarget2D(GraphicsDevice,Width,Height,false,SurfaceFormat.HdrBlendable, DepthFormat.Depth24Stencil8);
            normalTexture = new RenderTarget2D(GraphicsDevice,Width,Height,false,SurfaceFormat.HdrBlendable,DepthFormat.Depth24Stencil8);
            SSAOTarget = new RenderTarget2D(GraphicsDevice,Width,Height,false,SurfaceFormat.HdrBlendable,DepthFormat.Depth24Stencil8);

            colors = Content.Load<Texture2D>("Textures/colors");
            noise = Content.Load<Texture2D>("Textures/noise");
            uiTextures = Content.Load<Texture2D>("Textures/UITextures");

            postProcessing.Parameters["NoiseTexture"]?.SetValue(noise);

            stencilDraw = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.LessEqual,
                StencilPass = StencilOperation.DecrementSaturation,
                ReferenceStencil = 1,
                DepthBufferEnable = false,
                DepthBufferWriteEnable = true,
            };
            generationPool = new TaskPool(Environment.ProcessorCount);
            chunkUpdatePool = new TaskPool(4);
        }
        protected override void UnloadContent()
        {
            generationPool.Stop();
            chunkUpdatePool.Stop();
            base.UnloadContent();
        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            player.Update();

            dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            frustum = new BoundingFrustum(world * view * projection);

            if(toGenerate.Count > 0)
            {
                for (int i = 0; i < toGenerate.Count; i+=4)
                {
                    generationPool.EnqueueTask(() =>
                    {
                        for (int i = 0; i < MathF.Min(toGenerate.Count, 4); i++)
                        {
                            if (!toGenerate.TryDequeue(out (int x, int y, int z) t)) return;

                            if (loadedChunks.ContainsKey(t))
                            {
                                return; // If chunk already loaded, exit this task
                            }

                            BoundingBox chunkbounds = new BoundingBox(new Vector3(t.x, t.y, t.z) * Chunk.Size, (new Vector3(t.x, t.y, t.z) + Vector3.One) * Chunk.Size);

                            if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint)
                            {
                                return;
                            }

                            Chunk c = new Chunk();

                            c.chunkPos = t;

                            c.Generate();

                            loadedChunks.TryAdd(t, c);
                        }
                    });
                }
            }
            if (toMesh.Count > 0)
            {
                for (int i = 0; i < toMesh.Count; i += 4)
                {
                    chunkUpdatePool.EnqueueTask(() =>
                    {
                        for (int i = 0; i < MathF.Min(toMesh.Count, 4); i++)
                        {
                            if (!toMesh.TryDequeue(out (int x, int y, int z) t)) return;

                            BoundingBox chunkbounds = new BoundingBox(new Vector3(t.x, t.y, t.z) * Chunk.Size, (new Vector3(t.x, t.y, t.z) + Vector3.One) * Chunk.Size);

                            if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint || loadedChunks[t].meshUpdated[loadedChunks[t].GetLOD()])
                            {
                                return;
                            }

                            loadedChunks[t].Remesh();
                        }
                    });
                }
            }
            int cx;
            int cy;
            int cz;

            if (!Keyboard.GetState().IsKeyDown(Keys.F))
            {
                cx = (int)MathF.Floor(cameraPosition.X / Chunk.Size);
                cy = (int)MathF.Floor(cameraPosition.Y / Chunk.Size);
                cz = (int)MathF.Floor(cameraPosition.Z / Chunk.Size);
                chunkPos = (cx, cy, cz);
            }
            else
            {
                cx = chunkPos.x;
                cy = chunkPos.y;
                cz = chunkPos.z;
            }

            toRender.Clear();

            //{
            //    HashSet<(int x, int y, int z)> visited = new HashSet<(int x, int y, int z)>();
            //    Queue<(int x, int y, int z)> bfsQueue = new Queue<(int x, int y, int z)>();

            //    // Initialize with the player's chunk
            //    bfsQueue.Enqueue((cx, cy, cz));
            //    visited.Add((cx, cy, cz));

            //    while (bfsQueue.Count > 0 && toGenerate.Count < 32)
            //    {
            //        var currentChunk = bfsQueue.Dequeue();
            //        (int x, int y, int z) = currentChunk;

            //        // Define chunk bounds and check if in view
            //        BoundingBox chunkBounds = new BoundingBox(new Vector3(x, y, z) * Chunk.Size, (new Vector3(x, y, z) + Vector3.One) * Chunk.Size);
            //        if (frustum.Contains(chunkBounds) == ContainmentType.Disjoint)
            //            continue;

            //        // If chunk is not yet generated or queued, add it to generate queue
            //        if (!loadedChunks.ContainsKey(currentChunk) && !toGenerate.Contains(currentChunk))
            //        {
            //            toGenerate.Enqueue(currentChunk);
            //        }
            //        else if (loadedChunks.TryGetValue(currentChunk, out var chunk))
            //        {
            //            if (chunk.queueModified) chunk.CheckQueue();
            //        }

            //        // Add neighbors to the BFS queue if they haven't been visited
            //        var neighbors = new (int x, int y, int z)[]
            //        {
            //        (x + 1, y, z),
            //        (x - 1, y, z),
            //        (x, y + 1, z),
            //        (x, y - 1, z),
            //        (x, y, z + 1),
            //        (x, y, z - 1),
            //        };

            //        foreach (var neighbor in neighbors)
            //        {
            //            if (!visited.Contains(neighbor) && MathF.Abs(neighbor.x - cx) + MathF.Abs(neighbor.y - cy) + MathF.Abs(neighbor.z - cz) <= RenderDistance)
            //            {
            //                visited.Add(neighbor);
            //                bfsQueue.Enqueue(neighbor); // Add this chunk to the BFS queue
            //            }
            //        }
            //    }
            //}

            {
                Queue<(int x, int y, int z, int face, int depth)> bfsQueue = new Queue<(int, int, int, int, int)>();
                HashSet<(int, int, int)> visitedChunks = new HashSet<(int, int, int)>();

                // Start the BFS with the current chunk and all its visible faces
                bfsQueue.Enqueue((cx,cy,cz, -1, 0)); // -1 means no face restriction initially

                while (bfsQueue.Count > 0)
                {
                    var (x, y, z, fromFace, depth) = bfsQueue.Dequeue();

                    // Check if we have exceeded the render distance
                    if (MathF.Abs(x - cx) + MathF.Abs(y - cy) + MathF.Abs(z - cz) >= RenderDistance) continue;

                    if (!loadedChunks.TryGetValue((x, y, z), out var currentChunk))
                    {
                        // If chunk is not yet generated or queued, add it to generate queue
                        if (!toGenerate.Contains((x, y, z)))
                        {
                            toGenerate.Enqueue((x, y, z));
                        }
                        continue;
                    }

                    BoundingBox chunkbounds = new BoundingBox(new Vector3(x, y, z) * Chunk.Size, (new Vector3(x+1, y+1, z+1) * Chunk.Size));
                    if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint)
                        continue;

                    if (currentChunk.queueModified)
                    {
                        currentChunk.queueModified = false;
                        if (x == cx && z == cz) currentChunk.CheckQueue();
                        else chunkUpdatePool.EnqueueTask(() => currentChunk.CheckQueue());
                    }
                    if (!currentChunk.meshUpdated[currentChunk.GetLOD()] && !toMesh.Contains((x, y, z)))
                    {
                        toMesh.Enqueue((x,y,z));
                    }

                    if (visitedChunks.Contains((x, y, z))) continue;
                    visitedChunks.Add((x, y, z));

                    // Add to render queue if the chunk is not completely empty
                    if (!currentChunk.CompletelyEmpty)
                    {
                        toRender.Add((x, y, z));
                    }

                    // BFS to neighbors through visible faces
                    foreach (var (neighborX, neighborY, neighborZ, exitFace) in GetVisibleNeighbors(currentChunk, fromFace))
                    {
                        // Calculate the new depth for the neighbor
                        int newDepth = depth + 1;

                        var chunkDir = new Vector3(neighborX, neighborY, neighborZ) - new Vector3(x,y,z);
                        var cameraDir = new Vector3(cx, cy, cz) - new Vector3(x,y,z);

                        if (Vector3.Dot(chunkDir, cameraDir) > 0) continue;

                        // Enqueue the neighbor only if it hasn't been visited and we are within the depth limit
                        if (!visitedChunks.Contains((neighborX, neighborY, neighborZ)))
                        {
                            bfsQueue.Enqueue((neighborX, neighborY, neighborZ, exitFace, newDepth));
                        }
                    }
                }

                List<(int x, int y, int z, int face)> GetVisibleNeighbors(Chunk chunk, int fromFace)
                {
                    List<(int x, int y, int z, int face)> neighbors = new List<(int, int, int, int)>();

                    const int LEFT_FACE = 1;
                    const int RIGHT_FACE = 0;
                    const int TOP_FACE = 2;
                    const int BOTTOM_FACE = 3;
                    const int FRONT_FACE = 5;
                    const int BACK_FACE = 4;

                    // Check if we can travel through each face of the chunk
                    if (fromFace == -1 || (chunk.TestVisibility(LEFT_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x - 1, chunk.chunkPos.y, chunk.chunkPos.z, RIGHT_FACE));
                    if (fromFace == -1 || (chunk.TestVisibility(RIGHT_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x + 1, chunk.chunkPos.y, chunk.chunkPos.z, LEFT_FACE));
                    if (fromFace == -1 || (chunk.TestVisibility(BOTTOM_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y - 1, chunk.chunkPos.z, TOP_FACE));
                    if (fromFace == -1 || (chunk.TestVisibility(TOP_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y + 1, chunk.chunkPos.z, BOTTOM_FACE));
                    if (fromFace == -1 || (chunk.TestVisibility(FRONT_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z - 1, BACK_FACE));
                    if (fromFace == -1 || (chunk.TestVisibility(BACK_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z + 1, FRONT_FACE));

                    return neighbors;
                }

                toRender.Sort(((int x, int y, int z) a, (int x, int y, int z) b) =>
                {
                    float adist = MathF.Abs(a.x-cx)+MathF.Abs(a.y-cy)+MathF.Abs(a.z-cz);
                    float bdist = MathF.Abs(b.x-cx)+MathF.Abs(b.y-cy)+MathF.Abs(b.z-cz);

                    return adist.CompareTo(bdist);
                });
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            //raymarcher.Parameters["Texture"].SetValue(screenTexture);
            //raymarcher.Parameters["cameraViewMatrix"].SetValue(Matrix.Invert(view));
            //raymarcher.Parameters["cameraPosition"].SetValue(cameraPosition);

            //foreach (var pass in raymarcher.CurrentTechnique.Passes)
            //{
            //    const int threadsize = 8;
            //    pass.ApplyCompute();
            //    GraphicsDevice.DispatchCompute(Width/ threadsize, Height/ threadsize, 1);
            //}

            //_spriteBatch.Begin(samplerState:SamplerState.PointClamp);

            //_spriteBatch.Draw(screenTexture,GraphicsDevice.Viewport.Bounds,Color.White);

            //_spriteBatch.End();

            totalTime = (float)gameTime.TotalGameTime.TotalSeconds;

            GraphicsDevice.SetRenderTargets(screenTexture,normalTexture);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.Stencil, Color.LightSkyBlue, 1000f, 1);

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;

            chunk.Parameters["skyColor"].SetValue(Color.LightSkyBlue.ToVector3());
            chunk.Parameters["renderDistance"]?.SetValue(RenderDistance);
            chunk.Parameters["ChunkSize"]?.SetValue(Chunk.Size);
            chunk.Parameters["cameraPosition"]?.SetValue(cameraPosition);

            chunk.Parameters["View"].SetValue(view);
            chunk.Parameters["Projection"].SetValue(projection);
            chunk.Parameters["time"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
            chunk.Parameters["colors"]?.SetValue(colors);

            foreach (var c in toRender.ToArray().AsSpan())
            {
                BoundingBox chunkbounds = new BoundingBox(new Vector3(c.x, c.y, c.z) * Chunk.Size, (new Vector3(c.x, c.y, c.z) * Chunk.Size + new Vector3(Chunk.Size, loadedChunks[c].MaxY + 1, Chunk.Size)));
                if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint)
                    continue;

                int LOD = loadedChunks[c].GetLOD();

                if (loadedChunks[c].chunkVertexBuffers[LOD] == null || loadedChunks[c].chunkVertexBuffers[LOD].VertexCount == 0) 
                    continue;

                GraphicsDevice.SetVertexBuffer(loadedChunks[c].chunkVertexBuffers[LOD]);

                chunk.Parameters["World"].SetValue(world * Matrix.CreateTranslation(new Vector3(c.x, c.y, c.z) * Chunk.Size));
                chunk.Parameters["minSafeDistance"]?.SetValue(Vector3.Distance(cameraPosition, Vector3.Min(chunkbounds.Max, Vector3.Max(chunkbounds.Min, cameraPosition))));

                foreach (var pass in chunk.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, loadedChunks[c].chunkVertexBuffers[LOD].VertexCount/3);
                }
            }

            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            player.Render();
            //foreach (var c in toRender.ToArray().AsSpan())
            //{
            //    effect.Alpha = 0.4f;
            //    effect.DiffuseColor = Color.Black.ToVector3();

            //    effect.Projection = MGame.Instance.projection;
            //    effect.View = MGame.Instance.view;

            //    effect.World = Matrix.CreateScale(Chunk.Size, loadedChunks[c].MaxY + 1, Chunk.Size) * MGame.Instance.world * Matrix.CreateTranslation(new Vector3(c.x, c.y, c.z) * Chunk.Size);

            //    foreach (var pass in effect.CurrentTechnique.Passes)
            //    {
            //        pass.Apply();
            //        Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, boxVertices, 0, boxVertices.Length / 2);
            //    }
            //}


            GraphicsDevice.SetRenderTarget(SSAOTarget);

            postProcessing.Parameters["NormalTexture"]?.SetValue(normalTexture);
            postProcessing.Parameters["ScreenTexture"]?.SetValue(screenTexture);

            Matrix _view = Matrix.Identity;

            Matrix _projection = Matrix.CreateOrthographicOffCenter(0, SSAOTarget.Width, SSAOTarget.Height, 0, 0, 1);

            if(enableAO)
            {
                postProcessing.Parameters["view_projection"].SetValue(_view * _projection);
                postProcessing.Parameters["screenSize"]?.SetValue(new Vector2(SSAOTarget.Width, SSAOTarget.Height));
                postProcessing.Parameters["noiseOffset"]?.SetValue(totalTime % 32);

                postProcessing.CurrentTechnique = postProcessing.Techniques["AO"];

                _spriteBatch.Begin(effect: postProcessing);

                _spriteBatch.Draw(screenTexture, new Rectangle(0, 0, SSAOTarget.Width, SSAOTarget.Height), Color.White);

                _spriteBatch.End();
            }
            else
            {
                GraphicsDevice.Clear(Color.White);
            }

            GraphicsDevice.SetRenderTarget(null);

            postProcessing.Parameters["AOTexture"]?.SetValue(SSAOTarget);

            _projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1);

            postProcessing.Parameters["view_projection"].SetValue(_view * _projection);
            postProcessing.Parameters["screenSize"]?.SetValue(new Vector2(screenTexture.Width, screenTexture.Height));
            postProcessing.CurrentTechnique = postProcessing.Techniques["SpriteBatch"];

            _spriteBatch.Begin(effect: postProcessing);

            _spriteBatch.Draw(screenTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            _spriteBatch.End();

            player.RenderUI();

            base.Draw(gameTime);
        }

        public int GrabVoxel(Vector3 p)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.ContainsKey((cx, cy, cz)))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                return loadedChunks[(cx, cy, cz)].voxels[x + Chunk.Size * (y + Chunk.Size * z)];
            }
            return 0;
        }
        public void SetVoxel(Vector3 p, int newVoxel)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.ContainsKey((cx, cy, cz)))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                loadedChunks[(cx, cy, cz)].Modify(x, y, z, newVoxel);
            }
        }
        public void SetVoxel_Q(Vector3 p, int newVoxel)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.ContainsKey((cx, cy, cz)))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                loadedChunks[(cx, cy, cz)].ModifyQueue(x, y, z, newVoxel);
            }
            else if (!toGenerate.Contains((cx, cy, cz)))
            {
                toGenerate.Enqueue((cx, cy, cz));
            }
        }
    }
    public static class CollisionDetector
    {
        public static bool IsSolidTile(int x, int y, int z)
        {
            int v = MGame.Instance.GrabVoxel(new Vector3(x, y, z));
            return v != 0 && !Voxel.voxelTypes[v].ignoreCollision;
        }
        public static Vector3 ResolveCollision(BoundingBox aabb, Vector3 position, ref Vector3 velocity, bool step)
        {
            // Get the bounds of the AABB
            Vector3 min = aabb.Min + position;
            Vector3 max = aabb.Max + position;

            // Calculate the AABB's current tile bounds
            int minX = (int)Math.Floor(min.X);
            int maxX = (int)Math.Ceiling(max.X);
            int minY = (int)Math.Floor(min.Y);
            int maxY = (int)Math.Ceiling(max.Y);
            int minZ = (int)Math.Floor(min.Z);
            int maxZ = (int)Math.Ceiling(max.Z);

            // Initialize resolved position and minimum overlap variables
            Vector3 resolvedPosition = position; // Start with the original position
            float minOverlap = float.MaxValue;
            Vector3 collisionNormal = Vector3.Zero;

            float stepy = 0f;

            // Iterate through the bounding box's tiles
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (IsSolidTile(x, y, z))
                        {
                            if (x + 1 < min.X || y + 1 < min.Y || z + 1 < min.Z || x > max.X || y > max.Y || z > max.Z) continue;

                            if (step && y + 1 > min.Y && (y + 1 - min.Y) < 2 && !IsSolidTile(x, y + 1, z) && !IsSolidTile(x, (int)max.Y, z))
                            {
                                stepy = MathF.Abs(y + 1 - min.Y);
                                continue;
                            }

                            bool flipx = x > (min.X + max.X) / 2f;
                            bool flipy = y > (min.Y + max.Y) / 2f;
                            bool flipz = z > (min.Z + max.Z) / 2f;
                            // Calculate overlaps for each axis
                            float overlapX = (flipx) ? max.X - x : min.X - (x + 1);
                            float overlapY = (flipy) ? max.Y - y : min.Y - (y + 1);
                            float overlapZ = (flipz) ? max.Z - z : min.Z - (z + 1);

                            // Compare overlaps and update minimum overlap and normal accordingly
                            if (Math.Abs(overlapX) < Math.Abs(minOverlap))
                            {
                                minOverlap = Math.Abs(overlapX);
                                collisionNormal = (flipx) ? Vector3.Left : Vector3.Right;
                            }
                            if (Math.Abs(overlapY) < Math.Abs(minOverlap))
                            {
                                minOverlap = Math.Abs(overlapY);
                                collisionNormal = (flipy) ? Vector3.Down : Vector3.Up;
                            }
                            if (Math.Abs(overlapZ) < Math.Abs(minOverlap))
                            {
                                minOverlap = Math.Abs(overlapZ);
                                collisionNormal = (flipz) ? Vector3.Forward : Vector3.Backward;
                            }

                            // Resolve the collision based on the smallest overlap
                            if (minOverlap != float.MaxValue)
                            {
                                resolvedPosition += (minOverlap + float.Epsilon) * collisionNormal;
                                velocity = Maths.ProjectOnPlane(velocity, collisionNormal);
                                minOverlap = float.MaxValue;

                                // Get the bounds of the AABB
                                position = resolvedPosition;
                                min = aabb.Min + position;
                                max = aabb.Max + position;
                            }
                        }
                    }
                }
            }
            resolvedPosition.Y += stepy;    
            // Return the resolved position
            return resolvedPosition;
        }

    }
}
