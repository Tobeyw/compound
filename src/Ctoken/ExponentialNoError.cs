
using Neo.SmartContract.Framework;

using System;
using System.ComponentModel;
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
        [DisplayName("add_Exp")]
        public static Exp add_(Exp a,Exp b)
        {
            Exp exp = new Exp() ;
            exp.mantissa = add_(a.mantissa, b.mantissa);
            return exp;
        }
        [DisplayName("add_AmountExp")]
        public static AmountExp add_(AmountExp a, AmountExp b)
        {
            AmountExp amountExp = new AmountExp();
            amountExp.mantissa = add_(a.mantissa, b.mantissa);
            return amountExp;
        }
        [DisplayName("add_BigInteger")]
        public static BigInteger add_(BigInteger a,BigInteger b)
        {
            return add_(a, b, "addition overflow");
        }
        [DisplayName("add_BigIntegerMsg")]
        public static BigInteger add_(BigInteger a,BigInteger b,string errorMessage)
        {
            BigInteger c = a + b;
            if(c < a)
            {
                throw new Exception(errorMessage);
            }
            return c;
        }
        [DisplayName("sub_Exp")]
        public static Exp sub_(Exp a,Exp b)
        {
            Exp exp = new Exp();
            exp.mantissa = sub_(a.mantissa, b.mantissa);
            return exp;
        }
        [DisplayName("sub_AmountExp")]
        public static AmountExp sub_(AmountExp a, AmountExp b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = sub_(a.mantissa, b.mantissa);
            return doubleExp;
        }
        [DisplayName("sub_BigInteger")]
        public static BigInteger sub_(BigInteger a,BigInteger b)
        {
            return sub_(a, b, "subtraction underflow");
        }
        [DisplayName("sub_BigIntegerMsg")]
        public  static BigInteger sub_(BigInteger a,BigInteger b,string errorMessage)
        {
            if(b > a)
            {
                throw new Exception(errorMessage);
            }
            return a - b;
        }
        [DisplayName("mul_Exp")]
        public static Exp mul_(Exp a,Exp b)
        {
            Exp exp = new Exp();
            exp.mantissa = mul_(a.mantissa, b.mantissa) / expScale;
            return exp;
        }
        [DisplayName("mul_ExpBigInteger")]
        public static Exp mul_(Exp a,BigInteger b)
        {
            Exp exp = new Exp();
            exp.mantissa = mul_(a.mantissa, b);
            return exp;
        }
        [DisplayName("mul_BigIntegerExp")]
        public static BigInteger mul_(BigInteger a,Exp b)
        {
            return mul_(a, b.mantissa) / expScale;
        }
        [DisplayName("mul_AmountExp")]
        public static AmountExp mul_(AmountExp a, AmountExp b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = (mul_(a.mantissa, b.mantissa) / expScale)/expScale;
            return doubleExp;
        }
        [DisplayName("mul_AmountExpBigInteger")]
        public static AmountExp mul_(AmountExp a,BigInteger b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = mul_(a.mantissa, b);
            return doubleExp;
        }
        [DisplayName("mul_BigIntegerAmountExp")]
        public static BigInteger mul_(BigInteger a, AmountExp b)
        {
            return (mul_(a, b.mantissa) / expScale) / expScale;
        }
        [DisplayName("mul_BigInteger")]
        public static BigInteger mul_(BigInteger a, BigInteger b)
        {
            return mul_(a, b, "multiplication overflow");
        }
        [DisplayName("mul_BigIntegerMsg")]
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
        [DisplayName("div_Exp")]
        public static Exp div_(Exp a,Exp b)
        {
            Exp exp = new Exp();
            exp.mantissa = div_(mul_(a.mantissa, expScale), b.mantissa);
            return exp;
        }
        [DisplayName("div_ExpBigInteger")]
        public static Exp div_(Exp a, BigInteger b)
        {
            Exp exp = new Exp();
            exp.mantissa = div_(a.mantissa, b);
            return exp;
        }
        [DisplayName("div_BigIntegerExp")]
        public static BigInteger div_(BigInteger a,Exp b)
        {
            return div_(mul_(a, expScale), b.mantissa);
        }
        [DisplayName("div_AmountExp")]
        public static AmountExp div_(AmountExp a, AmountExp b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = div_(mul_(a.mantissa, amountScale), b.mantissa);
            return doubleExp;
        }
        [DisplayName("div_AmountExpBigInteger")]
        public static AmountExp div_(AmountExp a, BigInteger b)
        {
            AmountExp doubleExp = new AmountExp();
            doubleExp.mantissa = div_(a.mantissa, b);
            return doubleExp;
        }
        [DisplayName("div_BigIntegerAmountExp")]
        public static BigInteger div_(BigInteger a, AmountExp b)
        {
            return div_(mul_(a, amountScale), b.mantissa);
        }
        [DisplayName("div_BigInteger")]
        public static BigInteger div_(BigInteger a, BigInteger b)
        {
            return div_(a, b, "divided by zero");
        }
        [DisplayName("div_BigIntegerMsg")]
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
