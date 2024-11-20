using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public static class WorldTimeManager
    {
        static float wTime = 0;
        public static float WorldTime => wTime;
        public static bool NightTime = wTime > TURNOVER;

        public static void SetWorldTime(float time) => wTime = time;

        static Color daySkyColor = new Color(163, 209, 255), nightSkyColor = new Color(5,5,7);
        static Color dawnColor = new Color(255, 147, 117), duskColor = new Color(60,60,65);

        public static Vector3 GetSkyColor()
        {
            float dayPerc = (float.Clamp(MathF.Sin(MathHelper.ToRadians((WorldTime / 15) % 360)) * 2,-1,1) + 1) / 2f;
            var lerpedCol = Color.Lerp(daySkyColor,nightSkyColor, 1-dayPerc);
            return lerpedCol.ToVector3();
        }
        public static Vector3 GetSkyBandColor()
        {
            float dayPerc = (float.Clamp(MathF.Sin(MathHelper.ToRadians((WorldTime / 15) % 360)) * 5, -1, 1) + 1) / 2f;
            float bandPerc = float.Pow((MathF.Cos(MathHelper.ToRadians((WorldTime / 15) % 360)*2) + 1) / 2f, 6);

            var lerpedCol = Color.Lerp(Color.Lerp(dawnColor, daySkyColor, 1 - bandPerc), Color.Lerp(duskColor, nightSkyColor, 1 - bandPerc), 1-dayPerc);
            return lerpedCol.ToVector3();
        }

        const int TURNOVER = 15 * 180;

        public static void Tick()
        {
            wTime += MGame.dt*4;

            wTime = wTime % (TURNOVER*2);
        }
    }
}
