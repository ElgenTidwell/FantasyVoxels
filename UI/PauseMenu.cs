using FantasyVoxels.Saves;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FantasyVoxels.MGame;

namespace FantasyVoxels.UI
{
    public static class PauseMenu
    {
        static Panel mainMenu;
        static PauseMenu()
        {
            mainMenu = new Panel(new Vector2(800,800),PanelSkin.None);

            mainMenu.AddChild(new Button("Resume Game",anchor:Anchor.Center,offset:Vector2.UnitY*-128)).OnClick += (GeonBit.UI.Entities.Entity entity) =>
            {
                Mouse.SetPosition(Instance.GraphicsDevice.Viewport.Width / 2, Instance.GraphicsDevice.Viewport.Height / 2);
                Hide();
            };
            mainMenu.AddChild(new Button("Quit to Title")).OnClick += GoToMainMenu;
        }

        private static void GoToMainMenu(GeonBit.UI.Entities.Entity entity)
        {
            Hide();
            Instance.QuitWorld();
        }
        public static void Update()
        {

        }
        public static void Show()
        {
            Instance.IsMouseVisible = true;
            Instance.gamePaused = true;
            UserInterface.Active.AddEntity(mainMenu);
            Save.SaveToFile(Save.WorldName);
        }
        public static void Hide()
        {
            Instance.IsMouseVisible = false;
            UserInterface.Active.RemoveEntity(mainMenu);
            Instance.gamePaused = false;
        }
    }
}
