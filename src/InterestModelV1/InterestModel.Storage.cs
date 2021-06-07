using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace InterestModelV1
{
    public partial class InterestModel
    {

        public struct InterestAtrributes
        {
            //The multiplier of utilization rate that gives the slope of the interest rate
            public BigInteger multiplierPerBlock;

            //The base interest rate which is the y-intercept when utilization rate is 0
            public BigInteger baseRatePerBlock;

            //The multiplierPerBlock after hitting a specified utilization point
            public BigInteger jumpMultiplierPerBlock;

            //The utilization point at which the jump multiplier is applied
            public BigInteger kink;
        }

        public class defaultInterestAtrributes
        {

            static string key = "defaultInterestAtrributes";
            public static void Put(InterestAtrributes defaultInterest)
            {
                string defaultInterestObj = StdLib.Serialize(defaultInterest);
                Storage.Put(Storage.CurrentContext, key, defaultInterestObj);
            }

            public static InterestAtrributes Get()
            {
                if (Storage.Get(Storage.CurrentContext, key) == null) throw new Exception("Please Initial the InterestAtrributes");
                string defaultInterest = Storage.Get(Storage.CurrentContext, key);
                Object defaultInterestObj = StdLib.Deserialize(defaultInterest);
                InterestAtrributes result = (InterestAtrributes)defaultInterestObj;
                return result;
            }
        }



    }
}
