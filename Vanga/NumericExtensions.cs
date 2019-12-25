using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robot
{
    public static class NumericExtensions
    {
        public static double ToRadians(this double angle)
            => (Math.PI / 180) * angle;

        public static double CalibrateAngle(this double angle)
        {
            if (angle > 180) angle -= 360;
            else if (angle < -180) angle += 360;

            return angle;
        }
    }
}
