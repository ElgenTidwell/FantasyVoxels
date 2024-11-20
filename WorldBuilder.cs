using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public static class WorldBuilder
    {
        public static Curve ContinentalnessCurve = new Curve();
        public static Curve ErosionCurve = new Curve();
        public static Curve PVCurve = new Curve();
        public static Curve DensityCurve = new Curve();

        static FastNoiseLite humid = new FastNoiseLite();
        static FastNoiseLite temp = new FastNoiseLite();

        static WorldBuilder()
        {
            ContinentalnessCurve.Keys.Add(new CurveKey(1, 350));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.3f, 178));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.28f, 90));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.04f, 80));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.0f, 25));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.1f, 15));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.2f, 0));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.8f, -25));
            ContinentalnessCurve.Keys.Add(new CurveKey(-1.0f, 60));

            ErosionCurve.Keys.Add(new CurveKey(-1, 3));
            ErosionCurve.Keys.Add(new CurveKey(-0.3f, 2));
            ErosionCurve.Keys.Add(new CurveKey(-0.1f, -1));
            ErosionCurve.Keys.Add(new CurveKey(0.85f, 0.02f));
            ErosionCurve.Keys.Add(new CurveKey(1, 0.4f));

            PVCurve.Keys.Add(new CurveKey(-1, -400));
            PVCurve.Keys.Add(new CurveKey(-0.5f, -70));
            PVCurve.Keys.Add(new CurveKey(0, 30));
            PVCurve.Keys.Add(new CurveKey(1, 400));

            DensityCurve.Keys.Add(new CurveKey(-30, 0.8f));
            DensityCurve.Keys.Add(new CurveKey(0, 0.7f));
            DensityCurve.Keys.Add(new CurveKey(30, 0.4f));
            DensityCurve.Keys.Add(new CurveKey(100, 0.2f));
            DensityCurve.Keys.Add(new CurveKey(140, 0.0f));

            humid.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
            temp.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        }

        public static float GetHumidity(float x, float z)
        {
            return (humid.GetNoise(x, z) + 1)/2;
        }
        public static float GetTemperature(float x, float z)
        {
            return (temp.GetNoise(x, z)+1)/2;
        }
    }
}
