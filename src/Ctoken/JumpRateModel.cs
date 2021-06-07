using Neo;
using Neo.SmartContract.Framework;


namespace Ctoken
{
    public partial class Ctoken : SmartContract
    {
        //The address of the owner, i.e. the Timelock contract, which can update parameters directly
        public static UInt160 owner;

        //The approximate number of blocks per year that is assumed by the interest rate model
        public static uint blocksPerYear = 2102400;

        public static ulong Decimals() => 8;

        public static ulong carry = 100_000_000;


        public static void PutInterestAttribute(uint _multiplierPerBlock, uint _baseRatePerBlock, uint _jumpMultiplierPerBlock, uint _kink)
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
        /// <returns>The utilization rate as a mantissa between [0, 1e8]</returns>
        public static ulong utilizationRate(ulong cash, ulong borrows, ulong reserves)
        {
            if (borrows == 0)
            {
                return 0;
            }
            ulong result = borrows / (cash + borrows - reserves);

            return result;

        }

        /// <summary>
        /// Calculates the current borrow rate per block, with the error code expected by the market
        /// </summary>
        /// <param name="cash">The amount of cash in the market</param>
        /// <param name="borrows">The amount of borrows in the market</param>
        /// <param name="reserves">The amount of reserves in the market (currently unused)</param>
        /// <returns>The borrow rate percentage per block as a mantissa (scaled by 1e8)</returns>
        public static ulong getBorrowRate(ulong cash, ulong borrows, ulong reserves)
        {
            InterestAtrributes interestAtrributes = getInterestAttribute();

            uint kink = interestAtrributes.kink;

            uint multiplierPerBlock = interestAtrributes.multiplierPerBlock;

            uint baseRatePerBlock = interestAtrributes.baseRatePerBlock;

            uint jumpMultiplierPerBlock = interestAtrributes.jumpMultiplierPerBlock;

            ulong util = utilizationRate(cash, borrows, reserves);
            if (util <= kink)
            {


                ulong result = (util * multiplierPerBlock) / carry + baseRatePerBlock;
                return result;
            }
            else
            {
                ulong normalRate = (kink * multiplierPerBlock) / carry + baseRatePerBlock;

                ulong excessUtil = util - kink;

                ulong result = (excessUtil * jumpMultiplierPerBlock) / carry + normalRate;
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
        /// <returns>The supply rate percentage per block as a mantissa (scaled by 1e8)</returns>
        public static ulong getSupplyRate(ulong cash, ulong borrows, ulong reserves, ulong reserveFactorMantissa)
        {
            ulong oneMinusReserveFactor = carry - reserveFactorMantissa;

            ulong borrowRate = getBorrowRate(cash, borrows, reserves);

            ulong rateToPool = borrowRate * oneMinusReserveFactor / carry;

            ulong utilRate = utilizationRate(cash, borrows, reserves);

            //result = 
            ulong result = utilRate * (rateToPool) / carry;

            return result;
        }
    }
}
