#if UNITY_5
#define UNITY
#endif

#if UNITY
using System;
using UnityEngine;
#else
using System;
#endif

namespace Lunar.Utils
{
    public static class MathUtils
    {
#if UNITY
        public const float PI = Mathf.PI;
        public const float Deg2Rad = Mathf.Deg2Rad;
#else
    public const float PI = 3.14159265359f;
    public const float Deg2Rad = PI / 180.0f;
#endif

        #region CORE
        public static int Sign(float x)
        {
            if (x == 0)
            {
                return 0;
            }

            return x < 0 ? -1 : 1;
        }

        public static float Frac(float x)
        {
#if UNITY
            return x - Mathf.Floor(x);
#else
        return x - (int)(x);
#endif
        }

        public static float Round(float x)
        {
#if UNITY
            return Mathf.Round(x);
#else
        return (float)Math.Round(x);
#endif
        }


#if UNITY
        public static Vector4 Frac(Vector4 v)
        {
            return new Vector4(Frac(v.x), Frac(v.y), Frac(v.z), Frac(v.w));
        }
#endif

        public static float Sqr(float x)
        {
            return x * x;
        }

        public static float Sqrt(float x)
        {
#if UNITY
            return Mathf.Sqrt(x);
#else
        return (float)Math.Sqrt(x);
#endif
        }

        public static float Pow(float f, float p)
        {
#if UNITY
            return Mathf.Pow(f, p);
#else
        return (float)Math.Pow(f, p);
#endif
        }

        public static float Log(float x, float power)
        {
#if UNITY
            return Mathf.Log(x, power);
#else
        return (float)Math.Log(x, power);
#endif
        }

        public static float Log(float x)
        {
#if UNITY
            return Mathf.Log(x);
#else
        return (float)Math.Log(x);
#endif
        }

        public static float Abs(float x)
        {
#if UNITY
            return Mathf.Abs(x);
#else
        return (float)Math.Abs(x);
#endif
        }

        public static int Abs(int x)
        {
#if UNITY
            return Mathf.Abs(x);
#else
        return Math.Abs(x);
#endif
        }

        public static float Floor(float x)
        {
#if UNITY
            return Mathf.Floor(x);
#else
        return (float)Math.Floor(x);
#endif
        }


        public static float Sin(float x)
        {
#if UNITY
            return Mathf.Sin(x);
#else
        return (float)Math.Sin(x);
#endif
        }

        public static float Cos(float x)
        {
#if UNITY
            return Mathf.Cos(x);
#else
        return (float)Math.Cos(x);
#endif
        }

        public static float Asin(float x)
        {
#if UNITY
            return Mathf.Asin(x);
#else
        return (float)Math.Asin(x);
#endif
        }

        public static float Acos(float x)
        {
#if UNITY
            return Mathf.Acos(x);
#else
        return (float)Math.Acos(x);
#endif
        }


        public static float Min(float a, float b)
        {
#if UNITY
            return Mathf.Min(a, b);
#else
        return (float)Math.Min(a, b);
#endif
        }

        public static float Max(float a, float b)
        {
#if UNITY
            return Mathf.Max(a, b);
#else
        return (float)Math.Max(a, b);
#endif
        }

        public static int Min(int a, int b)
        {
#if UNITY
            return Mathf.Min(a, b);
#else
        return Math.Min(a, b);
#endif
        }

        public static int Max(int a, int b)
        {
#if UNITY
            return Mathf.Max(a, b);
#else
        return Math.Max(a, b);
#endif
        }

        public static void GetDirectionForAngle(float angle, float speed, out float dx, out float dy)
        {
            dx = Cos(angle) * speed;
            dy = Sin(angle) * speed;
        }

        #endregion

        #region DISTANCES
        public static float Distance(float x1, float y1, float x2, float y2)
        {
            float dx = x1 - x2;
            float dy = y1 - y2;
            dx *= dx;
            dy *= dy;
            return Sqrt(dx + dy);
        }

#if UNITY
        public static float Distance(Vector2 A, Vector2 B)
        {
            return Distance(A.x, A.y, B.x, B.y);
        }
#endif

        public static float DotProduct(float x1, float y1, float x2, float y2)
        {
            return x1 * x2 + y1 * y2;
        }

        public static float Angle(float x1, float y1, float x2, float y2)
        {
            return Acos(DotProduct(x1, y1, x2, y2));
        }

        #endregion

        #region CURVES
        public static float SmoothCurveWithOffset(float delta, float offset)
        {
            if (delta < offset)
            {
                delta = (delta / offset);
                return Abs(Sin(delta * PI * 0.5f));
            }
            else
            {
                delta = delta - offset;
                delta = (delta / (1.0f - offset));
                return Abs(Cos(delta * PI * 0.5f));
            }
        }

        public static float SmoothCurve(float delta)
        {
            return Abs(Sin(delta * PI));
        }

        public static float CubicInterpolate(float y0, float y1, float y2, float y3, float mu)
        {
            float mu2 = (mu * mu);
            float a0 = y3 - y2 - y0 + y1;
            float a1 = y0 - y1 - a0;
            float a2 = y2 - y0;
            float a3 = y1;
            return (a0 * mu * mu2) + (a1 * mu2) + (a2 * mu) + a3;
        }

        public static float CatmullRomInterpolate(float y0, float y1, float y2, float y3, float mu)
        {
            float mu2 = (mu * mu);
            float a0 = (-0.5f * y0) + (1.5f * y1) - (1.5f * y2) + (0.5f * y3);
            float a1 = y0 - (2.5f * y1) + (2.0f * y2) - (0.5f * y3);
            float a2 = (-0.5f * y0) + (0.5f * y2);
            float a3 = y1;
            return (a0 * mu * mu2) + (a1 * mu2) + (a2 * mu) + a3;
        }

        public static float HermiteInterpolate(float pA, float pB, float vA, float vB, float u)
        {
            float u2 = (u * u);
            float u3 = u2 * u;
            float B0 = 2.0f * u3 - 3.0f * u2 + 1.0f;
            float B1 = -2.0f * u3 + 3.0f * u2;
            float B2 = u3 - 2.0f * u2 + u;
            float B3 = u3 - u;
            return (B0 * pA + B1 * pB + B2 * vA + B3 * vB);

        }

        public static float QuadraticBezierCurve(float y0, float y1, float y2, float mu)
        {

            return Sqr(1 - mu) * y0 + 2 * (1 - mu) * y1 + Sqr(mu) * y2;
        }

        public static float CubicBezierCurve(float y0, float y1, float y2, float y3, float mu)
        {
            return (1 - mu) * Sqr(1 - mu) * y0 + 3 * Sqr(1 - mu) * y1 + 3 * (1 - mu) * Sqr(mu) * y2 + Sqr(mu) * mu * y3; ;
        }
        #endregion

        #region LERPING
        public static float Lerp(float min, float max, float delta)
        {
#if UNITY
            return Mathf.Lerp(min, max, delta);
#else
        delta = delta > 1 ? 1 : delta < 0 ? 0 : delta;
        return min * delta + max * (1.0f- delta);
#endif
        }

        public static float InverseLerp(float min, float max, float value)
        {
#if UNITY
            return Mathf.InverseLerp(min, max, value);
#else
        return (value - min) / (max - min);        
#endif
        }


        // Some quadrilateral with position vectors a, b, c, and d.
        // a---b
        // |     |
        // c---d

        // u = relative position on the "horizontal" axis between a and b, or c and d.
        // v = relative position on the "vertical" axis between a and c, or b and d.

#if UNITY
        public static Vector3 BilinearLerp(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
        {
            Vector3 ab = Vector3.Lerp(a, b, u); // interpolation of a and b by u
            Vector3 cd = Vector3.Lerp(c, d, u); // interpolation of c and d by u
            return Vector3.Lerp(ab, cd, v); // interpolation of the interpolation of a and b and c and d by u, by v
        }
#endif

        #endregion


        #region COLOR
#if UNITY
        //NOTE: values only valid for 1024 x 32
        private static Vector3 coord_scale = new Vector4(0.0302734375f, 0.96875f, 31.0f);
        private static Vector4 coord_offset = new Vector4(0.00048828125f, 0.015625f, 0.0f, 0.0f);
        private static Vector2 texel_height_X0 = new Vector2(0.03125f, 0.0f);

        private static Color LUTSample(Texture2D LUT, int red0, int green0, int blue, float u, float v)
        {
            int red1 = red0 < 31 ? red0 + 1 : red0;
            int green1 = green0 < 31 ? green0 + 1 : green0;

            Color c00 = LUT.GetPixel(blue * 32 + red0, green0);
            Color c10 = LUT.GetPixel(blue * 32 + red1, green0);
            Color c11 = LUT.GetPixel(blue * 32 + red1, green1);
            Color c01 = LUT.GetPixel(blue * 32 + red0, green1);

            Color ab = Color.Lerp(c00, c10, u); // interpolation of a and b by u
            Color cd = Color.Lerp(c01, c11, u); // interpolation of c and d by u
            return Color.Lerp(ab, cd, v); // interpolation of the interpolation of a and b and c and d by u, by v
        }

        public static Color32 LUTTransform(Color32 color, Texture2D LUT)
        {
            /*Vector4 coord = new Vector4(color.r * coord_scale.x, color.g * coord_scale.y, color.b * coord_scale.z, 0);
            coord += coord_offset;

            Vector4 coord_frac = Frac(coord);
            Vector4 coord_floor = coord - coord_frac;

            Vector2 coord_bot = new Vector2(coord.x + coord_floor.z * texel_height_X0.x, coord.y + coord_floor.z * texel_height_X0.y);
            Vector2 coord_top = coord_bot + texel_height_X0;

            Color lutcol_bot = LUT.GetPixelBilinear(coord_bot.x, coord_bot.y);
            Color lutcol_top = LUT.GetPixelBilinear(coord_top.x, coord_top.y);

            //Color lutcol_bot = LUT.GetPixel((int)(coord_bot.x * LUT.width), (int)(coord_bot.y * LUT.height));
            //Color lutcol_top = LUT.GetPixel((int)(coord_top.x * LUT.width), (int)(coord_top.x * LUT.height));

            return Color.Lerp(lutcol_bot, lutcol_top, coord_frac.z);
            */

            float div = 1.0f / 8.0f;
            float red = (float)color.r * div;
            float green = (float)color.g * div;
            float blue = (float)color.b * div;

            float u = Frac(red);
            float v = Frac(green);
            float w = Frac(blue);

            int x = Mathf.FloorToInt(red);
            int y = Mathf.FloorToInt(green);
            int z0 = Mathf.FloorToInt(blue);
            int z1 = z0 < 31 ? z0 + 1 : z0;

            Color A = LUTSample(LUT, x, y, z0, u, v);
            Color B = LUTSample(LUT, x, y, z0, u, v);

            return Color.Lerp(A, B, w);
        }

#endif
        #endregion

        #region RANDOM
        /// <summary>
        ///   Generates normally distributed numbers. Each operation makes two Gaussians for the price of one, and apparently they can be cached or something for better performance, but who cares.
        /// </summary>
        /// <param name="r"></param>
        /// <param name = "mu">Mean of the distribution</param>
        /// <param name = "sigma">Standard deviation</param>
        /// <returns></returns>
        public static float RandomGaussian(float mu = 0, float sigma = 1)
        {
            var u1 = RandomFloat(0, 1);
            var u2 = RandomFloat(0, 1);

            var rand_std_normal = Sqrt(-2.0f * Log(u1)) * Sin(2.0f * PI * u2);

            var rand_normal = mu + sigma * rand_std_normal;

            return rand_normal;
        }

        public static float RandomAngle(float minDegrees, float maxDegrees, float step = 1.0f)
        {
            return Deg2Rad * RandomFloat(minDegrees, maxDegrees);
        }

#if !UNITY
    private static Random _random = new Random();
#endif

        public static float RandomFloat(float min, float max)
        {
#if UNITY
            return UnityEngine.Random.Range(min, max);
#else
        return min + (float)(_random.NextDouble() * (max - min));
#endif
        }

        /// <summary>
        /// Returns a value between min and max - 1 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int RandomInt(int min, int max)
        {
#if UNITY
            return UnityEngine.Random.Range(min, max);
#else
        return min + _random.Next(max - min);
#endif
        }

        public static T RandomEnum<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(RandomInt(0, v.Length));
        }

        #endregion

    }
}
