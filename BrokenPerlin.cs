using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Icaria.Engine.Procedural.IcariaNoise;

namespace FantasyVoxels
{
    public static class BrokenPerlin3D
    {
        private static readonly byte[] Perm;

        static BrokenPerlin3D()
        {
            Perm = new byte[512];
            Random random = new Random(72);
            byte[] p = new byte[256];
            for (int i = 0; i < 256; i++) p[i] = (byte)i;
            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }
            for (int i = 0; i < 512; i++) Perm[i] = p[i & 255];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float t, float a, float b) => a + t * (b - a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float Noise(float x, float y, float z, int seed)
        {
            int X = (FastFloor(x) + seed * 15731) & 255;
            int Y = FastFloor(y) & 255;
            int Z = FastFloor(z) & 255;

            x -= FastFloor(x);
            y -= FastFloor(y);
            z -= FastFloor(z);

            float u = Fade(x), v = Fade(y), w = Fade(z);

            int A = Perm[X] + Y, AA = Perm[A] + Z, AB = Perm[A + 1] + Z;
            int B = Perm[X + 1] + Y, BA = Perm[B] + Z, BB = Perm[B + 1] + Z;

            return Lerp(w,
                Lerp(v,
                    Lerp(u, Grad(Perm[AA], x, y, z), Grad(Perm[BA], x - 1, y, z)),
                    Lerp(u, Grad(Perm[AB], x, y - 1, z), Grad(Perm[BB], x - 1, y - 1, z))
                ),
                Lerp(v,
                    Lerp(u, Grad(Perm[AA + 1], x, y, z - 1), Grad(Perm[BA + 1], x - 1, y, z - 1)),
                    Lerp(u, Grad(Perm[AB + 1], x, y - 1, z - 1), Grad(Perm[BB + 1], x - 1, y - 1, z - 1))
                )
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float x) => (x >= 0) ? (int)x : (int)x - 1;
    }
}
