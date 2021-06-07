
using Neo.SmartContract.Framework;
using System.Numerics;

namespace Ctoken
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is Exponential")]
    public partial class Ctoken : SmartContract
    {
        public static (MathError,Exp) getExp(BigInteger num, BigInteger denom)
        {
            (MathError err0, BigInteger scaledNumerator) = mulUInt(num, expScale);
            if(err0 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err0, exp);
            }
            (MathError err1, BigInteger rational) = divUInt(scaledNumerator, denom);
            if(err1 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err1, exp);
            }
            Exp expReturn = new Exp();
            expReturn.mantissa = 0;
            return (MathError.NO_ERROR, expReturn);
        }

        public static (MathError,Exp) addExp(Exp a,Exp b)
        {
            (MathError error, BigInteger result) = addUInt(a.mantissa, b.mantissa);
            Exp exp = new Exp();
            exp.mantissa = result;
            return (error, exp);
        }

        public static (MathError,Exp) subExp(Exp a,Exp b)
        {
            (MathError error, BigInteger result) = subUInt(a.mantissa, b.mantissa);
            Exp exp = new Exp();
            exp.mantissa = result;
            return (error, exp);
        }

        public static (MathError,Exp) mulScalar(Exp a, BigInteger scalar)
        {
            (MathError err0, BigInteger scaledMantissa) = mulUInt(a.mantissa, scalar);
            if(err0 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err0, exp);
            }
            Exp expReturn = new Exp();
            expReturn.mantissa = scaledMantissa;
            return (MathError.NO_ERROR, expReturn);
        }

        public static (MathError, BigInteger) mulScalarTruncate(Exp a, BigInteger scalar)
        {
            (MathError err, Exp product) = mulScalar(a, scalar);
            if (err != MathError.NO_ERROR)
            {
                return (err, 0);
            }
            return (MathError.NO_ERROR, truncate(product));
        }

        public static (MathError, BigInteger) mulScalarTruncateAddUInt(Exp a, BigInteger scalar, BigInteger addend)
        {
            (MathError err, Exp product) = mulScalar(a, scalar);
            if (err != MathError.NO_ERROR)
            {
                return (err, 0);
            }
            return addUInt(truncate(product), addend);
        }

        public static (MathError,Exp) divScalar(Exp a, BigInteger scalar)
        {
            (MathError err0, BigInteger descaledMantissa) = divUInt(a.mantissa, scalar);
            if (err0 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err0, exp);
            }
            Exp expReturn = new Exp();
            expReturn.mantissa = descaledMantissa;
            return (MathError.NO_ERROR, expReturn);
        }

        public static (MathError,Exp) divScalarByExp(BigInteger scalar,Exp divisor)
        {
            (MathError err0, BigInteger numerator) = mulUInt(expScale, scalar);
            if (err0 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err0, exp);
            }
            return getExp(numerator, divisor.mantissa);
        }

        public static (MathError, BigInteger) divScalarByExpTruncate(ulong scalar,Exp divisor)
        {
            (MathError err, Exp fraction) = divScalarByExp(scalar, divisor);
            if (err != MathError.NO_ERROR)
            {
                return (err, 0);
            }
            return (MathError.NO_ERROR, truncate(fraction));
        }

        public static (MathError,Exp) mulExp(Exp a,Exp b)
        {
            (MathError err0, BigInteger doubleScaledProduct) = mulUInt(a.mantissa, b.mantissa);
            if (err0 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err0, exp);
            }
            (MathError err1, BigInteger doubleScaledProductWithHalfScale) = addUInt(halfExpScale, doubleScaledProduct);
            if (err1 != MathError.NO_ERROR)
            {
                Exp exp = new Exp();
                exp.mantissa = 0;
                return (err1, exp);
            }
            (MathError err2, BigInteger product) = divUInt(doubleScaledProductWithHalfScale, expScale);
            //Assert(err2 == MathError.NO_ERROR);
            Exp expReturn = new Exp();
            expReturn.mantissa = product;
            return (MathError.NO_ERROR, expReturn);
        }

        public static (MathError,Exp) mulExp(BigInteger a, BigInteger b)
        {
            Exp expA = new Exp();
            Exp expB = new Exp();
            expA.mantissa = a;
            expB.mantissa = b;
            return mulExp(expA, expB);
        }

        public static (MathError,Exp) mulExp3(Exp a,Exp b,Exp c)
        {
            (MathError err, Exp ab) = mulExp(a, b);
            if (err != MathError.NO_ERROR)
            {
                return (err, ab);
            }
            return mulExp(ab, c);
        }

        public static (MathError,Exp) divExp(Exp a,Exp b)
        {
            return getExp(a.mantissa, b.mantissa);
        }
    }
}
