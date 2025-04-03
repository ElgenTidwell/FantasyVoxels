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
            ContinentalnessCurve.Keys.Add(new CurveKey(1, 60));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.2f, 16));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.1f, 90));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.04f, 80));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.1f, 25));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.3f, -25));
            ContinentalnessCurve.Keys.Add(new CurveKey(-1.0f, 60));

            ErosionCurve.Keys.Add(new CurveKey(-1, 3));
            ErosionCurve.Keys.Add(new CurveKey(-0.3f, 2));
            ErosionCurve.Keys.Add(new CurveKey(-0.1f, -1));
            ErosionCurve.Keys.Add(new CurveKey(0.85f, 0.2f));
            ErosionCurve.Keys.Add(new CurveKey(1, 2f));

            PVCurve.Keys.Add(new CurveKey(-1, 20));
            PVCurve.Keys.Add(new CurveKey(-0.5f, 0));
            PVCurve.Keys.Add(new CurveKey(0, 60));
            PVCurve.Keys.Add(new CurveKey(1, 30));

            DensityCurve.Keys.Add(new CurveKey(-30, 0.8f));
            DensityCurve.Keys.Add(new CurveKey(0, 0.7f));
            DensityCurve.Keys.Add(new CurveKey(30, 0.5f));
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
