
using Neo;
using Neo.SmartContract.Framework;
using System;
using System.ComponentModel;
using Neo.SmartContract.Framework.Services;
using System.Numerics;

//using CHelper;
namespace comptroller
{
    [DisplayName("comptroller")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is a comptroller contract")]
    [SupportedStandards("NEP17", "NEP10")]
    [ContractPermission("*", "*")]
    public partial class comptroller:SmartContract
    {
        #region Notifications

        [DisplayName("MarketListed")]
        public static event Action<UInt160> MarketListed;

        [DisplayName("MarketEntered")]
        public static event Action<UInt160,UInt160> MarketEntered;

        [DisplayName("MarketExited")]
        public static event Action<UInt160,UInt160> MarketExited;

        [DisplayName("NewCloseFactor")]
        public static event Action<ulong, ulong> NewCloseFactor;

        [DisplayName("NewCollateralFactor")]
        public static event Action<UInt160,ulong,ulong> NewCollateralFactor;

        [DisplayName("NewLiquidationIncentive")]
        public static event Action<ulong, ulong> NewLiquidationIncentive;

        [DisplayName("NewBorrowCap")]
        public static event Action<UInt160, BigInteger> NewBorrowCap;

        [DisplayName("NewBorrowCapGuardian")]
        public static event Action<UInt160, UInt160> NewBorrowCapGuardian;

        [DisplayName("NewPauseGuardian")]
        public static event Action<UInt160, UInt160> NewPauseGuardian;

        [DisplayName("ActionPaused")]
        public static event Action<UInt160, string, Boolean> ActionPaused;

        [DisplayName("Membership")]
        public static event Action<UInt160, UInt160, Boolean> Membership;



        private static ulong closeFactorMinMantissa = 5_000_000;

        private static ulong closeFactorMaxMantissa = 90_000_000;

        private static ulong collateralFactorMaxMantissa = 90_000_000;
        private static ulong carry = 100_000_000;
        #endregion


        private static readonly BigInteger Ten2Power8 = 100000000; // price or amount decimal = 10^8
        private static readonly BigInteger Ten2Power18 = 1000000000000000000; // value decimal = 10 ^ 18
        public static Boolean isComptroller() => true;

        /// <summary>
        /// Returns the assets an account has entered
        /// </summary>
        /// <param name="account">The address of the account to pull assets for</param>
        /// <returns>A dynamic list with the assets the account has entered</returns>
        public static UInt160[] getAssetsIn(UInt160 account)
        {
            UInt160[] result = accountAssets.Get(account);
            return result;
        }




        /// <summary>
        /// Returns whether the given account is entered in the given asset
        /// </summary>
        /// <param name="account">The address of the account to check</param>
        /// <param name="cTokenHash">The address of the cToken's hash</param>
        /// <returns>True if the account is in the asset, otherwise false.</returns>
        public static Boolean checkMembership( UInt160 cTokenHash, UInt160 account)
        {

            Map<UInt160, Boolean> result = markets.Get(cTokenHash).accountMembership;
            if (!result.HasKey(account)) return false;
            return result[account];

        }
        /// <summary>
        /// Add assets to be included in account liquidity calculation
        /// </summary>
        /// <param name="cTokens">The list of addresses of the cToken markets to be enabled</param>
        /// <returns>Success indicator for whether each corresponding market was entered</returns>
        public static int[] enterMarket(UInt160[] cTokens)
        {
            int len = cTokens.Length;
            int[] results = new int[len];
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            for (int i = 0; i < len; i++)
            {
                UInt160 cToken = cTokens[i];
              
                results[i] = addToMarketInternal(cToken, sender);
            }
            return results;
        }

        /// <summary>
        /// change account's access right of the given market
        /// </summary>
        /// <param name="cToken"></param>
        /// <param name="account"></param>
        /// <param name="membership"></param>
        public static void membershipController(UInt160 cToken, UInt160 account,Boolean membership)
        {
            Market market = markets.Get(cToken);
            Map<UInt160, Boolean> accountMap = market.accountMembership;
            accountMap[account]= membership;
            Market newMarket = new Market
            {
                isListed = market.isListed,
                collateralFactorMantissa = market.collateralFactorMantissa,
                accountMembership = accountMap
            };
            
            markets.Put(cToken,newMarket);
            
        }



        public static int checkMarket(UInt160 cToken)
        {
            Market market = markets.Get(cToken);
            if (!market.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;
            return (int)ErrorReporter.Error.NO_ERROR;
        }


        /// <summary>
        /// Add the market to the borrower's "assets in" for liquidity calculations
        /// </summary>
        /// <param name="cToken">The market to enter</param>
        /// <param name="account">The address of the account to modify</param>
        /// <returns>Success indicator for whether the market was entered</returns>
        public static int addToMarketInternal(UInt160 cToken,UInt160 account)
        {
            Market market = markets.Get(cToken);
            if(!market.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;
            if(market.accountMembership.HasKey(account)&&market.accountMembership[account]) return (int)ErrorReporter.Error.NO_ERROR;
            membershipController(cToken, account, true);
            List<UInt160> marketsOfAccount = accountAssets.Get(account);
            marketsOfAccount.Add(cToken);
            accountAssets.Put(account, marketsOfAccount);
            MarketEntered(cToken,account);
            return (int)ErrorReporter.Error.NO_ERROR;
        }
        /// <summary>
        /// Removes asset from sender's account liquidity calculation
        /// </summary>
        /// <param name="cToken">The address of the asset to be removed</param>
        /// <returns>Whether or not the account successfully exited the market</returns>
        public static int exitMarket(UInt160 cToken)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            Object AccountSnapshotObj = Contract.Call(cToken, "getAccountSnapshot", CallFlags.All, new object[] { tx.Sender });
            (int, BigInteger, BigInteger,ulong) accountSnapshot = ((int, BigInteger, BigInteger,ulong))AccountSnapshotObj;
            
            int err = accountSnapshot.Item1;
            if (err == 0) throw new Exception("exitMarket: getAccountSnapshot failed");

            BigInteger amountOwed = accountSnapshot.Item2;
            if (amountOwed != 0) return ErrorReporter.fail(ErrorReporter.Error.NONZERO_BORROW_BALANCE, ErrorReporter.FailureInfo.EXIT_MARKET_BALANCE_OWED);

            Market marketToExit = markets.Get(cToken);
            if (!marketToExit.accountMembership[tx.Sender]) return (int)ErrorReporter.Error.NO_ERROR;
            membershipController(cToken, tx.Sender, false);
            UInt160[] userAssetList = accountAssets.Get(tx.Sender);
            int len = userAssetList.Length;
            int assetIndex = len;
            for(int i = 0;i < len; i++)
            {
                if (userAssetList[i] == cToken)
                {
                    assetIndex = i;
                }


            }
            if (assetIndex >= len) throw new Exception("Index out of range");
            UInt160[] userAssetListNew = new UInt160[len - 1];
            for(int i = 0; i < len - 1; i++)
            {
                if (i < assetIndex) userAssetListNew[i] = userAssetList[i];
                else
                {
                    userAssetListNew[i] = userAssetList[i+1];
                }
            }
            accountAssets.Put(tx.Sender, userAssetListNew);
            MarketExited(tx.Sender, cToken);
            return (int)ErrorReporter.Error.NO_ERROR;
        }

        /// <summary>
        /// Checks if the account should be allowed to mint tokens in the given market
        /// </summary>
        /// <param name="cToken">The market to verify the mint against</param>
        /// <param name="minter">The account which would get the minted tokens</param>
        /// <param name="mintAmount">The amount of underlying being supplied to the market in exchange for tokens</param>
        /// <returns>0 if the mint is allowed, otherwise a semi-opaque error code (See ErrorReporter.cs)</returns>
        public static int mintAllowed(UInt160 cToken, UInt160 minter, BigInteger mintAmount)
        {
            Market market = markets.Get(cToken);
            if (!market.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;
            return (int)ErrorReporter.Error.NO_ERROR;

        }

        public static void mintVerify(UInt160 cToken, UInt160 minter, BigInteger actualMintAmount, BigInteger mintTokens)
        {
            
        }


        /// <summary>
        /// Checks if the account should be allowed to redeem tokens in the given market
        /// </summary>
        /// <param name="cToken">The market to verify the redeem against</param>
        /// <param name="redeemer">The account which would redeem the tokens</param>
        /// <param name="redeemTokens">The number of cTokens to exchange for the underlying asset in the market</param>
        /// <returns>0 if the redeem is allowed, otherwise a semi-opaque error code (See ErrorReporter.cs)</returns>
        public static int redeemAllowed(UInt160 cToken, UInt160 redeemer, BigInteger redeemTokens)
        {

            int allowed = redeemAllowedInternal(cToken, redeemer, redeemTokens);
            if (allowed != (int)ErrorReporter.Error.NO_ERROR)
            {
                return allowed;
            }

            return (int)ErrorReporter.Error.NO_ERROR;


        }



        private static int redeemAllowedInternal(UInt160 cToken, UInt160 redeemer, BigInteger redeemTokens)
        {
            Market market = markets.Get(cToken);
            if (!market.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;

            if (!market.accountMembership[redeemer]) return (int)ErrorReporter.Error.NO_ERROR;

            (int, BigInteger, BigInteger) AccountLiquidityTuple = getHypotheticalAccountLiquidityInternal(redeemer, cToken, redeemTokens, 0);

            int err = AccountLiquidityTuple.Item1;
            if (err != (int)ErrorReporter.Error.NO_ERROR) return err;
            BigInteger shortfall = AccountLiquidityTuple.Item3;
            if (shortfall > 0) return (int)ErrorReporter.Error.INSUFFICIENT_LIQUIDITY;
            return (int)ErrorReporter.Error.NO_ERROR;

        }


        /// <summary>
        /// Checks if the account should be allowed to borrow the underlying asset of the given market
        /// </summary>
        /// <param name="cToken">The market to verify the borrow against</param>
        /// <param name="borrower">The account which would borrow the asset</param>
        /// <param name="borrowAmount">The amount of underlying the account would borrow</param>
        /// <returns>0 if the borrow is allowed, otherwise a semi-opaque error code (See ErrorReporter.cs)</returns>
        public static int borrowAllowed(UInt160 cToken, UInt160 borrower, BigInteger borrowAmount)
        {

            Market market = markets.Get(cToken);
            if (!market.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;

            if (!market.accountMembership[borrower])
            {
                //only cTokens may call borrowAllowed if borrower not in market
                //require(msg.sender == cToken, "sender must be cToken");
                int error = addToMarketInternal(cToken, borrower);
                if (error != (int)ErrorReporter.Error.NO_ERROR) return error;
                membershipController(cToken, borrower, true);
            }

            if (PriceOracle.getUnderlyingPrice(cToken) == 0) return (int)ErrorReporter.Error.PRICE_ERROR;

            BigInteger borrowCap = borrowCaps.Get(cToken);

            if (borrowCap != 0)
            {
                Object totalBorrowsObj = Contract.Call(cToken, "totalBorrows", CallFlags.All, new object[] { });
                BigInteger totalBorrows = (BigInteger)totalBorrowsObj;
                BigInteger nextTolBorrows = totalBorrows + borrowAmount;
                if (nextTolBorrows > borrowCap) throw new Exception("market borrow cap reached");
            }

            (int, BigInteger, BigInteger) AccountLiquidityTuple = getHypotheticalAccountLiquidityInternal(borrower, cToken, 0, borrowAmount);
            int err = AccountLiquidityTuple.Item1;
            if (err != (int)ErrorReporter.Error.NO_ERROR) return err;
            BigInteger shortfall = AccountLiquidityTuple.Item3;
            if (shortfall > 0) return (int)ErrorReporter.Error.INSUFFICIENT_LIQUIDITY;

            return (int)ErrorReporter.Error.NO_ERROR;


        }

        public static void borrowVerify(UInt160 cToken, UInt160 borrower, BigInteger borrowAmount)
        {

        }


        /// <summary>
        /// Checks if the account should be allowed to repay a borrow in the given market
        /// </summary>
        /// <param name="cToken">The market to verify the repay against</param>
        /// <param name="payer">The account which would repay the asset</param>
        /// <param name="borrower">The account which would borrowed the asset</param>
        /// <param name="repayAmount">The amount of the underlying asset the account would repay</param>
        /// <returns>0 if the repay is allowed, otherwise a semi-opaque error code (See ErrorReporter.cs)</returns>
        public static int repayBorrowAllowed(UInt160 cToken, UInt160 payer, UInt160 borrower, BigInteger repayAmount)
        {
            Market market = markets.Get(cToken);
            if (!market.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;

            return (int)ErrorReporter.Error.NO_ERROR;
        }

        public static void repayBorrowVerify(UInt160 cToken, UInt160 payer, UInt160 borrower, BigInteger actualRepayAmount) { }

        /// <summary>
        /// Checks if the liquidation should be allowed to occur
        /// </summary>
        /// <param name="cTokenBorrowed">Asset which was borrowed by the borrower</param>
        /// <param name="cTokenCollateral">Asset which was used as collateral and will be seized</param>
        /// <param name="liquidator">The address repaying the borrow and seizing the collateral</param>
        /// <param name="borrower">The address of the borrower</param>
        /// <param name="repayAmount">The amount of underlying being repaid</param>
        /// <returns>0 if the repay is allowed, otherwise a semi-opaque error code (See ErrorReporter.cs)</returns>
        public static int liquidateBorrowAllowed(UInt160 cTokenBorrowed, UInt160 cTokenCollateral, UInt160 liquidator, UInt160 borrower, BigInteger repayAmount)
        {
            Market borrowedMarket = markets.Get(cTokenBorrowed);
            Market collateralMarket = markets.Get(cTokenCollateral);
            if(!borrowedMarket.isListed||!collateralMarket.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;
            (int, BigInteger, BigInteger) AccountLiquidityTuple = getAccountLiquidityInternal(borrower);
            int err = AccountLiquidityTuple.Item1;
            if (err != (int)ErrorReporter.Error.NO_ERROR) return err;
            BigInteger shortfall = AccountLiquidityTuple.Item3;
            if (shortfall > 0) return (int)ErrorReporter.Error.INSUFFICIENT_LIQUIDITY;
            Object borrowBalanceObj = Contract.Call(cTokenBorrowed, "borrowBalanceStored", CallFlags.All, new object[] { borrower });
            BigInteger borrowBalance = (BigInteger)borrowBalanceObj;

            BigInteger maxClose = CloseFactorMantissa.Get() * borrowBalance;
            if (repayAmount > maxClose)
            {
                return (int)ErrorReporter.Error.TOO_MUCH_REPAY;
            }

            return (int)ErrorReporter.Error.NO_ERROR;
        }

        public static void liquidateBorrowVerify(UInt160 cTokenBorrowed, UInt160 cTokenCollateral, UInt160 liquidator, UInt160 borrower, BigInteger actualRepayAmount, BigInteger seizeTokens){ }

        /// <summary>
        /// Checks if the seizing of assets should be allowed to occur
        /// </summary>
        /// <param name="cTokenBorrowed">Asset which was borrowed by the borrower</param>
        /// <param name="cTokenCollateral">Asset which was used as collateral and will be seized</param>
        /// <param name="liquidator">The address repaying the borrow and seizing the collateral</param>
        /// <param name="borrower">The address of the borrower</param>
        /// <param name="seizeTokens">The number of collateral tokens to seize</param>
        /// <returns></returns>
        public static int seizeAllowed(UInt160 cTokenBorrowed, UInt160 cTokenCollateral, UInt160 liquidator, UInt160 borrower, BigInteger seizeTokens)
        {
            Market borrowedMarket = markets.Get(cTokenBorrowed);
            Market collateralMarket = markets.Get(cTokenCollateral);
            if (!borrowedMarket.isListed || !collateralMarket.isListed) return (int)ErrorReporter.Error.MARKET_NOT_LISTED;
            return (int)ErrorReporter.Error.NO_ERROR;
        }

        public static void seizeVerify(UInt160 cTokenCollateral, UInt160 cTokenBorrowed, UInt160 liquidator, UInt160 borrower, BigInteger seizeTokens) { }

        /// <summary>
        /// Checks if the account should be allowed to transfer tokens in the given market
        /// </summary>
        /// <param name="cToken">The market to verify the transfer against</param>
        /// <param name="src">The account which sources the tokens</param>
        /// <param name="dst">The account which receives the tokens</param>
        /// <param name="transferTokens">The number of cTokens to transfer</param>
        /// <returns>0 if the transfer is allowed, otherwise a semi-opaque error code (See ErrorReporter.cs)</returns>
        public static int transferAllowed(UInt160 cToken, UInt160 src, UInt160 dst, BigInteger transferTokens)
        {
            int allowed = redeemAllowedInternal(cToken, src, transferTokens);
            if (allowed != (int)ErrorReporter.Error.NO_ERROR) return allowed;
            return (int)ErrorReporter.Error.NO_ERROR;
        }

        public static void transferVerify(UInt160 cToken, UInt160 src, UInt160 dst, BigInteger transferTokens) { }

        public struct AccountLiquidityLocalVars
        {
            public BigInteger sumCollateral;
            public BigInteger sumBorrowPlusEffects;
            public BigInteger cTokenBalance;
            public BigInteger borrowBalance;
            public ulong exchangeRateMantissa;
            public ulong oraclePriceMantissa;
            //Exp
            public ulong collateralFactor;
            public ulong exchangeRate;
            public BigInteger oraclePrice;
            public BigInteger tokensToDenom;
        }

        public static (int, BigInteger, BigInteger) getAccountLiquidity(UInt160 account)
        {
            (int, BigInteger, BigInteger) result = getHypotheticalAccountLiquidityInternal(account, null, 0, 0);
            return result;

        }

        public static (int, BigInteger, BigInteger) getAccountLiquidityInternal(UInt160 account)
        {
            (int, BigInteger, BigInteger) result = getHypotheticalAccountLiquidityInternal(account, null, 0, 0);
            return result;

        }

        public static (int, BigInteger, BigInteger) getHypotheticalAccountLiquidity(UInt160 account, UInt160 cTokenModify, BigInteger redeemTokens, BigInteger borrowAmount)
        {
            (int, BigInteger, BigInteger) result = getHypotheticalAccountLiquidityInternal(account, cTokenModify, redeemTokens, borrowAmount);
            return result;
        }

        public static (int, BigInteger, BigInteger) getHypotheticalAccountLiquidityInternal(UInt160 account, UInt160 cTokenModify, BigInteger redeemTokens, BigInteger borrowAmount)
        {
            AccountLiquidityLocalVars vars;

            UInt160[] assets = accountAssets.Get(account);
            int len = assets.Length;
            vars.sumCollateral = 0;
            vars.sumBorrowPlusEffects = 0;
            for (int i = 0; i < len; i++)
            {
                UInt160 asset = assets[i];
                Object AccountSnapshotObj = Contract.Call(cTokenModify, "getAccountSnapshot", CallFlags.All, new object[] { account });
                (int, BigInteger, BigInteger, ulong) accountSnapshot = ((int, BigInteger, BigInteger, ulong))AccountSnapshotObj;
                int err = accountSnapshot.Item1;
                if (err != (int)ErrorReporter.Error.NO_ERROR)
                {
 
                    return ((int)ErrorReporter.Error.SNAPSHOT_ERROR, 0, 0);
                }
                vars.cTokenBalance = accountSnapshot.Item2;
                vars.borrowBalance = accountSnapshot.Item3;
                vars.exchangeRateMantissa = accountSnapshot.Item4;
                vars.collateralFactor = markets.Get(asset).collateralFactorMantissa;
                vars.exchangeRate = vars.exchangeRateMantissa;
                vars.oraclePriceMantissa = PriceOracle.getUnderlyingPrice(asset);
 
                if (vars.oraclePriceMantissa == 0)
                {
 
                    return ((int)ErrorReporter.Error.PRICE_ERROR, 0, 0);
                }
                vars.oraclePrice = vars.oraclePriceMantissa;
                vars.tokensToDenom = vars.collateralFactor * vars.exchangeRate * vars.oraclePrice / (carry*carry);
                vars.sumCollateral += vars.tokensToDenom * vars.cTokenBalance;
                vars.sumBorrowPlusEffects += vars.oraclePrice* vars.borrowBalance;
                if(asset == cTokenModify)
                {
                    vars.sumBorrowPlusEffects += vars.tokensToDenom * redeemTokens;
                    vars.sumBorrowPlusEffects += vars.oraclePrice * borrowAmount;
                }
            }
            if (vars.sumCollateral > vars.sumBorrowPlusEffects)
            {
                return ((int)ErrorReporter.Error.NO_ERROR, vars.sumCollateral - vars.sumBorrowPlusEffects, 0);
            }
            else
            {
                return ((int)ErrorReporter.Error.NO_ERROR, 0, vars.sumBorrowPlusEffects - vars.sumCollateral);
            }


        }


        /// <summary>
        /// Calculate number of tokens of collateral asset to seize given an underlying amount
        /// </summary>
        /// <param name="cTokenBorrowed">The address of the borrowed cToken</param>
        /// <param name="cTokenCollateral">The address of the collateral cToken</param>
        /// <param name="actualRepayAmount">The amount of cTokenBorrowed underlying to convert into cTokenCollateral tokens</param>
        /// <returns>(errorCode, number of cTokenCollateral tokens to be seized in a liquidation)</returns>
        public static (int, BigInteger) liquidateCalculateSeizeTokens(UInt160 cTokenBorrowed, UInt160 cTokenCollateral, BigInteger actualRepayAmount)
        {
            ulong priceBorrowedMantissa = PriceOracle.getUnderlyingPrice(cTokenBorrowed);
            ulong priceCollateralMantissa = PriceOracle.getUnderlyingPrice(cTokenCollateral);
            if (priceBorrowedMantissa == 0 || priceCollateralMantissa == 0)
            {
                return ((int)ErrorReporter.Error.PRICE_ERROR, 0);
            }
            Object exchangeRateMantissaObj = Contract.Call(cTokenCollateral, "exchangeRateStored", CallFlags.All, new object[] {});
            ulong exchangeRateMantissa = (ulong)exchangeRateMantissaObj;
            ulong numerator = LiquidationIncentiveMantissa.Get() * priceBorrowedMantissa;
            ulong denominator = priceCollateralMantissa * exchangeRateMantissa;
            ulong ratio = (numerator * carry) / denominator;
            BigInteger seizeTokens = (ratio * actualRepayAmount)/carry;
            return ((int)ErrorReporter.Error.NO_ERROR, seizeTokens);
        }


    }
}
