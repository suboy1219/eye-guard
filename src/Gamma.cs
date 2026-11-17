using System;

namespace EyeGuard
{
    internal static class Gamma
    {
        private static Native.RAMP _saved;
        private static bool _hasSaved;
        private static bool _active;

        public static bool IsActive
        {
            get { return _active; }
        }

        public static bool Apply(double strength)
        {
            if (strength < 0) strength = 0;
            if (strength > 1) strength = 1;

            IntPtr dc = Native.ScreenDC();
            if (dc == IntPtr.Zero) return false;

            try
            {
                if (!_hasSaved)
                {
                    Native.RAMP current = Native.RAMP.Create();
                    if (Native.GetGammaRamp(dc, ref current))
                    {
                        _saved = current;
                        _hasSaved = true;
                    }
                }

                if (strength <= 0.001)
                {
                    return Restore(dc);
                }

                Native.RAMP ramp = Native.RAMP.Create();
                double gamma = 1.0 + 0.06 * strength;
                double redWeight = 1.0;
                double greenWeight = 1.0 - 0.11 * strength;
                double blueWeight = 1.0 - 0.29 * strength;

                for (int i = 0; i < 256; i++)
                {
                    double value = i / 255.0;
                    double shaped = Math.Pow(value, gamma);
                    ramp.Red[i] = Scale(shaped * redWeight);
                    ramp.Green[i] = Scale(shaped * greenWeight);
                    ramp.Blue[i] = Scale(shaped * blueWeight);
                }

                bool ok = Native.SetGammaRamp(dc, ref ramp);
                _active = ok;
                return ok;
            }
            catch
            {
                _active = false;
                return false;
            }
            finally
            {
                Native.ReleaseScreenDC(dc);
            }
        }

        public static bool Restore()
        {
            IntPtr dc = Native.ScreenDC();
            if (dc == IntPtr.Zero) return false;
            try
            {
                return Restore(dc);
            }
            finally
            {
                Native.ReleaseScreenDC(dc);
            }
        }

        private static bool Restore(IntPtr dc)
        {
            try
            {
                Native.RAMP ramp;
                if (_hasSaved)
                {
                    ramp = _saved;
                }
                else
                {
                    ramp = Native.RAMP.Create();
                    for (int i = 0; i < 256; i++)
                    {
                        ushort v = (ushort)(i * 257);
                        ramp.Red[i] = v;
                        ramp.Green[i] = v;
                        ramp.Blue[i] = v;
                    }
                }

                bool ok = Native.SetGammaRamp(dc, ref ramp);
                _active = false;
                return ok;
            }
            catch
            {
                _active = false;
                return false;
            }
        }

        private static ushort Scale(double value)
        {
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            return (ushort)Math.Round(value * 65535.0);
        }
    }
}
