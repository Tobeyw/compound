
using Neo.SmartContract.Framework;

using System;
using System.Numerics;

namespace Ctoken
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is ExponentialNoError")]
    public partial class Ctoken : SmartContract
    {
        const ulong expScale = 1_000_000_000_000_000_000;
        const ulong amountScale = 100_000_000;
        const ulong halfExpScale = expScale / 2;
        const ulong mantissaOne = expScale;

        public struct Exp
        {
            public BigInteger mantissa;
        }

        public struct AmountExp {
            public BigInteger mantissa;
        }

        public static BigInteger truncate(Exp exp)
        {
            return exp.mantissa / expScale;
        }

        public static BigInteger mul_ScalarTruncate(Exp a, BigInteger scalar)
        {
            Exp product = mul_(a, scalar);
            return truncate(product);
        }

        public static BigInteger mul_ScalarTruncateAddUInt(Exp a, BigInteger scalar, BigInteger addend)
        {
            Exp product = mul_(a, scalar);
            return add_(truncate(product), addend);
        }

        public static Boolean lessThanExp(Exp left,Exp right)
        {
            return left.mantissa < right.mantissa;
        }

        public static Boolean lessThanOrEqualExp(Exp left,Exp right)
        {
            return left.mantissa <= right.mantissa;
        }

        public static Boolean greaterThanExp(Exp left, Exp right)
        {
            return left.mantissa > right.mantissa;
        }

        public static Boolean isZeroExp(Exp value)
        {
            return value.mantissa == 0;
        }

        /*public static BigInteger safe224(BigInteger n,string errorMessage)
        {
        }*/

        /*public static BigInteger safe32(BigInteger n,string errorMessage)
        {
        }*/

        public static Exp add_(Exp a,Exp b)
        {
            Exp exp = new Exp() ;
            exp.mantissa = add_(a.mantissa, b.mantissa);
            return exp;
        }

        public static AmountExp add_(AmountExp a, AmountExp b)
        {
            AmountExp amountExp = new AmountExp();
            amountExp.mantissa = add_(a.mantissa, b.mantissa);
            return amountExp;
        }

        public static BigInteger add_(BigInteger a,BigInteger b)
        {
            return add_(a, b, "addition overflow");
        }

        public static BigInteger add_(BigInteger a,BigInteger b,string errorMessage)
        {
            BigInteger c = a + b;
            if(c < a)
            {
                throw new Exception(errorMessage);
            }
            return c;
        }

        public static Exp sub_(Exp a,Exp b)
        {
            Exp exp = new Exp();
            exp.mantissa = sub_(a.mantissa, b.mantissa);
            return exp;
        }

        public static AmountExp sub_(AmountExp a, AmountExp b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = sub_(a.mantissa, b.mantissa);
            return doubleExp;
        }

        public static BigInteger sub_(BigInteger a,BigInteger b)
        {
            return sub_(a, b, "subtraction underflow");
        }

        public  static BigInteger sub_(BigInteger a,BigInteger b,string errorMessage)
        {
            if(b > a)
            {
                throw new Exception(errorMessage);
            }
            return a - b;
        }

        public static Exp mul_(Exp a,Exp b)
        {
            Exp exp = new Exp();
            exp.mantissa = mul_(a.mantissa, b.mantissa) / expScale;
            return exp;
        }

        public static Exp mul_(Exp a,BigInteger b)
        {
            Exp exp = new Exp();
            exp.mantissa = mul_(a.mantissa, b);
            return exp;
        }

        public static BigInteger mul_(BigInteger a,Exp b)
        {
            return mul_(a, b.mantissa) / expScale;
        }

        public static AmountExp mul_(AmountExp a, AmountExp b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = (mul_(a.mantissa, b.mantissa) / expScale)/expScale;
            return doubleExp;
        }

        public static AmountExp mul_(AmountExp a,BigInteger b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = mul_(a.mantissa, b);
            return doubleExp;
        }

        public static BigInteger mul_(BigInteger a, AmountExp b)
        {
            return (mul_(a, b.mantissa) / expScale) / expScale;
        }

        public static BigInteger mul_(BigInteger a, BigInteger b)
        {
            return mul_(a, b, "multiplication overflow");
        }

        public static BigInteger mul_(BigInteger a, BigInteger b,string errorMessage)
        {
            if(a == 0 || b == 0)
            {
                return 0;
            }
            BigInteger c = a * b;
            if(c/a != b)
            {
                throw new Exception(errorMessage);
            }
            return c;
        }

        public static Exp div_(Exp a,Exp b)
        {
            Exp exp = new Exp();
            exp.mantissa = div_(mul_(a.mantissa, expScale), b.mantissa);
            return exp;
        }

        public static Exp div_(Exp a, BigInteger b)
        {
            Exp exp = new Exp();
            exp.mantissa = div_(a.mantissa, b);
            return exp;
        }

        public static BigInteger div_(BigInteger a,Exp b)
        {
            return div_(mul_(a, expScale), b.mantissa);
        }

        public static AmountExp div_(AmountExp a, AmountExp b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = div_(mul_(a.mantissa, amountScale), b.mantissa);
            return doubleExp;
        }

        public static AmountExp div_(AmountExp a, BigInteger b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = div_(a.mantissa, b);
            return doubleExp;
        }

        public static BigInteger div_(BigInteger a, AmountExp b)
        {
            return div_(mul_(a, amountScale), b.mantissa);
        }

        public static BigInteger div_(BigInteger a, BigInteger b)
        {
            return div_(a, b, "divided by zero");
        }

        public static BigInteger div_(BigInteger a, BigInteger b, string errorMessage)
        {
            if(b <= 0)
            {
                throw new Exception(errorMessage);
            }
            return a / b;
        }

        public static AmountExp fraction(BigInteger a, BigInteger b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = div_(mul_(a, amountScale), b);
            return doubleExp;
        }
    }
}
