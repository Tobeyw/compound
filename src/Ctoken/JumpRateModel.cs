using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;

namespace Ctoken
{
    public partial class Ctoken : SmartContract
    {
        //The address of the owner, i.e. the Timelock contract, which can update parameters directly
        public static UInt160 owner;

        //The approximate number of blocks per year that is assumed by the interest rate model
        public static uint blocksPerYear = 2102400;



        public static void PutInterestAttribute(uint _multiplierPerBlock, uint _baseRatePerBlock, uint _jumpMultiplierPerBlock, uint _kink)
        {
            UInt160 InterestModelAddress = InterestModel.Get();
            Contract.Call(InterestModelAddress, "PutInterestAttribute", CallFlags.All, new object[] { _multiplierPerBlock, _baseRatePerBlock, _jumpMultiplierPerBlock , _kink });

        }

        public static InterestAtrributes getInterestAttribute()
        {
            UInt160 InterestModelAddress = InterestModel.Get();
            object modelObj = Contract.Call(InterestModelAddress, "getInterestAttribute", CallFlags.All, new object[] { });
            return (InterestAtrributes)modelObj;

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
            UInt160 InterestModelAddress = InterestModel.Get();
            object utilizationRateObj = Contract.Call(InterestModelAddress, "getInterestAttribute", CallFlags.All, new object[] { cash,borrows, reserves });
            return (ulong)utilizationRateObj;

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
            UInt160 InterestModelAddress = InterestModel.Get();
            object BorrowRateObj = Contract.Call(InterestModelAddress, "getBorrowRate", CallFlags.All, new object[] { cash, borrows, reserves });
            return (ulong)BorrowRateObj;
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
            UInt160 InterestModelAddress = InterestModel.Get();
            object SupplyRateObj = Contract.Call(InterestModelAddress, "getSupplyRate", CallFlags.All, new object[] { cash, borrows, reserves, reserveFactorMantissa });
            return (ulong)SupplyRateObj;
        }
    }
}
