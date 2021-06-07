
using Neo.SmartContract.Framework;

using System;


namespace Ctoken
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is ExponentialNoError")]
    public partial class Ctoken : SmartContract
    {
        const ulong expScale = 1_000_000_00;
        const ulong doubleScale = 1_000_000_000_000_000_0;
        const ulong halfExpScale = expScale / 2;
        const ulong mantissaOne = expScale;

        public struct Exp
        {
            public ulong mantissa;
        }

        public struct Double {
            public ulong mantissa;
        }

        public static ulong truncate(Exp exp)
        {
            return exp.mantissa / expScale;
        }

        public static ulong mul_ScalarTruncate(Exp a,ulong scalar)
        {
            Exp product = mul_(a, scalar);
            return truncate(product);
        }

        public static ulong mul_ScalarTruncateAddUInt(Exp a,ulong scalar,ulong addend)
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

        /*public static ulong safe224(ulong n,string errorMessage)
        {
        }*/

        /*public static ulong safe32(ulong n,string errorMessage)
        {
        }*/

        public static Exp add_(Exp a,Exp b)
        {
            Exp exp = new Exp() ;
            exp.mantissa = add_(a.mantissa, b.mantissa);
            return exp;
        }

        public static Double add_(Double a,Double b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = add_(a.mantissa, b.mantissa);
            return doubleExp;
        }

        public static ulong add_(ulong a,ulong b)
        {
            return add_(a, b, "addition overflow");
        }

        public static ulong add_(ulong a,ulong b,string errorMessage)
        {
            ulong c = a + b;
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

        public static Double sub_(Double a,Double b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = sub_(a.mantissa, b.mantissa);
            return doubleExp;
        }

        public static ulong sub_(ulong a,ulong b)
        {
            return sub_(a, b, "subtraction underflow");
        }

        public  static ulong sub_(ulong a,ulong b,string errorMessage)
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

        public static Exp mul_(Exp a,ulong b)
        {
            Exp exp = new Exp();
            exp.mantissa = mul_(a.mantissa, b);
            return exp;
        }

        public static ulong mul_(ulong a,Exp b)
        {
            return mul_(a, b.mantissa) / expScale;
        }

        public static Double mul_(Double a,Double b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = (mul_(a.mantissa, b.mantissa) / expScale)/expScale;
            return doubleExp;
        }

        public static Double mul_(Double a,ulong b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = mul_(a.mantissa, b);
            return doubleExp;
        }

        public static ulong mul_(ulong a,Double b)
        {
            return (mul_(a, b.mantissa) / expScale) / expScale;
        }

        public static ulong mul_(ulong a,ulong b)
        {
            return mul_(a, b, "multiplication overflow");
        }

        public static ulong mul_(ulong a,ulong b,string errorMessage)
        {
            if(a == 0 || b == 0)
            {
                return 0;
            }
            ulong c = a * b;
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

        public static Exp div_(Exp a,ulong b)
        {
            Exp exp = new Exp();
            exp.mantissa = div_(a.mantissa, b);
            return exp;
        }

        public static ulong div_(ulong a,Exp b)
        {
            return div_(mul_(a, expScale), b.mantissa);
        }

        public static Double div_(Double a,Double b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = div_(mul_(a.mantissa,doubleScale), b.mantissa);
            return doubleExp;
        }

        public static Double div_(Double a,ulong b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = div_(a.mantissa, b);
            return doubleExp;
        }

        public static ulong div_(ulong a,Double b)
        {
            return div_(mul_(a, doubleScale), b.mantissa);
        }

        public static ulong div_(ulong a,ulong b)
        {
            return div_(a, b, "divided by zero");
        }

        public static ulong div_(ulong a,ulong b,string errorMessage)
        {
            if(b <= 0)
            {
                throw new Exception(errorMessage);
            }
            return a / b;
        }

        public static Double fraction(ulong a,ulong b)
        {
            Double doubleExp = new Double();
            doubleExp.mantissa = div_(mul_(a, doubleScale), b);
            return doubleExp;
        }
    }
}
