using Neo;
using Neo.SmartContract.Framework;
using System;
using System.Numerics;

namespace InterestModelV1
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is InterestModel")]
    public partial class InterestModel : SmartContract
    {


        //The address of the owner, i.e. the Timelock contract, which can update parameters directly
        public static UInt160 owner;

        //The approximate number of blocks per year that is assumed by the interest rate model
        public static uint blocksPerYear = 2102400;


        private static readonly BigInteger Ten2Power8 = 100000000; // price or amount decimal = 10^8
        private static readonly BigInteger Ten2Power18 = 1000000000000000000; // ratio decimal = 10 ^ 18

        public static Boolean isInterestModel() => true;                                                        


        public static void PutInterestAttribute(BigInteger _multiplierPerBlock, BigInteger _baseRatePerBlock, BigInteger _jumpMultiplierPerBlock, BigInteger _kink)
        {
            defaultInterestAtrributes.Put(
                new InterestAtrributes
                {
                    multiplierPerBlock = _multiplierPerBlock,
                    baseRatePerBlock = _baseRatePerBlock,
                    jumpMultiplierPerBlock = _jumpMultiplierPerBlock,
                    kink = _kink

                }
                );

        }

        public static InterestAtrributes getInterestAttribute()
        {

            return defaultInterestAtrributes.Get();


        }

        /// <summary>
        /// Calculates the utilization rate of the market: `borrows / (cash + borrows - reserves)`
        /// </summary>
        /// <param name="cash">The amount of cash in the market</param>
        /// <param name="borrows">The amount of borrows in the market</param>
        /// <param name="reserves">The amount of reserves in the market (currently unused)</param>
        /// <returns>The utilization rate as a mantissa between [0, 1e18]</returns>
        public static BigInteger utilizationRate(BigInteger cash, BigInteger borrows, BigInteger reserves)
        {
            if (borrows == 0)
            {
                return 0;
            }
            BigInteger result = borrows * Ten2Power18 / (cash + borrows - reserves);

            return result;

        }

        /// <summary>
        /// Calculates the current borrow rate per block, with the error code expected by the market
        /// </summary>
        /// <param name="cash">The amount of cash in the market</param>
        /// <param name="borrows">The amount of borrows in the market</param>
        /// <param name="reserves">The amount of reserves in the market (currently unused)</param>
        /// <returns>The borrow rate percentage per block as a mantissa (scaled by 1e18)</returns>
        public static BigInteger getBorrowRate(BigInteger cash, BigInteger borrows, BigInteger reserves)
        {
            InterestAtrributes interestAtrributes = getInterestAttribute();

            BigInteger kink = interestAtrributes.kink;

            BigInteger multiplierPerBlock = interestAtrributes.multiplierPerBlock;

            BigInteger baseRatePerBlock = interestAtrributes.baseRatePerBlock;

            BigInteger jumpMultiplierPerBlock = interestAtrributes.jumpMultiplierPerBlock;

            BigInteger util = utilizationRate(cash, borrows, reserves);
            if (util <= kink)
            {


                BigInteger result = (util * multiplierPerBlock) / Ten2Power18 + baseRatePerBlock;
                return result;
            }
            else
            {
                BigInteger normalRate = (kink * multiplierPerBlock) / Ten2Power18 + baseRatePerBlock;

                BigInteger excessUtil = util - kink;

                BigInteger result = (excessUtil * jumpMultiplierPerBlock) / Ten2Power18 + normalRate;
                return result;


            }
        }

        /// <summary>
        /// Calculates the current supply rate per block
        /// </summary>
        /// <param name="cash">The amount of cash in the market</param>
        /// <param name="borrows">The amount of borrows in the market</param>
        /// <param name="reserves">The amount of reserves in the market</param>
        /// <param name="reserveFactorMantissa">The current reserve factor for the market</param>
        /// <returns>The supply rate percentage per block as a mantissa (scaled by 1e18)</returns>
        public static BigInteger getSupplyRate(BigInteger cash, BigInteger borrows, BigInteger reserves, BigInteger reserveFactorMantissa)
        {
            BigInteger oneMinusReserveFactor = Ten2Power18 - reserveFactorMantissa;

            BigInteger borrowRate = getBorrowRate(cash, borrows, reserves);

            BigInteger rateToPool = borrowRate * oneMinusReserveFactor / Ten2Power18;

            BigInteger utilRate = utilizationRate(cash, borrows, reserves);

            //result = 
            BigInteger result = utilRate * (rateToPool) / Ten2Power18;

            return result;
        }

        public static void putInterestModel(BigInteger multiplierPerBlock_,BigInteger baseRatePerBlock_,BigInteger jumpMultiplierPerBlock_,BigInteger kink_)
        {
            InterestAtrributes InterestModel = new InterestAtrributes()
            {
                multiplierPerBlock = multiplierPerBlock_,
                  baseRatePerBlock = baseRatePerBlock_,
            jumpMultiplierPerBlock = jumpMultiplierPerBlock_,
                              kink = kink_

            };

            defaultInterestAtrributes.Put(InterestModel);
        }














    }
}
