using System;

namespace TF.State.Domain
{
    // This is to prevent sin, cos, atan2, pow  to produce different result in windows and linux
    // That would desynch the game
    //
    // Sin/Cos/Atan/Atan2 kernels and coefficients are ported from fdlibm 5.3 (https://www.netlib.org/fdlibm/):
    // "Copyright (C) 1993 by Sun Microsystems, Inc. All rights reserved. Developed at SunSoft, a Sun Microsystems,
    // Inc. business. Permission to use, copy, modify, and distribute this software is freely granted,
    // provided that this notice is preserved."
    public static class DeterministicMath
    {
        private const double PiOver4 = 0.7853981633974483;
        private const double TwoPi = 6.283185307179586;
        private const double InvPiOver2 = 6.36619772367581382433e-01;
        private const double Pio2Hi = 1.57079632673412561417e+00;
        private const double Pio2Lo = 6.07710050650619224932e-11;

        private const double S1 = -1.66666666666666324348e-01;
        private const double S2 = 8.33333333332248946124e-03;
        private const double S3 = -1.98412698298579493134e-04;
        private const double S4 = 2.75573137070700676789e-06;
        private const double S5 = -2.50507602534068634195e-08;
        private const double S6 = 1.58969099521155010221e-10;

        private const double Cc1 = 4.16666666666666019037e-02;
        private const double Cc2 = -1.38888888888741095749e-03;
        private const double Cc3 = 2.48015872894767294178e-05;
        private const double Cc4 = -2.75573143513906633035e-07;
        private const double Cc5 = 2.08757232129817482790e-09;
        private const double Cc6 = -1.13596475577881948265e-11;

        public static double Sin(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x))
            {
                return double.NaN;
            }

            double ax = Math.Abs(x);
            if (ax <= PiOver4)
            {
                return KernelSin(x);
            }

            if (ax > 1e6)
            {
                x %= TwoPi;
            }

            int n = ReducePiOver2(x, out double r);
            return n switch
            {
                0 => KernelSin(r),
                1 => KernelCos(r),
                2 => -KernelSin(r),
                _ => -KernelCos(r),
            };
        }

        public static double Cos(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x))
            {
                return double.NaN;
            }

            double ax = Math.Abs(x);
            if (ax <= PiOver4)
            {
                return KernelCos(x);
            }

            if (ax > 1e6)
            {
                x %= TwoPi;
            }

            int n = ReducePiOver2(x, out double r);
            return n switch
            {
                0 => KernelCos(r),
                1 => -KernelSin(r),
                2 => -KernelCos(r),
                _ => KernelSin(r),
            };
        }

        private static int ReducePiOver2(double x, out double r)
        {
            double fn = Math.Floor(x * InvPiOver2 + 0.5);
            r = (x - fn * Pio2Hi) - fn * Pio2Lo;
            return ((int)(fn % 4.0) + 4) % 4;
        }

        private static double KernelSin(double r)
        {
            double z = r * r;
            double v = z * r;
            double poly = S2 + z * (S3 + z * (S4 + z * (S5 + z * S6)));
            return r + v * (S1 + z * poly);
        }

        private static double KernelCos(double r)
        {
            double z = r * r;
            double poly = Cc1 + z * (Cc2 + z * (Cc3 + z * (Cc4 + z * (Cc5 + z * Cc6))));
            double hz = 0.5 * z;
            double w = 1.0 - hz;
            return w + (((1.0 - w) - hz) + z * z * poly);
        }

        private const double AT0 = 3.33333333333329318027e-01;
        private const double AT1 = -1.99999999998764832476e-01;
        private const double AT2 = 1.42857142725034663711e-01;
        private const double AT3 = -1.11111104054623557880e-01;
        private const double AT4 = 9.09088713343650656196e-02;
        private const double AT5 = -7.69187620504482999495e-02;
        private const double AT6 = 6.66107313738753120669e-02;
        private const double AT7 = -5.83357013379057348645e-02;
        private const double AT8 = 4.97687799461593236017e-02;
        private const double AT9 = -3.65315727442169155270e-02;
        private const double AT10 = 1.62858201153657823623e-02;

        private static readonly double[] AtanHi =
        {
            4.63647609000806093515e-01,
            7.85398163397448278999e-01,
            9.82793723247329054082e-01,
            1.57079632679489655800e+00,
        };

        private static readonly double[] AtanLo =
        {
            2.26987774529616870924e-17,
            3.06161699786838301793e-17,
            1.39033110312309984516e-17,
            6.12323399573676603587e-17,
        };

        private const double Pi = 3.14159265358979311600e+00;
        private const double PiLo = 1.2246467991473531772e-16;

        private static double Atan(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            double sign = x < 0.0 ? -1.0 : 1.0;
            double ax = Math.Abs(x);

            if (ax >= 7.3786976294838206e19)
            {
                return sign * (AtanHi[3] + AtanLo[3]);
            }

            int id;
            if (ax < 0.4375)
            {
                if (ax < 3.725290298461914e-09)
                {
                    return x;
                }
                id = -1;
            }
            else if (ax < 0.6875)
            {
                id = 0;
                ax = (2.0 * ax - 1.0) / (2.0 + ax);
            }
            else if (ax < 1.1875)
            {
                id = 1;
                ax = (ax - 1.0) / (ax + 1.0);
            }
            else if (ax < 2.4375)
            {
                id = 2;
                ax = (ax - 1.5) / (1.0 + 1.5 * ax);
            }
            else
            {
                id = 3;
                ax = -1.0 / ax;
            }

            double z = ax * ax;
            double w = z * z;
            double s1 = z * (AT0 + w * (AT2 + w * (AT4 + w * (AT6 + w * (AT8 + w * AT10)))));
            double s2 = w * (AT1 + w * (AT3 + w * (AT5 + w * (AT7 + w * AT9))));

            if (id < 0)
            {
                return sign * (ax - ax * (s1 + s2));
            }

            double t = AtanHi[id] - ((ax * (s1 + s2) - AtanLo[id]) - ax);
            return sign * t;
        }

        public static double Atan2(double y, double x)
        {
            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return double.NaN;
            }

            if (x > 0.0 && !double.IsInfinity(x) && !double.IsInfinity(y))
            {
                if (y == 0.0)
                {
                    return y;
                }
                return Atan(y / x);
            }

            int m = (y < 0.0 ? 1 : 0) | (x < 0.0 ? 2 : 0);

            if (y == 0.0)
            {
                return m switch
                {
                    0 => y,
                    1 => y,
                    2 => Pi,
                    _ => -Pi,
                };
            }

            if (x == 0.0)
            {
                return y > 0.0 ? AtanHi[3] + AtanLo[3] : -(AtanHi[3] + AtanLo[3]);
            }

            if (double.IsInfinity(x) || double.IsInfinity(y))
            {
                double xi = double.IsInfinity(x) ? (x > 0 ? 1.0 : -1.0) : 0.0;
                double yi = double.IsInfinity(y) ? (y > 0 ? 1.0 : -1.0) : 0.0;
                if (xi != 0.0 && yi != 0.0)
                {
                    return yi * (xi > 0 ? PiOver4 : 3.0 * PiOver4);
                }
                if (yi != 0.0)
                {
                    return yi * (AtanHi[3] + AtanLo[3]);
                }
                return xi > 0 ? (y < 0 ? -0.0 : 0.0) : (y < 0 ? -Pi : Pi);
            }

            double z = Atan(Math.Abs(y / x));
            return m switch
            {
                0 => z,
                1 => -z,
                2 => Pi - (z - PiLo),
                _ => (z - PiLo) - Pi,
            };
        }

        private const double Ln2Hi = 6.93147180369123816490e-01;
        private const double Ln2Lo = 1.90821492927058770002e-10;
        private const double InvLn2 = 1.44269504088896338700e+00;
        private const double Sqrt2 = 1.4142135623730951;

        public static double Pow(double x, double y)
        {
            if (y == 0.0 || x == 1.0)
            {
                return 1.0;
            }

            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return double.NaN;
            }

            if (x == 0.0)
            {
                return y > 0.0 ? 0.0 : double.PositiveInfinity;
            }

            double sign = 1.0;
            if (x < 0.0)
            {
                if (Math.Floor(y) != y || double.IsInfinity(y))
                {
                    return double.NaN;
                }
                x = -x;
                if (Math.Floor(y * 0.5) != y * 0.5)
                {
                    sign = -1.0;
                }
            }

            if (Math.Floor(y) == y && Math.Abs(y) <= 512.0)
            {
                return sign * PowInt(x, (long)y);
            }

            return sign * Exp2(y * Log2(x));
        }

        private static double PowInt(double x, long n)
        {
            bool invert = n < 0;
            ulong m = (ulong)Math.Abs(n);
            double acc = 1.0;
            double b = x;
            while (m > 0)
            {
                if ((m & 1) != 0)
                {
                    acc *= b;
                }
                b *= b;
                m >>= 1;
            }
            return invert ? 1.0 / acc : acc;
        }

        private static double Log2(double x)
        {
            long bits = BitConverter.DoubleToInt64Bits(x);
            int exp = (int)((bits >> 52) & 0x7FF);
            if (exp == 0)
            {
                x *= 4.503599627370496e15;
                bits = BitConverter.DoubleToInt64Bits(x);
                exp = (int)((bits >> 52) & 0x7FF) - 52;
            }

            int k = exp - 1023;
            double m = BitConverter.Int64BitsToDouble((bits & 0xFFFFFFFFFFFFFL) | 0x3FF0000000000000L);
            if (m > Sqrt2)
            {
                m *= 0.5;
                k += 1;
            }

            double s = (m - 1.0) / (m + 1.0);
            double z = s * s;
            double lnm = s * (2.0 + z * (0.6666666666666666 + z * (0.4 + z * (0.2857142857142857
                + z * (0.2222222222222222 + z * (0.18181818181818182 + z * (0.15384615384615385
                + z * (0.13333333333333333 + z * (0.11764705882352941 + z * (0.10526315789473684
                + z * 0.09523809523809523))))))))));
            return k + lnm * InvLn2;
        }

        private static double Exp2(double v)
        {
            if (double.IsNaN(v))
            {
                return double.NaN;
            }
            if (v >= 1024.0)
            {
                return double.PositiveInfinity;
            }
            if (v <= -1075.0)
            {
                return 0.0;
            }

            double n = Math.Floor(v + 0.5);
            double f = v - n;
            double r = f * Ln2Hi + f * Ln2Lo;
            double e = 1.0 + r * (1.0 + r * (0.5 + r * (0.16666666666666666 + r * (0.041666666666666664
                + r * (0.008333333333333333 + r * (0.001388888888888889 + r * (1.984126984126984e-4
                + r * (2.48015873015873e-5 + r * (2.7557319223985893e-6 + r * (2.755731922398589e-7
                + r * (2.505210838544172e-8 + r * (2.08767569878681e-9 + r * 1.6059043836821613e-10))))))))))));
            return Scale2(e, (int)n);
        }

        private static double Scale2(double a, int n)
        {
            if (n >= -1022)
            {
                return a * BitConverter.Int64BitsToDouble((long)(n + 1023) << 52);
            }
            return a * BitConverter.Int64BitsToDouble((long)(n + 1023 + 54) << 52) * 5.551115123125783e-17;
        }
    }
}
