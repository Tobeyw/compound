
using Neo.SmartContract.Framework;
using System.Numerics;

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

        public static (MathError, BigInteger) mulUInt(BigInteger a, BigInteger b)
        {
            if(a == 0)
            {
                return (MathError.NO_ERROR, 0);
            }

            BigInteger c = a * b;

            if(c / a != b)
            {
                return (MathError.INTEGER_OVERFLOW, 0);
            }
            else
            {
                return (MathError.NO_ERROR, c);
            }
        }

        public static (MathError, BigInteger) divUInt(BigInteger a, BigInteger b)
        {
            if(b == 0)
            {
                return (MathError.DIVISION_BY_ZERO, 0);
            }
            return (MathError.NO_ERROR, a / b);
        }

        public static (MathError, BigInteger) subUInt(BigInteger a, BigInteger b)
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

        public static (MathError, BigInteger) addUInt(BigInteger a, BigInteger b)
        {
            BigInteger c = a + b;
            if (c >= a)
            {
                return (MathError.NO_ERROR, 0);
            }
            else
            {
                return (MathError.INTEGER_OVERFLOW, 0);
            }
        }

        public static (MathError, BigInteger) addThenSubUInt(BigInteger a, BigInteger b, BigInteger c)
        {
            (MathError err0, BigInteger sum) = addUInt(a, b);
            if (err0 != MathError.NO_ERROR)
            {
                return (err0, 0);
            }
            return subUInt(sum, c);
        }
    }
}
