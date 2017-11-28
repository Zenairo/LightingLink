using RGB.NET.Core;
using System;

namespace LightingLink
{
    class ColorUtils
    {
        public static Color colorMixer(Color c1, Color c2)
        {

            int _r = Math.Min((c1.R + c2.R), 255);
            int _g = Math.Min((c1.G + c2.G), 255);
            int _b = Math.Min((c1.B + c2.B), 255);

            return new Color(Convert.ToByte(_r),
                             Convert.ToByte(_g),
                             Convert.ToByte(_b));
        }
    }
}
