using FantasyVoxels.Entities;
using FantasyVoxels.Saves;
using FantasyVoxels.UI;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Icaria.Engine.Procedural.IcariaNoise;
using static System.Net.Mime.MediaTypeNames;
using FantasyVoxels.ItemManagement;
using FmodForFoxes;
using FmodForFoxes.Studio;

namespace FantasyVoxels
{
    public class MGame : Game
    {
        public static MGame Instance { get; private set; }

        public static float dt = 0;
        public static float totalTime = 0;

        public const int AtlasSize = 512;
        public const int ItemAtlasSize = 512;

        public int seed = Environment.TickCount;
        public Random worldRandom;

        #region Rendering Variables
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SamplerState crunchy;

        public Matrix world, view, projection, sunProjection, sunView;
        public Vector3 cameraPosition;
        public Vector3 cameraForward;
        public Vector3Double worldSpawnpoint;

        public SpriteBatch spriteBatch => _spriteBatch;
        private RenderTarget2D screenTexture, normalTexture, SSAOTarget, shadowMap;
        public Texture2D colors,items,normals,noise,uiTextures,uiBackback,sunTexture,moonTexture,aoMap,white,breaking,lightmap,waterOverlay,waterDUDV;
        private Effect chunk;
        private Effect entity;
        private Effect particles;
        private Effect breakingVoxel;
        private Effect sky;
        private Effect postProcessing;

        private bool cameraUnderwater;

        public Effect GetEntityShader() => entity;
        public Effect GetParticleShader() => particles;
        public Effect GetBreakingShader() => breakingVoxel;

        public float daylightPercentage;

        private int Width => GraphicsDevice.Viewport.Width;
        private int Height => GraphicsDevice.Viewport.Height;

        public BoundingFrustum frustum = new BoundingFrustum(Matrix.Identity);

        List<(int x, int y, int z)> toRender = new List<(int x, int y, int z)>();

        public int RenderDistance => (int)Options.renderDistance;
        public bool enableAO = false;
        float _fov = 70f;
        public float FOV
        {
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

        #endregion

        public bool gamePaused;

        public Player player;
        public (int x, int y, int z) playerChunkPos;

        #region Chunking Variables
        public ConcurrentDictionary<long, Chunk> loadedChunks = new ConcurrentDictionary<long, Chunk>();
        public ConcurrentQueue<(int x, int y, int z)> toGenerate = new ConcurrentQueue<(int x, int y, int z)>();
        public ConcurrentQueue<(int x, int y, int z)> toMesh = new ConcurrentQueue<(int x, int y, int z)>();
        
        Queue<long> instantRemesh = new Queue<long>();
        #endregion

        DepthStencilState stencilDraw;

        int threadsActive = 0;
        //TaskPool generationPool;
        CancellationTokenSource chunkThreadCancel;
        Thread chunkUpdateThread;
        Thread chunkGenerateThread;

        int meshesWorking;
        int generateWorking;

        #region verts
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

        public static VertexPosition[] skyboxVerts = GenerateSphereVerticesDirect(1,50,50);

        public static VertexPositionTexture[] flatSprite = 
        [
            new VertexPositionTexture(new Vector3(0, 0, 0),new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(0, 1, 0),new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(1, 0, 0),new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(1, 0, 0),new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(0, 1, 0),new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(1, 1, 0),new Vector2(1, 1)),
        ];

        public static short[] triangles =
        [
            1,
            2,
            5, //face right
            1,
            5,
            6,
            0,
            7,
            4, //face left
            0,
            4,
            3,
            0,
            2,
            1, //face front
            0,
            3,
            2,
            5,
            4,
            7, //face back
            5,
            7,
            6,
            2,
            3,
            4, //face top
            2,
            4,
            5,
            0,
            6,
            7, //face bottom
            0,
            1,
            6
        ];
        #endregion
        
        public BasicEffect basicEffect;

        public static BlendState shadowBlend,crosshair;

        #region AudioVariables

        public static List<Bank> soundBanks = new List<Bank>();
        public static EventDescription[] placeBlockEvents = new EventDescription[(int)Enum.GetValues(typeof(Voxel.SurfaceType)).Length-1];
        public static EventDescription[] walkBlockEvents = new EventDescription[(int)Enum.GetValues(typeof(Voxel.SurfaceType)).Length-1];
        public static Listener3D listener;

        #endregion
        public enum PlayState
        {
            World,
            Menu,
            LoadingWorld,
            SavingWorld,
        }

        public PlayState currentPlayState = PlayState.Menu;

        public static VertexPosition[] GenerateSphereVerticesDirect(float radius = 1.0f, int latitudeSegments = 10, int longitudeSegments = 10)
        {
            List<VertexPosition> sphereVerts = new List<VertexPosition>();

            for (int lat = 0; lat < latitudeSegments; lat++)
            {
                // Calculate angles for current and next latitudes
                float phi1 = MathF.PI * lat / latitudeSegments;
                float phi2 = MathF.PI * (lat + 1) / latitudeSegments;

                float y1 = radius * MathF.Cos(phi1);
                float latRadius1 = radius * MathF.Sin(phi1);

                float y2 = radius * MathF.Cos(phi2);
                float latRadius2 = radius * MathF.Sin(phi2);

                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    // Calculate angles for current and next longitude
                    float theta1 = 2 * MathF.PI * lon / longitudeSegments;
                    float theta2 = 2 * MathF.PI * (lon + 1) / longitudeSegments;

                    // Calculate four points defining a quad
                    Vector3 v1 = new Vector3(latRadius1 * MathF.Cos(theta1), y1, latRadius1 * MathF.Sin(theta1));
                    Vector3 v2 = new Vector3(latRadius1 * MathF.Cos(theta2), y1, latRadius1 * MathF.Sin(theta2));
                    Vector3 v3 = new Vector3(latRadius2 * MathF.Cos(theta1), y2, latRadius2 * MathF.Sin(theta1));
                    Vector3 v4 = new Vector3(latRadius2 * MathF.Cos(theta2), y2, latRadius2 * MathF.Sin(theta2));

                    // Create two triangles for each quad
                    sphereVerts.Add(new VertexPosition(v1));
                    sphereVerts.Add(new VertexPosition(v3));
                    sphereVerts.Add(new VertexPosition(v2));

                    sphereVerts.Add(new VertexPosition(v2));
                    sphereVerts.Add(new VertexPosition(v3));
                    sphereVerts.Add(new VertexPosition(v4));
                }
            }

            return sphereVerts.ToArray();
        }
        public MGame()
        {
            _graphics = new GraphicsDeviceManager(this);

            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            _graphics.HardwareModeSwitch = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.PreferMultiSampling = false;

            _graphics.GraphicsProfile = GraphicsProfile.Reach;

            _graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Instance = this;
            IsFixedTimeStep = false;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            float minlength = float.Min(Window.ClientBounds.Width/1920f, Window.ClientBounds.Height/1080f);

            UserInterface.Active.GlobalScale = float.Round(minlength*16)/16;
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_fov), GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);
        }

        protected override void Initialize()
        {
            world = Matrix.CreateWorld(Vector3.Zero,Vector3.Forward,Vector3.Up);
            view = Matrix.Identity;
            projection = Matrix.CreateOrthographic(MathHelper.ToRadians(FOV),GraphicsDevice.Viewport.AspectRatio,0.01f,4000f);

            sunProjection = Matrix.CreateOrthographicOffCenter(Chunk.Size* -8, Chunk.Size* 8, Chunk.Size * -8, Chunk.Size * 8, -Chunk.Size * 8, Chunk.Size * 8);
            sunView = Matrix.CreateRotationX(MathHelper.ToRadians(45));

            Chunk.chunkBuffer = new VertexBuffer(GraphicsDevice,typeof(VertexPosition),Chunk.chunkVerts.Length,BufferUsage.WriteOnly);
            Chunk.chunkBuffer.SetData(Chunk.chunkVerts);

            shadowBlend = new BlendState
            {
                ColorBlendFunction = BlendFunction.Min
            };

            // GeonBit.UI: Init the UI manager using the "hd" built-in theme
            UserInterface.Initialize(Content, BuiltinThemes.lowres);
            UserInterface.Active.ShowCursor = false;

            Paragraph.BaseSize = 3;

            TitleMenu.ReInit();

            CraftingManager.Setup();

            FmodManager.Init(new DesktopNativeFmodLibrary(), FmodInitMode.CoreAndStudio, "Content");

            soundBanks.Add(StudioSystem.LoadBank("Master.bank"));
            soundBanks.Add(StudioSystem.LoadBank("Master.strings.bank"));

            for(int i = 0; i < placeBlockEvents.Length; i++)
            {
                placeBlockEvents[i] = StudioSystem.GetEvent($"event:/Blocks/{((Voxel.SurfaceType)(i+1))}_place");
                walkBlockEvents[i] = StudioSystem.GetEvent($"event:/Blocks/{((Voxel.SurfaceType)(i+1))}_walk");
            }

            listener = new Listener3D();

            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            chunk = Content.Load<Effect>("Shaders/chunk");
            entity = Content.Load<Effect>("Shaders/entity");
            particles = Content.Load<Effect>("Shaders/particles");
            breakingVoxel = Content.Load<Effect>("Shaders/breakingVoxel");
            sky = Content.Load<Effect>("Shaders/skybox");
            postProcessing = Content.Load<Effect>("Shaders/pscreen");

            basicEffect = new BasicEffect(MGame.Instance.GraphicsDevice);
            basicEffect.TextureEnabled = false;
            basicEffect.LightingEnabled = false;
            basicEffect.FogEnabled = false;
            basicEffect.VertexColorEnabled = false;

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
            screenTexture = new RenderTarget2D(GraphicsDevice,Width,Height,false,SurfaceFormat.Color, DepthFormat.Depth24);
            normalTexture = new RenderTarget2D(GraphicsDevice,Width,Height,false,SurfaceFormat.HdrBlendable,DepthFormat.Depth24);
            SSAOTarget = new RenderTarget2D(GraphicsDevice,Width/2,Height/2,false,SurfaceFormat.HdrBlendable,DepthFormat.None);
            shadowMap = new RenderTarget2D(GraphicsDevice, 2048, 2048, false, SurfaceFormat.Single, DepthFormat.Depth24);

            colors = Content.Load<Texture2D>("Textures/colors");
            items = Content.Load<Texture2D>("Textures/items");
            normals = Content.Load<Texture2D>("Textures/normals");
            aoMap = Content.Load<Texture2D>("Textures/aomap");
            noise = Content.Load<Texture2D>("Textures/noise");
            uiTextures = Content.Load<Texture2D>("Textures/UITextures");
            uiBackback = Content.Load<Texture2D>("Textures/backpack");
            white = Content.Load<Texture2D>("Textures/white");
            breaking = Content.Load<Texture2D>("Textures/breaking");
            sunTexture = Content.Load<Texture2D>("Textures/sun");
            moonTexture = Content.Load<Texture2D>("Textures/moon");
            lightmap = Content.Load<Texture2D>("Textures/lightmap");
            waterOverlay = Content.Load<Texture2D>("Textures/waterOverlay");
            waterDUDV = Content.Load<Texture2D>("Textures/water_dudv");

            chunk.Parameters["ChunkSize"]?.SetValue(Chunk.Size);
            chunk.Parameters["colors"]?.SetValue(colors);
            chunk.Parameters["normal"]?.SetValue(normals);
            chunk.Parameters["lightmap"]?.SetValue(lightmap);
            entity.Parameters["lightmap"]?.SetValue(lightmap);
            particles.Parameters["lightmap"]?.SetValue(lightmap);

            postProcessing.Parameters["NoiseTexture"]?.SetValue(noise);

            TitleMenu.LoadContent();

            stencilDraw = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.LessEqual,
                StencilPass = StencilOperation.DecrementSaturation,
                ReferenceStencil = 1,
                DepthBufferEnable = false,
                DepthBufferWriteEnable = true,
            };
            crosshair = new BlendState
            {
                ColorBlendFunction = BlendFunction.Add,
                ColorSourceBlend = Blend.InverseDestinationColor,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
            };
            crunchy = new SamplerState
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = TextureFilter.Point,
                MaxMipLevel = 0,
                MaxAnisotropy = 0
            };


            //for(int i = 1; i < blockSounds.GetLength(0); i++)
            //{
            //    for(int w = 0; w < numWalkSounds; w++)
            //    {
            //        blockSounds[i-1, w] = Content.Load<SoundEffect>($"Sounds/blocks/{(Voxel.SurfaceType)i}/step-{w:00}");
            //    }
            //    for (int p = 0; p < numPlaceSounds; p++)
            //    {
            //        blockSounds[i-1, p+numWalkSounds] = Content.Load<SoundEffect>($"Sounds/blocks/{(Voxel.SurfaceType)i}/place-{p:00}");
            //    }
            //}
        }
        protected override void UnloadContent()
        {
            FmodManager.Unload();
            UserInterface.Active.Dispose();
            chunkThreadCancel?.Dispose();

            base.UnloadContent();
        }

        public async Task LoadWorld(bool fromsave = false)
        {
            VoxelStructurePlacer.Clear();
            toGenerate.Clear();
            toMesh.Clear();

            TitleMenu.Cleanup();
            chunkThreadCancel = new();

            chunkUpdateThread = new Thread(() => BackgroundWorkers.BackgroundChunks(chunkThreadCancel.Token));
            chunkUpdateThread.Name = "Background Chunk Update Thread";
            chunkUpdateThread.Priority = ThreadPriority.AboveNormal;
            chunkUpdateThread.IsBackground = false;

            chunkGenerateThread = new Thread(() => BackgroundWorkers.BackgroundGenerate(chunkThreadCancel.Token));
            chunkGenerateThread.Name = "Background Chunk Generate Thread";
            chunkGenerateThread.Priority = ThreadPriority.Highest;
            chunkGenerateThread.IsBackground = false;

            IsMouseVisible = false;

            player = new Player();

            player.position = new Vector3(0, 256, 0);

            totalTime = 0;

            chunkUpdateThread.Start();
            chunkGenerateThread.Start();

            player.Start();

            Chunk.noise3d = new FastNoiseLite(seed + 10);
            Chunk.noise3d.SetNoiseType(FastNoiseLite.NoiseType.Value);

            Chunk.domainWarp = new FastNoiseLite(seed + 10);
            Chunk.domainWarp.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
            Chunk.domainWarp.SetDomainWarpAmp(72);
            Chunk.domainWarp.SetFrequency(.02f);

            if (!fromsave)
            {
                var label = new Label("Building terrain", Anchor.AutoCenter);
                var prog = new ProgressBar(0, 100, new Vector2(800,70), Anchor.AutoCenter);
                UserInterface.Active.AddEntity(label);
                UserInterface.Active.AddEntity(prog);

                const int spawnGenRadius = 4;

                List<(int x, int y, int z)> generate = new List<(int x, int y, int z)>();

                int cx;
                int cy;
                int cz;
                
                player.position.Y = 0;

                cx = (int)MathF.Floor(cameraPosition.X / Chunk.Size);
                cy = (int)MathF.Floor(cameraPosition.Y / Chunk.Size);
                cz = (int)MathF.Floor(cameraPosition.Z / Chunk.Size);

                for (int x = -spawnGenRadius; x <= spawnGenRadius; x++)
                {
                    for (int y = -1; y <= 4; y++)
                    {
                        for (int z = -spawnGenRadius; z <= spawnGenRadius; z++)
                        {
                            var t = (x + cx, y + cy, z + cz);
                            if (!loadedChunks.ContainsKey(CCPos(t)))
                            {
                                generate.Add((x, y, z));
                            }
                        }
                    }
                }

                int genamount = generate.Count;
                int genprog = 0;

                await Parallel.ForEachAsync(generate, async (pos, token) =>
                {
                    Chunk c = new Chunk();

                    c.chunkPos = pos;

                    c.Generate();
                    c.CheckQueue(false);

                    loadedChunks.TryAdd(CCPos(pos), c);

                    genprog++;
                    prog.Value = (int)(((float)genprog / genamount) * 100);
                });
                UserInterface.Active.RemoveEntity(label);
                UserInterface.Active.RemoveEntity(prog);

                for(int y = 0; y < 8; y++)
                {
                    if (!loadedChunks.TryGetValue(CCPos((cx, y, cz)), out var c)) continue;

                    player.position.Y = float.Max((float)player.position.Y, c.MaxY);
                }

                worldSpawnpoint = player.position;
            }
        }
        public void QuitWorld()
        {
            chunkThreadCancel.Cancel();

            chunkUpdateThread.Interrupt();
            chunkUpdateThread.Join();
            chunkGenerateThread.Interrupt();
            chunkGenerateThread.Join();

            loadedChunks.Clear();

            player.Destroyed();

            currentPlayState = PlayState.Menu;
            TitleMenu.ReInit();
        }

        public static long CCPos((int x, int y, int z) pos)
        {
            const short shift = short.MaxValue/2;
            return (((long)((short)pos.z + shift) | ((long)((short)pos.y + shift) << 16) | ((long)((short)pos.x + shift) << 32)));
        }

        void ProcessMesh()
        {
            if (!toMesh.TryDequeue(out (int x, int y, int z) t)) return;

            loadedChunks[CCPos(t)].Remesh();
        }
        public void ProcessGeneration()
        {
            if (!toGenerate.TryDequeue(out (int x, int y, int z) t)) return;

            //if(Save.chunkJsonPaths.ContainsKey(CCPos(t)))
            //{
            //    Chunk chunk = JsonConvert.DeserializeObject<Chunk>(File.ReadAllText(Save.chunkJsonPaths[CCPos(t)]));
            //    chunk.generated = true;
            //    chunk.chunkPos = t;
            //    chunk.GenerateVisibility();
            //    Array.Fill(chunk.meshLayer, true);
            //    loadedChunks.TryAdd(CCPos(t), chunk);
            //    return;
            //}
            
            Chunk c = new Chunk();

            c.chunkPos = t;

            c.Generate(true);

            loadedChunks.TryAdd(CCPos(t), c);

            c.CheckQueue(true);
        }
        protected override void Update(GameTime gameTime)
        {
            dt = gamePaused ? 0 : (float)gameTime.ElapsedGameTime.TotalSeconds;

            FmodManager.Update();

            BetterKeyboard.GetState();
            BetterMouse.GetState(false);

            switch(currentPlayState)
            {
                case PlayState.Menu:

                    TitleMenu.Update();

                    break;
                case PlayState.LoadingWorld:
                case PlayState.World:

                    UpdateWorld();

                    break;
            }
            UserInterface.Active.Update(gameTime);

            base.Update(gameTime);
        }

        void UpdateWorld()
        {
            if (BetterKeyboard.HasBeenPressed(Keys.Escape) && !player.ExitOtherMenus())
            {
                gamePaused = !gamePaused;
                if(gamePaused)
                {
                    PauseMenu.Show();
                }
                else
                {
                    PauseMenu.Hide();
                }
            }

            if (BetterKeyboard.HasBeenPressed(Keys.F12)) TakeScreenCap();

            if(!gamePaused && !IsActive && !player.OtherMenusActive())
            {
                gamePaused = true;
                PauseMenu.Show();
            }

            if (!gamePaused && currentPlayState != PlayState.LoadingWorld)
            {
                WorldTimeManager.Tick();

                player.Update();
            }
            else
            {
                PauseMenu.Update();
            }
            frustum = new BoundingFrustum(world * view * projection);

            int camvoxel = GrabVoxel(cameraPosition+Vector3.UnitY*0.02f);
            if (camvoxel >= 0)cameraUnderwater = Voxel.voxelTypes[camvoxel].isLiquid;

            int cx;
            int cy;
            int cz;

            cx = (int)MathF.Floor(cameraPosition.X / Chunk.Size);
            cy = (int)MathF.Floor(cameraPosition.Y / Chunk.Size);
            cz = (int)MathF.Floor(cameraPosition.Z / Chunk.Size);
            playerChunkPos = (cx, cy, cz);

            toRender.Clear();

            const bool enableBFS = true;
            int generatedWithPreference = 0;
            if (enableBFS)
            {
                Queue<(int x, int y, int z, int face, int depth, bool fromEmpty)> bfsQueue = new Queue<(int, int, int, int, int, bool)>();
                HashSet<(int, int, int)> visitedChunks = new HashSet<(int, int, int)>();

                // Start the BFS with the current chunk and all its visible faces
                bfsQueue.Enqueue((cx, cy, cz, -1, 0, false)); // -1 means no face restriction initially

                while (bfsQueue.Count > 0)
                {
                    var (x, y, z, fromFace, depth, fromEmpty) = bfsQueue.Dequeue();
                    var cameraDir = new Vector3(cx, cy, cz) - new Vector3(x, y, z);

                    if (visitedChunks.Contains((x, y, z))) continue;

                    // Check if we have exceeded the render distance
                    float cDist = MathF.Abs(x - cx) + MathF.Abs(y - cy) + MathF.Abs(z - cz);
                    if (cDist >= RenderDistance + 4) continue;

                    visitedChunks.Add((x, y, z));

                    BoundingBox chunkbounds = new BoundingBox(new Vector3(x, y, z) * Chunk.Size, (new Vector3(x + 1, y + 1, z + 1) * Chunk.Size));
                    if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint)
                        continue;

                    Chunk currentChunk;

                    if (!loadedChunks.TryGetValue(CCPos((x, y, z)), out currentChunk))
                    {
                        if (generatedWithPreference > 256) continue;
                        if (toGenerate.Count > 16) continue;

                        // If chunk is not yet generated or queued, add it to generate queue
                        if (!toGenerate.Contains((x, y, z)))
                        {
                            toGenerate.Enqueue((x, y, z));
                        }

                        // Mark any existing neighbors, it's safer to not tunnel or fly with it. This is the "chunk zipping"
                        foreach (var (neighborX, neighborY, neighborZ, exitFace) in GetAllHorizontalNeighbors((x, y, z)))
                        {
                            // Calculate the new depth for the neighbor
                            int newDepth = depth + 1;

                            // Enqueue the neighbor only if it hasn't been visited and we are within the depth limit
                            if (!visitedChunks.Contains((neighborX, neighborY, neighborZ)) && !loadedChunks.TryGetValue(CCPos((neighborX, neighborY, neighborZ)), out _))
                            {
                                generatedWithPreference++;
                                bfsQueue.Enqueue((neighborX, neighborY, neighborZ, exitFace, newDepth, true));
                            }
                        }

                        continue;
                    }

                    if (!currentChunk.CompletelyEmpty)
                        toRender.Add((x, y, z));

                    // BFS to neighbors through visible faces
                    foreach (var (neighborX, neighborY, neighborZ, exitFace) in GetVisibleNeighbors(currentChunk, fromFace))
                    {
                        // Calculate the new depth for the neighbor
                        int newDepth = depth + 1;

                        var chunkDir = new Vector3(neighborX, neighborY, neighborZ) - new Vector3(x, y, z);

                        if (Vector3.Dot(chunkDir, cameraDir) > 0) continue;

                        // Enqueue the neighbor only if it hasn't been visited and we are within the depth limit
                        if (!visitedChunks.Contains((neighborX, neighborY, neighborZ)))
                        {
                            bfsQueue.Enqueue((neighborX, neighborY, neighborZ, exitFace, newDepth, currentChunk.CompletelyEmpty));
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
                    if ((fromFace == -1 && chunk.facesVisibleAtAll[TOP_FACE]) || (chunk.TestVisibility(TOP_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y + 1, chunk.chunkPos.z, BOTTOM_FACE));
                    if ((fromFace == -1 && chunk.facesVisibleAtAll[LEFT_FACE]) || (chunk.TestVisibility(LEFT_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x - 1, chunk.chunkPos.y, chunk.chunkPos.z, RIGHT_FACE));
                    if ((fromFace == -1 && chunk.facesVisibleAtAll[RIGHT_FACE]) || (chunk.TestVisibility(RIGHT_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x + 1, chunk.chunkPos.y, chunk.chunkPos.z, LEFT_FACE));
                    if ((fromFace == -1 && chunk.facesVisibleAtAll[FRONT_FACE]) || (chunk.TestVisibility(FRONT_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z - 1, BACK_FACE));
                    if ((fromFace == -1 && chunk.facesVisibleAtAll[BACK_FACE]) || (chunk.TestVisibility(BACK_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z + 1, FRONT_FACE));
                    if ((fromFace == -1 && chunk.facesVisibleAtAll[BOTTOM_FACE]) || (chunk.TestVisibility(BOTTOM_FACE, fromFace)))
                        neighbors.Add((chunk.chunkPos.x, chunk.chunkPos.y - 1, chunk.chunkPos.z, TOP_FACE));

                    return neighbors;
                }
                List<(int x, int y, int z, int face)> GetAllHorizontalNeighbors((int x, int y, int z) cPos)
                {
                    List<(int x, int y, int z, int face)> neighbors = new List<(int, int, int, int)>();

                    const int LEFT_FACE = 1;
                    const int RIGHT_FACE = 0;
                    const int TOP_FACE = 2;
                    const int BOTTOM_FACE = 3;
                    const int FRONT_FACE = 5;
                    const int BACK_FACE = 4;

                    neighbors.Add((cPos.x - 1, cPos.y, cPos.z, RIGHT_FACE));
                    neighbors.Add((cPos.x + 1, cPos.y, cPos.z, LEFT_FACE));
                    neighbors.Add((cPos.x, cPos.y, cPos.z - 1, BACK_FACE));
                    neighbors.Add((cPos.x, cPos.y, cPos.z + 1, FRONT_FACE));

                    return neighbors;
                }
            }

            for (int _x = -RenderDistance; _x < RenderDistance; _x++)
            {
                for (int _y = -RenderDistance; _y < RenderDistance; _y++)
                {
                    for (int _z = -RenderDistance; _z < RenderDistance; _z++)
                    {
                        int x = _x + cx;
                        int y = _y + cy;
                        int z = _z + cz;

                        // Check if we have exceeded the render distance
                        if (MathF.Abs(x - cx) + MathF.Abs(y - cy) * 2 + MathF.Abs(z - cz) >= RenderDistance || !loadedChunks.TryGetValue(CCPos((x,y,z)), out _)) continue;

                        EntityManager.UpdateChunk(CCPos((x,y,z)));
                    }
                }
            }
            ParticleSystemManager.UpdateSystems();

            while (instantRemesh.Count > 0)
            {
                var key = instantRemesh.Dequeue();
                loadedChunks[key].CheckQueue(true);
            }

        }

        void RenderChunks(bool transparent)
        {
            if (transparent) goto transparent;

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            foreach (var c in toRender.ToArray().AsSpan())
            {
                long pos = CCPos(c);

                if (!loadedChunks.TryGetValue(pos, out _)) continue;

                BoundingBox chunkbounds = new BoundingBox(new Vector3(c.x, c.y, c.z) * Chunk.Size, (new Vector3(c.x, c.y, c.z) * Chunk.Size + new Vector3(Chunk.Size, loadedChunks[pos].MaxY + 1, Chunk.Size)));
                if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint)
                    continue;

                int LOD = 0;

                if (loadedChunks[pos].chunkVertexBuffers[LOD] == null || loadedChunks[pos].chunkVertexBuffers[LOD].VertexCount == 0)
                    continue;

                GraphicsDevice.SetVertexBuffer(loadedChunks[pos].chunkVertexBuffers[LOD]);

                chunk.Parameters["World"].SetValue(world * Matrix.CreateTranslation(new Vector3(c.x, c.y, c.z) * Chunk.Size));

                foreach (var pass in chunk.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, loadedChunks[pos].chunkVertexBuffers[LOD].VertexCount / 3);
                }
            }

            if (!transparent) return;

            transparent:

            //Transparent
            foreach (var c in toRender.ToArray().AsSpan())
            {
                long pos = CCPos(c);
                if (!loadedChunks.TryGetValue(pos, out _)) continue;

                BoundingBox chunkbounds = new BoundingBox(new Vector3(c.x, c.y, c.z) * Chunk.Size, (new Vector3(c.x, c.y, c.z) * Chunk.Size + new Vector3(Chunk.Size, loadedChunks[pos].MaxY + 1, Chunk.Size)));
                if (frustum.Contains(chunkbounds) == ContainmentType.Disjoint)
                    continue;
                int LOD = 4;

                if (loadedChunks[pos].chunkVertexBuffers[LOD] == null || loadedChunks[pos].chunkVertexBuffers[LOD].VertexCount == 0)
                    continue;

                GraphicsDevice.SetVertexBuffer(loadedChunks[pos].chunkVertexBuffers[LOD]);

                chunk.Parameters["World"].SetValue(world * Matrix.CreateTranslation(new Vector3(c.x, c.y, c.z) * Chunk.Size));

                foreach (var pass in chunk.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, loadedChunks[pos].chunkVertexBuffers[LOD].VertexCount / 3);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (currentPlayState)
            {
                default:
                case PlayState.Menu:

                    TitleMenu.Render();

                    break;
                case PlayState.World:

                    RenderWorld();

                    break;
            }
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            UserInterface.Active.Draw(spriteBatch);

            base.Draw(gameTime);
        }

        void RenderWorld()
        {
            sunView = Matrix.CreateRotationX(MathHelper.ToRadians((WorldTimeManager.WorldTime / 15)));

            daylightPercentage = (float.Clamp(MathF.Sin(MathHelper.ToRadians((WorldTimeManager.WorldTime / 15) % 360)) * 2f - 0.4f, -0.98f, 0f) + 1);

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            GraphicsDevice.SamplerStates[0] = crunchy;
            GraphicsDevice.SamplerStates[1] = crunchy;
            GraphicsDevice.SamplerStates[2] = crunchy;
            GraphicsDevice.SamplerStates[3] = SamplerState.LinearClamp;

            sky.Parameters["cameraPosition"]?.SetValue(cameraPosition);
            sky.Parameters["cameraForward"]?.SetValue(cameraForward);
            sky.Parameters["sunDirection"]?.SetValue(sunView.Forward);

            var skyColor = WorldTimeManager.GetSkyColor();
            var skyBandColor = WorldTimeManager.GetSkyBandColor();

            sky.Parameters["skyColor"].SetValue(skyColor);
            sky.Parameters["skyBandColor"].SetValue(skyBandColor);

            chunk.Parameters["skyColor"].SetValue(skyColor);
            chunk.Parameters["skyBandColor"].SetValue(skyBandColor);

            chunk.Parameters["renderDistance"]?.SetValue((cameraUnderwater ? 2.5f : RenderDistance));
            chunk.Parameters["cameraPosition"]?.SetValue(cameraPosition);
            chunk.Parameters["time"]?.SetValue(totalTime);
            chunk.Parameters["sunDir"]?.SetValue(sunView.Forward);

            chunk.Parameters["sunColor"]?.SetValue(Color.White.ToVector3() * daylightPercentage);
            //Shadows
            /*
            chunk.Parameters["View"].SetValue(sunView);
            chunk.Parameters["Projection"].SetValue(sunProjection);
            
            chunk.CurrentTechnique = chunk.Techniques["Shadow"];
            
            GraphicsDevice.SetRenderTarget(shadowMap);
            GraphicsDevice.Clear(Color.White);

            Matrix lightWorld = Matrix.CreateWorld(Vector3.Floor(-cameraPosition/Chunk.Size)*Chunk.Size,Vector3.Forward,Vector3.Up);

            
            for (int _x = -8; _x < 8; _x++)
            {
                for (int _y = -8; _y < 8; _y++)
                {
                    for (int _z = -8; _z < 8; _z++)
                    {
                        int x = _x + playerChunkPos.x;
                        int y = _y + playerChunkPos.y;
                        int z = _z + playerChunkPos.z;

                        long c = CCPos((x, y, z));

                        if (!loadedChunks.ContainsKey(c) || !loadedChunks[c].generated)
                            continue;

                        int LOD = loadedChunks[c].GetLOD();

                        if ((loadedChunks[c].chunkVertexBuffers[LOD] == null || loadedChunks[c].chunkVertexBuffers[LOD].VertexCount == 0) && LOD > 0)
                            LOD--;
                        if (loadedChunks[c].chunkVertexBuffers[LOD] == null || loadedChunks[c].chunkVertexBuffers[LOD].VertexCount == 0)
                            continue;

                        GraphicsDevice.SetVertexBuffer(loadedChunks[c].chunkVertexBuffers[LOD]);

                        chunk.Parameters["World"].SetValue(lightWorld * Matrix.CreateTranslation(new Vector3(x, y, z) * Chunk.Size));

                        foreach (var pass in chunk.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, loadedChunks[c].chunkVertexBuffers[LOD].VertexCount / 3);
                        }
                    }
                }
            }

            chunk.CurrentTechnique = chunk.Techniques["Terrain"];
            */
            GraphicsDevice.SetRenderTargets(screenTexture, normalTexture);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.Stencil, Color.LightSkyBlue, 1000f, 1);


            sky.Parameters["World"].SetValue(Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) * Matrix.CreateScale(10));
            sky.Parameters["View"].SetValue(view);
            sky.Parameters["Projection"].SetValue(projection);

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            foreach (var pass in sky.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, skyboxVerts, 0, skyboxVerts.Length / 3);
            }

            basicEffect.TextureEnabled = true;
            basicEffect.Texture = sunTexture;

            basicEffect.World = (Matrix.CreateWorld(new Vector3(-0.5f, -0.5f, -5), Vector3.Forward, Vector3.Up) * Matrix.CreateScale(1) * (sunView) * Matrix.CreateRotationY(MathHelper.ToRadians(180)));
            basicEffect.View = (view);
            basicEffect.Projection = (projection);
            basicEffect.Alpha = 1.0f;
            basicEffect.DiffuseColor = Color.White.ToVector3();

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, flatSprite, 0, flatSprite.Length / 3);
            }
            basicEffect.World = (Matrix.CreateWorld(new Vector3(-0.5f, -0.5f, 15), Vector3.Forward, Vector3.Up) * Matrix.CreateScale(1) * (sunView) * Matrix.CreateRotationY(MathHelper.ToRadians(180)));
            basicEffect.Texture = moonTexture;

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, flatSprite, 0, flatSprite.Length / 3);
            }

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            chunk.Parameters["View"].SetValue(view);
            chunk.Parameters["Projection"].SetValue(projection);

            entity.Parameters["View"].SetValue(view);
            entity.Parameters["Projection"].SetValue(projection);
            entity.Parameters["skyColor"]?.SetValue(skyColor);
            entity.Parameters["skyBandColor"]?.SetValue(skyBandColor);
            entity.Parameters["renderDistance"]?.SetValue(RenderDistance);
            entity.Parameters["cameraPosition"]?.SetValue(cameraPosition);
            entity.Parameters["time"]?.SetValue(totalTime);
            entity.Parameters["sunDir"]?.SetValue(sunView.Forward);

            //chunk.Parameters["LightViewProj"]?.SetValue(lightWorld * sunView*sunProjection);
            //chunk.Parameters["shadowmap"]?.SetValue(shadowMap);

            RenderChunks(false);

            player.Render();

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var c in toRender.ToArray().AsSpan())
            {
                long pos = CCPos(c);

                EntityManager.RenderChunk(pos);
            }

            ParticleSystemManager.RenderSystems();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            RenderChunks(true);

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);

            player.RenderViewmodel();

            //GraphicsDevice.DepthStencilState = DepthStencilState.None;
            //foreach (var c in toRender.ToArray().AsSpan())
            //{
            //    effect.Alpha = 0.4f;
            //    effect.DiffuseColor = Color.Black.ToVector3();

            //    effect.Projection = MGame.Instance.projection;
            //    effect.View = MGame.Instance.view;

            //    effect.World = Matrix.CreateScale(Chunk.Size, Chunk.Size, Chunk.Size) * MGame.Instance.world * Matrix.CreateTranslation(new Vector3(c.x, c.y, c.z) * Chunk.Size);

            //    foreach (var pass in effect.CurrentTechnique.Passes)
            //    {
            //        pass.Apply();
            //        Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, boxVertices, 0, boxVertices.Length / 2);
            //    }
            //}

            GraphicsDevice.SetRenderTarget(SSAOTarget);

            postProcessing.Parameters["NormalTexture"]?.SetValue(normalTexture);
            postProcessing.Parameters["ScreenTexture"]?.SetValue(screenTexture);
            postProcessing.Parameters["waterOverlay"]?.SetValue(waterOverlay);
            postProcessing.Parameters["waterDUDV"]?.SetValue(waterDUDV);
            postProcessing.Parameters["underwater"]?.SetValue(cameraUnderwater);
            postProcessing.Parameters["screenSize"]?.SetValue(new Vector2(screenTexture.Width, screenTexture.Height));
            postProcessing.Parameters["cameraRotation"]?.SetValue(player.rotation);

            GraphicsDevice.SamplerStates[4] = SamplerState.PointWrap;
            GraphicsDevice.SamplerStates[5] = SamplerState.PointWrap;

            Matrix _view = Matrix.Identity;

            Matrix _projection = Matrix.CreateOrthographicOffCenter(0, SSAOTarget.Width, SSAOTarget.Height, 0, 0, 1);

            if (enableAO)
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
            postProcessing.CurrentTechnique = postProcessing.Techniques["SpriteBatch"];

            _spriteBatch.Begin(effect: postProcessing);

            _spriteBatch.Draw(screenTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            _spriteBatch.End();


            //_spriteBatch.Begin();

            //_spriteBatch.Draw(shadowMap, Vector2.Zero, Color.White);

            //_spriteBatch.End();

            player.RenderUI();
        }
        public void TakeScreenCap()
        {

            int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = GraphicsDevice.PresentationParameters.BackBufferHeight;

            //force a frame to be drawn (otherwise back buffer is empty) 
            Draw(new GameTime());

            //pull the picture from the buffer 
            int[] backBuffer = new int[w * h];
            GraphicsDevice.GetBackBufferData(backBuffer);

            //copy into a texture 
            Texture2D texture = new Texture2D(GraphicsDevice, w, h, false, GraphicsDevice.PresentationParameters.BackBufferFormat);
            texture.SetData(backBuffer);

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/fvoxel/screenshots")) Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/fvoxel/screenshots");

            //save to disk 
            Stream stream = File.OpenWrite($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/fvoxel/screenshots/{System.DateTime.Now:yyyyMMddHHmmss}.png");

            texture.SaveAsPng(stream, w, h);
            stream.Dispose();

            texture.Dispose();
        }
        public int GrabVoxel(Vector3 p)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.TryGetValue(CCPos((cx, cy, cz)), out var chunk) && chunk.generated)
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                return chunk.voxels[x + Chunk.Size * (y + Chunk.Size * z)];
            }
            return -1;
        }
        public bool GrabVoxelData(Vector3 p, out VoxelData data)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            data = new VoxelData {skyLight = 255 };

            if (loadedChunks.TryGetValue(CCPos((cx, cy, cz)), out var chunk) && chunk.generated)
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);
                data = chunk.voxeldata[x + Chunk.Size * (y + Chunk.Size * z)];
                return true;
            }
            return true;
        }
        public void UpdateBlock(Vector3 p)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.TryGetValue(CCPos((cx, cy, cz)), out var chunk))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                int vox = chunk.voxels[x + Chunk.Size * (y + Chunk.Size * z)];

                Voxel.voxelTypes[vox].myClass?.TryUpdateBlock((x,y,z), chunk);
            }
        }
        public void DigVoxel(Vector3 p, int toolLevel, bool disableDrops = false)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.TryGetValue(CCPos((cx, cy, cz)), out var chunk))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                if (!disableDrops)
                {
                    var t = Voxel.voxelTypes[chunk.voxels[x + Chunk.Size * (y + Chunk.Size * z)]];

                    int tex = t.bottomTexture;
                    PlayDigSound(t, p + Vector3.One * 0.5f);
                    ParticleSystemManager.AddSystem(new ParticleSystem(25, ParticleSystem.TextureProvider.BlockAtlas, tex, p + Vector3.One * 0.5f, Vector3.Up, 2f, 12f, Vector3.One * 0.25f, Vector3.One * 2));

                    if (t.droppedItemID >= 0 && t.requiredLevel <= toolLevel)
                    {
                        if (t.myClass == null || !t.myClass.customDrops)
                        {
                            var droppedItem = new DroppedItem(new Item { itemID = t.droppedItemID })
                            {
                                position = p + Vector3.One * 0.6f
                            };
                            droppedItem.position += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f) * 0.3f;
                            droppedItem.velocity += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, 2, (float)Random.Shared.NextDouble() * 2 - 1f);
                            droppedItem.gravity = 2f;

                            EntityManager.SpawnEntity(droppedItem);
                        }
                        else
                        {
                            var drops = t.myClass.GetCustomDrops();

                            if (drops is not null)
                            {
                                foreach (var d in drops)
                                {
                                    for (int i = 0; i < d.stack; i++)
                                    {
                                        var droppedItem = new DroppedItem(d)
                                        {
                                            position = p + Vector3.One * 0.6f
                                        };
                                        droppedItem.position += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f) * 0.3f;
                                        droppedItem.velocity += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, 2, (float)Random.Shared.NextDouble() * 2 - 1f);
                                        droppedItem.gravity = 2f;

                                        EntityManager.SpawnEntity(droppedItem);
                                    }
                                }
                            }
                        }
                    }
                }

                chunk.Modify(x, y, z, 0);
            }
        }

        public static void PlayDigSound(Voxel t, Vector3 p)
        {
            var instance = placeBlockEvents[(int)t.surfaceType - 1].CreateInstance();
            instance.Position3D = p;
            instance.Start();
        }
        public static void PlayWalkSound(Voxel t, Vector3 p)
        {
            var instance = walkBlockEvents[(int)t.surfaceType - 1].CreateInstance();
            instance.Position3D = p;
            instance.Start();
        }

        public void SetVoxel(Vector3 p, int newVoxel, bool disableDrops = false, Voxel.PlacementSettings placement = Voxel.PlacementSettings.ANY)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.TryGetValue(CCPos((cx, cy, cz)), out var chunk))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                if (newVoxel == 0 && !disableDrops)
                {
                    var t = Voxel.voxelTypes[chunk.voxels[x + Chunk.Size * (y + Chunk.Size * z)]];

                    int tex = t.bottomTexture;
                    PlayDigSound(t, p + Vector3.One * 0.5f);
                    ParticleSystemManager.AddSystem(new ParticleSystem(25, ParticleSystem.TextureProvider.BlockAtlas, tex, p + Vector3.One * 0.5f, Vector3.Up, 2f, 12f, Vector3.One * 0.25f, Vector3.One * 2));

                    if (t.droppedItemID >= 0)
                    {
                        if (t.myClass == null || !t.myClass.customDrops)
                        {
                            var droppedItem = new DroppedItem(new Item { itemID = t.droppedItemID })
                            {
                                position = p + Vector3.One * 0.6f
                            };
                            droppedItem.position += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f) * 0.3f;
                            droppedItem.velocity += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, 2, (float)Random.Shared.NextDouble() * 2 - 1f);
                            droppedItem.gravity = 2f;

                            EntityManager.SpawnEntity(droppedItem);
                        }
                        else
                        {
                            var drops = t.myClass.GetCustomDrops();

                            if(drops is not null)
                            {
                                foreach(var d in drops)
                                {
                                    for(int i = 0; i < d.stack; i++)
                                    {
                                        var droppedItem = new DroppedItem(d)
                                        {
                                            position = p + Vector3.One * 0.6f
                                        };
                                        droppedItem.position += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f, (float)Random.Shared.NextDouble() * 2 - 1f) * 0.3f;
                                        droppedItem.velocity += new Vector3((float)Random.Shared.NextDouble() * 2 - 1f, 2, (float)Random.Shared.NextDouble() * 2 - 1f);
                                        droppedItem.gravity = 2f;

                                        EntityManager.SpawnEntity(droppedItem);
                                    }
                                }
                            }
                        }
                    }
                }

                chunk.Modify(x, y, z, newVoxel, placement);
            }
        }
        public void SetVoxel_Q(Vector3 p, int newVoxel, bool instantRegen = false)
        {
            int cx = (int)MathF.Floor(p.X / Chunk.Size);
            int cy = (int)MathF.Floor(p.Y / Chunk.Size);
            int cz = (int)MathF.Floor(p.Z / Chunk.Size);

            if (loadedChunks.TryGetValue(CCPos((cx, cy, cz)), out var chunk))
            {
                int x = (int)(p.X - cx * Chunk.Size);
                int y = (int)(p.Y - cy * Chunk.Size);
                int z = (int)(p.Z - cz * Chunk.Size);

                chunk.ModifyQueue(x, y, z, newVoxel);

                if (instantRegen && !instantRemesh.Contains(CCPos((cx, cy, cz)))) instantRemesh.Enqueue(CCPos((cx, cy, cz)));
            }
            else if (!toGenerate.Contains((cx, cy, cz)))
            {
                toGenerate.Enqueue((cx, cy, cz));
            }
        }
    }
    public static class CollisionDetector
    {
        public static bool IsSolidTile(int x, int y, int z, bool raycast = false)
        {
            int v = MGame.Instance.GrabVoxel(new Vector3(x, y, z));

            if (v == -1) return false;

            return v != 0 && (raycast? !Voxel.voxelTypes[v].ignoreRaycast : !Voxel.voxelTypes[v].ignoreCollision);
        }
        public static Vector3Double ResolveCollision(BoundingBox aabb, Vector3Double position, ref Vector3 velocity)
        {
            // Get the bounds of the AABB
            Vector3Double min = aabb.Min + position;
            Vector3Double max = aabb.Max + position;

            // Calculate the AABB's current tile bounds
            int minX = (int)Math.Floor(min.X);
            int maxX = (int)Math.Ceiling(max.X);
            int minY = (int)Math.Floor(min.Y);
            int maxY = (int)Math.Ceiling(max.Y);
            int minZ = (int)Math.Floor(min.Z);
            int maxZ = (int)Math.Ceiling(max.Z);

            // Initialize resolved position and minimum overlap variables
            Vector3Double resolvedPosition = position; // Start with the original position
            double minOverlap = double.MaxValue;
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

                            bool flipx = x > (min.X + max.X) / 2f;
                            bool flipy = y > (min.Y + max.Y) / 2f;
                            bool flipz = z > (min.Z + max.Z) / 2f;
                            // Calculate overlaps for each axis
                            double overlapX = (flipx) ? max.X - x : min.X - (x + 1);
                            double overlapY = (flipy) ? max.Y - y : min.Y - (y + 1);
                            double overlapZ = (flipz) ? max.Z - z : min.Z - (z + 1);

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
                                resolvedPosition += ((float)minOverlap + float.Epsilon) * collisionNormal;
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

            // Return the resolved position
            return resolvedPosition;
        }

    }
}

public class BetterKeyboard
{
    static KeyboardState currentKeyState;
    static KeyboardState previousKeyState;

    public static KeyboardState GetState()
    {
        previousKeyState = currentKeyState;
        currentKeyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        return currentKeyState;
    }


    public static Keys[] GetPressedKeys()
    {
        return currentKeyState.GetPressedKeys();
    }

    public static bool IsPressed(Keys key)
    {
        return currentKeyState.IsKeyDown(key);
    }

    public static bool HasBeenPressed(Keys key)
    {
        return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
    }
    public static bool HasBeenReleased(Keys key)
    {
        return previousKeyState.IsKeyDown(key) && !currentKeyState.IsKeyDown(key);
    }
}

public class BetterMouse
{
    static MouseState currentMouseState;
    static MouseState previousMouseState;

    static float timeSinceLeftClick = 0;
    static float timeSinceRightClick = 0;
    static float timeSinceMiddleClick = 0;

    const int miliAllowedTime = 250; //0.25 of a second

    public static MouseState GetCurrentState() => currentMouseState;
    public static MouseState GetPreviousState() => previousMouseState;

    static bool wasleftDown, wasrightDown;
    static bool leftDown, rightDown;
    public static MouseState GetState(bool skipClicks)
    {
        previousMouseState = currentMouseState;
        currentMouseState = Mouse.GetState();

        if (skipClicks) return currentMouseState;

        wasleftDown = leftDown;
        wasrightDown = rightDown;

        if (currentMouseState.LeftButton == ButtonState.Pressed) { timeSinceLeftClick = DateTime.Now.Millisecond; leftDown = true; }
        if (currentMouseState.RightButton == ButtonState.Pressed) { timeSinceRightClick = DateTime.Now.Millisecond; rightDown = true; }
        if (currentMouseState.MiddleButton == ButtonState.Pressed) { timeSinceMiddleClick = DateTime.Now.Millisecond; }
        if (currentMouseState.LeftButton == ButtonState.Released) { timeSinceLeftClick = -1; leftDown = false; }
        if (currentMouseState.RightButton == ButtonState.Released) { timeSinceRightClick = -1; rightDown = false; }
        if (currentMouseState.MiddleButton == ButtonState.Released) { timeSinceMiddleClick = -1; }

        return currentMouseState;
    }

    public static bool WasLeftPressed() => leftDown == true && wasleftDown == false;
    public static bool WasRightPressed() => rightDown == true && wasrightDown == false;

    public static bool IsDraggingLeft()
    {
        return (timeSinceLeftClick > 0 && Math.Abs(timeSinceLeftClick - DateTime.Now.Millisecond) > miliAllowedTime);
    }
    public static bool IsDraggingRight()
    {
        return (timeSinceRightClick > 0 && Math.Abs(timeSinceRightClick - DateTime.Now.Millisecond) > miliAllowedTime);
    }
    public static bool IsDraggingMiddle()
    {
        return (timeSinceMiddleClick > 0 && Math.Abs(timeSinceMiddleClick - DateTime.Now.Millisecond) > miliAllowedTime);
    }

    public static Vector2 GetMovement()
    {
        return -new Vector2(currentMouseState.X - previousMouseState.X, currentMouseState.Y - previousMouseState.Y);
    }
}