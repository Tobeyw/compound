
using Neo.SmartContract.Framework;


namespace Ctoken
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is careful math")]
    public partial class Ctoken : SmartContract
    {
        public enum MathError
        {
            NO_ERROR,
            DIVISION_BY_ZERO,
            INTEGER_OVERFLOW,
            INTEGER_UNDERFLOW
        }

        public static (MathError,ulong) mulUInt(ulong a,ulong b)
        {
            if(a == 0)
            {
                return (MathError.NO_ERROR, 0);
            }

            ulong c = a * b;

            if(c / a != b)
            {
                return (MathError.INTEGER_OVERFLOW, 0);
            }
            else
            {
                return (MathError.NO_ERROR, c);
            }
        }

        public static (MathError,ulong) divUInt(ulong a,ulong b)
        {
            if(b == 0)
            {
                return (MathError.DIVISION_BY_ZERO, 0);
            }
            return (MathError.NO_ERROR, a / b);
        }

        public static (MathError,ulong) subUInt(ulong a,ulong b)
        {
            if(b <= a)
            {
                return (MathError.NO_ERROR, a - b);
            }
            else
            {
                return (MathError.INTEGER_UNDERFLOW, 0);
            }
        }

        public static (MathError,ulong) addUInt(ulong a,ulong b)
        {
            ulong c = a + b;
            if (c >= a)
            {
                return (MathError.NO_ERROR, 0);
            }
            else
            {
                return (MathError.INTEGER_OVERFLOW, 0);
            }
        }

        public static (MathError,ulong) addThenSubUInt(ulong a,ulong b,ulong c)
        {
            (MathError err0, ulong sum) = addUInt(a, b);
            if (err0 != MathError.NO_ERROR)
            {
                return (err0, 0);
            }
            return subUInt(sum, c);
        }
    }
}
