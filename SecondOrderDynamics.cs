using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace InternalFPS
{
    public class SecondOrderDynamics
    {
        float xp;
        float y, yd;
        float k1, k2, k3;
        float T_crit;

        public SecondOrderDynamics(float f, float z, float r, float x0)
        {
            k1 = z / (MathF.PI*f);
            k2 = 1/ ((2*MathF.PI*f)*(2*MathF.PI*f));
            k3 = r * z / (2 * MathF.PI * f);

            T_crit = 0.8f * (MathF.Sqrt(4*k2 + k1*k1)-k1);

            xp = x0;
            y = x0;
            yd = 0f;
        }

        public float Update(float t, float x, float velocity = float.NaN)
        {
            float xd;

            if (float.IsNaN(velocity)) xd = (x - xp) / t;
            else xd = velocity;

            xp = x;
            int iterations = (int)MathF.Ceiling(t/T_crit);
            t = t / iterations;
            for(int i = 0; i < iterations; i++)
            {
                y = y + t * yd;
                yd = yd + t * (x + k3 * xd - y - k1 * yd) / k2;
            }

            return y;
        }
    }
}
