using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using FantasyVoxels.Saves;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static FantasyVoxels.MGame;

namespace FantasyVoxels.UI
{
    public static class TitleMenu
    {
        static Effect tiledBackground;
        private static Matrix GetView()
        {
            int width = Instance.GraphicsDevice.Viewport.Width;
            int height = Instance.GraphicsDevice.Viewport.Height;
            Vector2 origin = new(width / 2f, height / 2f);

            return
                Matrix.CreateTranslation(-origin.X, -origin.Y, 0f) *
                Matrix.CreateScale(1f) *
                Matrix.CreateTranslation(origin.X, origin.Y, 0f);
        }
        private static Matrix GetUVTransform(Texture2D t, Vector2 offset, float scale, Viewport v)
        {
            return
                Matrix.CreateScale(t.Width, t.Height, 1f) *
                Matrix.CreateScale(scale, scale, 1f) *
                Matrix.CreateTranslation(offset.X, offset.Y, 0f) *
                GetView() *
                Matrix.CreateScale(1f / v.Width, 1f / v.Height, 1f);
        }


        private static Panel mainMenu;
        private static Panel worldBrowser;
        public static Panel optionsMenu;

        private static SelectList worldBrowserWorlds;
        private static Panel worldCreator;

        private static TextInput worldName, worldSeed;

        enum CurrentMenu
        {
            Title,
            Options,
            WorldBrowser,
            WorldCreation
        };

        static CurrentMenu currentMenu;

        static GeonBit.UI.Entities.Entity prevMenuBeforeOptions;

        static TitleMenu()
        {
            mainMenu = new Panel(new Vector2(1024, 800), PanelSkin.None);

            mainMenu.AddChild(new Image(MGame.Instance.titleTexture, new Vector2(560,128)*2, ImageDrawMode.Stretch, Anchor.TopCenter));

            mainMenu.AddChild(new Button("Singleplayer", ButtonSkin.Alternative, Anchor.Center)).OnClick += GoToWorldBrowser;
            mainMenu.AddChild(new Button("Options", ButtonSkin.Alternative)).OnClick += GoToOptions;
            mainMenu.AddChild(new Button("Quit Game", ButtonSkin.Alternative)).OnClick += QuitPressed;


            worldBrowser = new Panel(new Vector2(1512, 800), PanelSkin.Default, Anchor.Center);
            worldBrowser.AddChild(new Label("Select World", Anchor.TopLeft));

            worldBrowserWorlds = new SelectList(new Vector2(1400, 600), Anchor.Center, skin: PanelSkin.Simple);
            worldBrowser.AddChild(worldBrowserWorlds);

            worldBrowser.AddChild(new Button("New World", anchor: Anchor.BottomRight, skin: ButtonSkin.Alternative, size: new Vector2(300, 70))).OnClick += GoToWorldCreation;
            worldBrowser.AddChild(new Button("Delete World", anchor: Anchor.BottomLeft, skin: ButtonSkin.Alternative, size: new Vector2(300, 70), offset: Vector2.UnitX * 316)).OnClick += TryDeleteWorld;
            worldBrowser.AddChild(new Button("Play World", anchor: Anchor.BottomRight, skin: ButtonSkin.Alternative, size: new Vector2(300, 70), offset: Vector2.UnitX * 316)).OnClick += TryPlayWorld;
            worldBrowser.AddChild(new Button("Cancel", anchor: Anchor.BottomLeft, skin: ButtonSkin.Alternative, size: new Vector2(300, 70))).OnClick += GoToTitle;

            worldCreator = new Panel(new Vector2(1512, 800), PanelSkin.Default);
            worldCreator.AddChild(new Label("Create World", Anchor.TopLeft));

            worldCreator.AddChild(new Label("World Name:", anchor: Anchor.AutoCenter));
            worldName = new TextInput(false, new Vector2(512, 70), Anchor.AutoCenter, skin: PanelSkin.Default);
            worldName.PlaceholderText = "New World";
            worldCreator.AddChild(worldName);

            worldCreator.AddChild(new Label("World Seed (Required):", anchor: Anchor.AutoCenter, offset: Vector2.UnitY * 200));
            worldSeed = new TextInput(false, new Vector2(512, 70), Anchor.AutoCenter, skin: PanelSkin.Default);
            worldCreator.AddChild(worldSeed);

            worldCreator.AddChild(new Button("Cancel", anchor: Anchor.BottomLeft, skin: ButtonSkin.Alternative, size: new Vector2(300, 70))).OnClick += GoToWorldBrowser;
            worldCreator.AddChild(new Button("Create New World", anchor: Anchor.BottomRight, skin: ButtonSkin.Alternative, size: new Vector2(300, 70))).OnClick += CreateNewWorld;


            //Options
            optionsMenu = new Panel(new Vector2(1024, 800), PanelSkin.None, Anchor.Center);

            optionsMenu.AddChild(new Button("Back", ButtonSkin.Alternative, Anchor.BottomLeft)).OnClick = (GeonBit.UI.Entities.Entity entity) => { HideOptions(); };

            var rdistbutton = new Button($"View Distance: {Options.renderDistance}", ButtonSkin.Alternative, Anchor.TopLeft, new Vector2(480,70));
            optionsMenu.AddChild(rdistbutton).OnClick = (GeonBit.UI.Entities.Entity entity) =>
            {
                int index = Array.IndexOf(Enum.GetValues(Options.renderDistance.GetType()), Options.renderDistance);
                index++;
                if (index >= Enum.GetValues(Options.renderDistance.GetType()).Length) index = 0;

                Options.RenderDistance newdistance = (Options.RenderDistance)(Enum.GetValues(Options.renderDistance.GetType())).GetValue(index);
                Options.SetRenderDistance(newdistance);

                rdistbutton.ButtonParagraph.Text = $"View Distance: {Options.renderDistance}";
            };
            var smlightbutton = new Button($"Smooth Lighting {(Options.smoothLightingEnable ? "ON" : "OFF")}", ButtonSkin.Alternative, Anchor.AutoInline, new Vector2(480, 70));
            optionsMenu.AddChild(smlightbutton).OnClick = (GeonBit.UI.Entities.Entity entity) =>
            {
                Options.SetSmoothLighting(!Options.smoothLightingEnable);
                TriggerRelight();
                smlightbutton.ButtonParagraph.Text = $"Smooth Lighting {(Options.smoothLightingEnable ? "ON" : "OFF")}";
            };
        }

        private static void TriggerRelight()
        {
            if (Instance.loadedChunks != null && !Instance.loadedChunks.IsEmpty)
            {
                Parallel.ForEach(Instance.loadedChunks, (chunk) =>
                {
                    Instance.loadedChunks[chunk.Key].lightOutOfDate = true;
                    Instance.loadedChunks[chunk.Key].chunkVertexBuffers[0] = null;
                });
            }
        }

        private static void GoToWorldCreation(GeonBit.UI.Entities.Entity entity)
        {
            currentMenu = CurrentMenu.WorldCreation;
            UserInterface.Active.RemoveEntity(worldBrowser);
            UserInterface.Active.AddEntity(worldCreator);
        }
        private static void GoToTitle(GeonBit.UI.Entities.Entity entity)
        {
            currentMenu = CurrentMenu.Title;
            UserInterface.Active.RemoveEntity(worldBrowser);
            UserInterface.Active.AddEntity(mainMenu);
        }

        private static void GoToWorldBrowser(GeonBit.UI.Entities.Entity entity)
        {
            currentMenu = CurrentMenu.WorldBrowser;
            UserInterface.Active.AddEntity(worldBrowser);
            if (mainMenu.Parent != null) UserInterface.Active.RemoveEntity(mainMenu);
            if (worldCreator.Parent != null) UserInterface.Active.RemoveEntity(worldCreator);

            worldBrowserWorlds.ClearIcons();
            worldBrowserWorlds.ClearItems();

            string[] saves = Save.GetAllSavedWorlds();

            if (saves == null || saves.Length == 0) return;

            worldBrowserWorlds.Items = saves;
        }
        private static void GoToOptions(GeonBit.UI.Entities.Entity entity)
        {
            currentMenu = CurrentMenu.Options;

            if (mainMenu.Parent != null) UserInterface.Active.RemoveEntity(mainMenu);
            if (worldBrowser.Parent != null) UserInterface.Active.RemoveEntity(worldBrowser);
            if (worldCreator.Parent != null) UserInterface.Active.RemoveEntity(worldCreator);

            ShowOptions(mainMenu);
        }
        private static void QuitPressed(GeonBit.UI.Entities.Entity entity)
        {
            Instance.Exit();
        }
        private static async void TryPlayWorld(GeonBit.UI.Entities.Entity entity)
        {
            if (string.IsNullOrEmpty(worldBrowserWorlds.SelectedValue)) return;

            Save.LoadSave(worldBrowserWorlds.SelectedValue);
        }
        private static void TryDeleteWorld(GeonBit.UI.Entities.Entity entity)
        {
            if (string.IsNullOrEmpty(worldBrowserWorlds.SelectedValue)) return;

            Save.DeleteSave(worldBrowserWorlds.SelectedValue);

            var l = worldBrowserWorlds.Items.ToList(); l.Remove(worldBrowserWorlds.SelectedValue);

            worldBrowserWorlds.Items = l.ToArray();
        }
        private static async void CreateNewWorld(GeonBit.UI.Entities.Entity entity)
        {
            if (string.IsNullOrEmpty(worldName.TextParagraph.Text)) return;
            if (string.IsNullOrEmpty(worldSeed.TextParagraph.Text)) return;
			WorldTimeManager.SetWorldTime(0f);
            Save.WorldName = worldName.TextParagraph.Text;
            Instance.seed = (int.TryParse(worldSeed.TextParagraph.Text, out int seed) ? seed : worldSeed.TextParagraph.Text.GetHashCode());

            Mouse.SetPosition(200, 200);

            var label = new Label("Loading World...", Anchor.Center);
            UserInterface.Active.AddEntity(label);

            await Instance.LoadWorld();

            UserInterface.Active.RemoveEntity(label);
            Instance.currentPlayState = PlayState.World;
        }

        public static void ShowOptions(GeonBit.UI.Entities.Entity external)
        {
            prevMenuBeforeOptions = external;
            UserInterface.Active.AddEntity(optionsMenu);
        }
        public static void HideOptions()
        {
            Options.SaveOptions();
            if (optionsMenu.Parent != null) UserInterface.Active.RemoveEntity(optionsMenu);
            UserInterface.Active.AddEntity(prevMenuBeforeOptions);
        }

        public static void LoadContent()
        {
            tiledBackground = Instance.Content.Load<Effect>("Shaders/tiledBackground");
        }
        public static void ReInit()
        {
            UserInterface.Active.AddEntity(mainMenu);
            currentMenu = CurrentMenu.Title;
            Instance.IsMouseVisible = true;
        }
        public static void Cleanup()
        {
            if (mainMenu.Parent != null) UserInterface.Active.RemoveEntity(mainMenu);
            if (worldBrowser.Parent != null) UserInterface.Active.RemoveEntity(worldBrowser);
            if (worldCreator.Parent != null) UserInterface.Active.RemoveEntity(worldCreator);
        }
        public static void Update()
        {
        }

        public static void Render()
        {
            Instance.GraphicsDevice.Clear(Color.Black);

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Instance.GraphicsDevice.Viewport.Width, Instance.GraphicsDevice.Viewport.Height, 0, 0, 1);
            Matrix uv_transform = GetUVTransform(Instance.colors, Vector2.Zero, 4f, Instance.GraphicsDevice.Viewport);

            tiledBackground.Parameters["view_projection"].SetValue(Matrix.Identity * projection);
            tiledBackground.Parameters["screenSize"].SetValue(Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2());
            tiledBackground.Parameters["screenAspect"].SetValue(new Vector2(1, Instance.GraphicsDevice.Viewport.AspectRatio + 1));
            tiledBackground.Parameters["texIndex"]?.SetValue(currentMenu == CurrentMenu.WorldCreation || currentMenu == CurrentMenu.WorldBrowser ? 8 : 11);
            tiledBackground.Parameters["atlasSize"]?.SetValue(MGame.AtlasSize);

            Instance.spriteBatch.Begin(samplerState: SamplerState.PointWrap, effect: tiledBackground);
            Instance.spriteBatch.Draw(Instance.colors, Instance.GraphicsDevice.Viewport.Bounds, new Color(Color.White * 0.1f, 1f));
            Instance.spriteBatch.End();
        }
    }
}
