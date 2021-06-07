
using Neo;
using Neo.SmartContract.Framework;
using System;
using System.Numerics;
using Neo.SmartContract.Framework.Services;

//using CHelper;
//using Neo.SmartContract.Framework.Native;

namespace comptroller
{
    public partial class comptroller
    {

        /*** Admin Functions ***/
        public static int setCollateralFactor(UInt160 cToken, ulong newCollateralFactorMantissa)
        {

            // Verify market is listed
            Market market = markets.Get(cToken);
            if (!market.isListed)
            {
                return ErrorReporter.fail(ErrorReporter.Error.MARKET_NOT_LISTED, ErrorReporter.FailureInfo.SET_COLLATERAL_FACTOR_NO_EXISTS);
            }
            // Check collateral factor <= 0.9
            if (newCollateralFactorMantissa > collateralFactorMaxMantissa)
            {
                return ErrorReporter.fail(ErrorReporter.Error.INVALID_COLLATERAL_FACTOR, ErrorReporter.FailureInfo.SET_COLLATERAL_FACTOR_VALIDATION);
            }
            // If collateral factor != 0, fail if price == 0
            if (newCollateralFactorMantissa != 0 && PriceOracle.getUnderlyingPrice(cToken) == 0)
            {
                return ErrorReporter.fail(ErrorReporter.Error.PRICE_ERROR, ErrorReporter.FailureInfo.SET_COLLATERAL_FACTOR_WITHOUT_PRICE);
            }

            // Set market's collateral factor to new collateral factor, remember old value
            ulong oldCollateralFactorMantissa = market.collateralFactorMantissa;
            market.collateralFactorMantissa = newCollateralFactorMantissa;
            markets.Put(cToken, market);
            NewCollateralFactor(cToken, oldCollateralFactorMantissa, newCollateralFactorMantissa);
            return (int)ErrorReporter.Error.NO_ERROR;
        }


        public static ulong getCollateralFactor(UInt160 cToken)
        {
            Market market = markets.Get(cToken);
            if (!market.isListed) throw new Exception("market is not listed");
            return market.collateralFactorMantissa;
        }




        public static int setLiquidationIncentive(ulong newLiquidationIncentiveMantissa)
        {


            // Save current value for use in log
            ulong oldLiquidationIncentiveMantissa = LiquidationIncentiveMantissa.Get();

            // Set liquidation incentive to new incentive
            LiquidationIncentiveMantissa.Put(newLiquidationIncentiveMantissa);

            NewLiquidationIncentive(oldLiquidationIncentiveMantissa, newLiquidationIncentiveMantissa);
            return (int)ErrorReporter.Error.NO_ERROR;

        }


        public static int setCloseFactor(ulong newCloseFactorMantissa)
        {


            // Save current value for use in log
            ulong oldCloseFactorMantissa = CloseFactorMantissa.Get();



            if (newCloseFactorMantissa > closeFactorMaxMantissa || newCloseFactorMantissa < closeFactorMinMantissa)
            {
                return ErrorReporter.fail(ErrorReporter.Error.INVALID_CLOSE_FACTOR, ErrorReporter.FailureInfo.SET_CLOSE_FACTOR_VALIDATION);
            }

            // Set liquidation incentive to new incentive
            CloseFactorMantissa.Put(newCloseFactorMantissa);

            NewCloseFactor(oldCloseFactorMantissa, newCloseFactorMantissa);
            return (int)ErrorReporter.Error.NO_ERROR;

        }


        public static int supportMarket(UInt160 cToken)
        {
            if (!IsOwner()) throw new Exception("only admin can make market isListed");
            Object isCTokenObj = Contract.Call(cToken, "isCToken", CallFlags.All, new object[] { });
            Boolean isCToken = (Boolean)isCTokenObj;
            if (!isCToken) throw new Exception("There is not a cToken contract!");
            if (markets.Get(cToken).isListed)
            {
                return ErrorReporter.fail(ErrorReporter.Error.MARKET_ALREADY_LISTED, ErrorReporter.FailureInfo.SUPPORT_MARKET_EXISTS);
            }
            Market market = new Market()
            {
                isListed = true,
                collateralFactorMantissa = 5000,

            };
            markets.Put(cToken, market);
            addToMarketList(cToken);
            MarketListed(cToken);
            return (int)ErrorReporter.Error.NO_ERROR;

        }

        public static void addToMarketList(UInt160 cToken)
        {
            List<UInt160> market = allMarkets.Get();
            int len = market.Count;
            for (int i = 0; i < len; i++)
            {
                if (market[i] == cToken) throw new Exception("Market already isListed!");
            }
            market.Add(cToken);
            allMarkets.Put(market);
        }

        public static void setMarketBorrowCaps(UInt160[] cTokens, BigInteger[] newBorrowCaps)
        {
            if (!IsOwner()) throw new Exception("only admin can set MarketBorrowCaps");
            int numMarkets = cTokens.Length;
            int numBorrowCaps = newBorrowCaps.Length;
            if (numMarkets == 0 || numMarkets != numBorrowCaps)
            {
                throw new Exception("invalid input");
            }
            for (int i = 0; i < numMarkets; i++)
            {
                borrowCaps.Put(cTokens[i], newBorrowCaps[i]);
                NewBorrowCap(cTokens[i], newBorrowCaps[i]);

            }


        }


        public static void putMarketIsList(UInt160 cToken, ulong collateralFactorMantissa)
        {
            

            Market market = new Market {
                isListed = true,
                collateralFactorMantissa = collateralFactorMantissa,
                accountMembership = new Map<UInt160, Boolean>()
        };

            markets.Put(cToken, market);
            allMarkets.Add(cToken);
        }

        public static Market getMarket(UInt160 cToken)
        {
            Market market = markets.Get(cToken);

            return market;
        }


        public static void setBorrowCapGuardian(UInt160 newBorrowCapGuardian)
        {

            if (!IsOwner()) throw new Exception("only admin can assign guardian");

            UInt160 oldBorrowCapGuardian = BorrowCapGuardian.Get();

            BorrowCapGuardian.Put(newBorrowCapGuardian);

            NewBorrowCapGuardian(oldBorrowCapGuardian, newBorrowCapGuardian);


        }



        public static void setPauseGuardian(UInt160 newPauseGuardian)
        {

            if (!IsOwner()) throw new Exception("only admin can assign guardian");

            UInt160 oldPauseGuardian = PauseGuardian.Get();

            PauseGuardian.Put(newPauseGuardian);

            NewPauseGuardian(oldPauseGuardian, newPauseGuardian);


        }


        public static UInt160 getPauseGuardian()
        {
            return PauseGuardian.Get();
        }



        public static void setMintPaused(UInt160 cToken,Boolean state)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            UInt160 pauseGuardian = PauseGuardian.Get();
            Market market = markets.Get(cToken);
            if (!market.isListed) throw new Exception("cannot pause a market that is not listed");
            if(sender!= pauseGuardian && !IsOwner()) throw new Exception("only pause guardian and admin can pause");
            if(state=false && !IsOwner()) throw new Exception("only admin can unpause");
            MintGuardianPaused.Put(cToken, state);
            ActionPaused(cToken, "Mint", state);

        }
        public static Boolean getMintPaused(UInt160 cToken)
        {
            Market market = markets.Get(cToken);
            if (!market.isListed) throw new Exception("market is not listed");
            return MintGuardianPaused.Get(cToken);
        }

        public static void setBorrowPaused(UInt160 cToken, Boolean state)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            UInt160 pauseGuardian = (UInt160)Storage.Get(Storage.CurrentContext, "PauseGuardian");
            Market market = markets.Get(cToken);
            if (!market.isListed) throw new Exception("cannot pause a market that is not listed");
            //Owner
            if (sender != pauseGuardian&& !IsOwner()) throw new Exception("only pause guardian and admin can pause");
            if (state = false && !IsOwner()) throw new Exception("only admin can unpause");
            BorrowGuardianPaused.Put(cToken, state);
            ActionPaused(cToken, "Borrow", state);

        }

        public static void setTransferPaused(Boolean state)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            UInt160 pauseGuardian = (UInt160)Storage.Get(Storage.CurrentContext, "PauseGuardian");
            //Owner
            if (sender != pauseGuardian && !IsOwner()) throw new Exception("only pause guardian and admin can pause");
            if (state = false && !IsOwner()) throw new Exception("only admin can unpause");
            //Storage.Put(Storage.CurrentContext, "TransferPaused", StdLib.Deserialize(state));
            ActionPaused(admin,"Transfer", state);

        }

        public static void setSeizePaused(Boolean state)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            UInt160 pauseGuardian = (UInt160)Storage.Get(Storage.CurrentContext, "PauseGuardian");
            //Owner
            if (sender != pauseGuardian && !IsOwner()) throw new Exception("only pause guardian and admin can pause");
            if (state = false && !IsOwner()) throw new Exception("only admin can unpause");
            //Storage.Put(Storage.CurrentContext, "SeizePaused", StdLib.Deserialize(state));
            ActionPaused(admin, "Seize", state);

        }















































    }
}
