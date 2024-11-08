using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static FantasyVoxels.MGame;

namespace FantasyVoxels
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
        static TitleMenu()
        {

        }
        public static void LoadContent()
        {
            tiledBackground = Instance.Content.Load<Effect>("Shaders/tiledBackground");
        }
        public static void Update()
        {
        }

        public static void Render()
        {
            Instance.GraphicsDevice.Clear(Color.Black);

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Instance.GraphicsDevice.Viewport.Width, Instance.GraphicsDevice.Viewport.Height, 0, 0, 1);
            Matrix uv_transform = GetUVTransform(Instance.colors,Vector2.Zero,4f,Instance.GraphicsDevice.Viewport);

            tiledBackground.Parameters["view_projection"].SetValue(Matrix.Identity * projection);
            tiledBackground.Parameters["screenSize"].SetValue(Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2());
            tiledBackground.Parameters["screenAspect"].SetValue(new Vector2(1,Instance.GraphicsDevice.Viewport.AspectRatio+1));
            tiledBackground.Parameters["texIndex"]?.SetValue(11);

            Instance.spriteBatch.Begin(samplerState:SamplerState.PointWrap, effect: tiledBackground);
            Instance.spriteBatch.Draw(Instance.colors,Instance.GraphicsDevice.Viewport.Bounds,new Color(Color.White*0.5f,1f));
            Instance.spriteBatch.End();
        }
    }
}
