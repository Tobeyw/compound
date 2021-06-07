
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace comptroller
{
    public partial class comptroller
    {
        public static UInt160 admin;

        public static void setMaxAssets(BigInteger maxAssets)
        {
            string maxAssetsJson = StdLib.Serialize(maxAssets);
            Storage.Put(Storage.CurrentContext, "maxAssets", maxAssetsJson);
        }

        public static BigInteger getMaxAssets()
        {
            string maxAssetsJson = Storage.Get(Storage.CurrentContext, "maxAssets");
            if (maxAssetsJson == null) return 0;
            Object maxAssets = StdLib.Deserialize(maxAssetsJson);
            return (BigInteger)maxAssets;
        }

        public static class allMarkets
        {
            static string key = "allMarkets";

            public static void Put(List<UInt160> allMarkets)
            {

                string allMarketsJson = StdLib.Serialize(allMarkets);
                Storage.Put(Storage.CurrentContext, key, allMarketsJson);
            }

            public static void Add(UInt160 market)
            {
                List<UInt160> marketsList = Get();
                marketsList.Add(market);
                Put(marketsList);
            }


            public static List<UInt160> Get()
            {

                
                if (Storage.Get(Storage.CurrentContext, "allMarkets") == null)
                {
                    return new List<UInt160> { };
                }
                string allMarketsJson = Storage.Get(Storage.CurrentContext, "allMarkets");
                Object allMarkets = StdLib.Deserialize(allMarketsJson);
                return (List<UInt160>)allMarkets;

            }




        }
        public static class BorrowCapGuardian
        {
            static string key = "borrowCapGuardian";

            public static StorageMap BorrowCapGuardianMap = new StorageMap(Storage.CurrentContext, key);
            public static void Put(UInt160 account)
            {

                BorrowCapGuardianMap.Put(key, account);
            }

            public static UInt160 Get()
            {

                return (UInt160)BorrowCapGuardianMap.Get(key);
            }


        }
        public static class PauseGuardian
        {
            static string key = "pauseGuardian";

            public static StorageMap PauseGuardianMap = new StorageMap(Storage.CurrentContext, key);
            public static void Put(UInt160 account)
            {

                PauseGuardianMap.Put(key, account);
            }

            public static UInt160 Get()
            {
              
                return (UInt160)PauseGuardianMap.Get(key);
            }
        }



        public static class LiquidationIncentiveMantissa
        {
            static string key = "liquidationIncentiveMantissa";
            

            public static void Put(ulong liquidationIncentiveMantissa)
            {

                string liquidationIncentiveMantissaJson = StdLib.Serialize(liquidationIncentiveMantissa);
                Storage.Put(Storage.CurrentContext, key, liquidationIncentiveMantissaJson);
            }

            public static ulong Get()
            {
                if (Storage.Get(Storage.CurrentContext, key) == null) throw new Exception("Please set the liquidationIncentive before liquidate");
                string liquidationIncentiveMantissaJson = Storage.Get(Storage.CurrentContext, key);
                Object liquidationIncentiveMantissa = StdLib.Deserialize(liquidationIncentiveMantissaJson);
                return (ulong)liquidationIncentiveMantissa;
            }
        }

        public static class CloseFactorMantissa
        {
            static string key = "closeFactorMantissa";
            //public static StorageMap CloseFactorMantissaMap = new StorageMap(Storage.CurrentContext, key);
            public static void Put(ulong closeFactorMantissa)
            {
                string closeFactorMantissaJson = StdLib.Serialize(closeFactorMantissa);
                Storage.Put(Storage.CurrentContext, key, closeFactorMantissaJson);
            }

            public static ulong Get()
            {

                string closeFactorMantissaJson = Storage.Get(Storage.CurrentContext, key);
                Object closeFactorMantissa = StdLib.Deserialize(closeFactorMantissaJson);
                return (ulong)closeFactorMantissa;
            }
        }

        public struct Market
        {
            public Boolean isListed;

            public ulong collateralFactorMantissa;

            public Map<UInt160 ,Boolean> accountMembership;
            
            //Boolean isComped;

        }

        public static class accountAssets
        {
            public static StorageMap accountAssetsMap = new StorageMap(Storage.CurrentContext, "accountAssets");

            public static void Put(UInt160 account, List<UInt160> cToken)
            {

                string cTokenJson = StdLib.Serialize(cToken);
                accountAssetsMap.Put(account, cTokenJson);
  
            }

            public static List<UInt160> Get(UInt160 account)
            {

                string cTokensJson = accountAssetsMap.Get(account);
                if (cTokensJson == null)
                {
                    return new List<UInt160> { };
                }
                Object cTokens = StdLib.Deserialize(cTokensJson);
                List<UInt160> result = (List<UInt160>)cTokens;
                return result;
            }
        }

        public static class markets
        {
            public static StorageMap marketsMap = new StorageMap(Storage.CurrentContext, "markets");
            public static void Put(UInt160 account, Market market)
            {
                string marketJson = StdLib.Serialize(market);
                marketsMap.Put(account, marketJson);
                

            }

            public static Market Get(UInt160 account)
            {

                
                if (marketsMap.Get(account) == null)
                {
                    return new Market {
                        isListed = false,
                        collateralFactorMantissa = 5000,
                        accountMembership = new Map<UInt160,Boolean>()



                    };
                }
                string marketJson = marketsMap.Get(account);
                Object market= StdLib.Deserialize(marketJson);
                Market result = (Market)market;
                return result;

            }

        }

        public static class borrowCaps
        {
            public static StorageMap borrowCapsMap = new StorageMap(Storage.CurrentContext, "borrowCaps");

            public static void Put(UInt160 account,BigInteger cap) => borrowCapsMap.Put(account, cap);

            public static BigInteger Get(UInt160 account) => (BigInteger)borrowCapsMap.Get(account);


        }
        public static class BorrowGuardianPaused
        {
            static string key = "borrowGuardianPaused";

            public static StorageMap borrowGuardianPausedMap = new StorageMap(Storage.CurrentContext, key);

            public static void Put(UInt160 cToken, Boolean state)
            {
                string stateJson = StdLib.Serialize(state);
                borrowGuardianPausedMap.Put(cToken, stateJson);
            }

            public static Boolean Get(UInt160 cToken)
            {
                string stateJson = borrowGuardianPausedMap.Get(cToken);
                if (stateJson == null)
                {
                    return true;
                }
                Object state = StdLib.Deserialize(stateJson);
                Boolean result = (Boolean)state;
                return result;
            }


        }

        public static class MintGuardianPaused
        {
            static string key = "mintGuardianPaused";

            public static StorageMap MintGuardianPausedMap = new StorageMap(Storage.CurrentContext, key);

            public static void Put(UInt160 cToken, Boolean state)
            {
                string stateJson = StdLib.Serialize(state);
                MintGuardianPausedMap.Put(cToken, stateJson);
            }

            public static Boolean Get(UInt160 cToken)
            {
                string stateJson = MintGuardianPausedMap.Get(cToken);
                if (stateJson == null)
                {
                    return true;
                }
                Object state = StdLib.Deserialize(stateJson);
                Boolean result = (Boolean)state;
                return result;
            }


        }

        public static class TransferGuardianPaused
        {
            static string key = "transferGuardianPaused";

            public static StorageMap TransferGuardianPausedMap = new StorageMap(Storage.CurrentContext, key);

            public static void Put(Boolean state)
            {
                string stateJson = StdLib.Serialize(state);
                TransferGuardianPausedMap.Put(key, stateJson);
            }

            public static Boolean Get()
            {
                string stateJson = TransferGuardianPausedMap.Get(key);
                if (stateJson == null)
                {
                    return true;
                }
                Object state = StdLib.Deserialize(stateJson);
                Boolean result = (Boolean)state;
                return result;
            }


        }


        public static class SeizeGuardianPaused
        {
            static string key = "seizeGuardianPaused";

            public static StorageMap TransferGuardianPausedMap = new StorageMap(Storage.CurrentContext, key);

            public static void Put(bool state)
            {
                string stateJson = StdLib.Serialize(state);
                TransferGuardianPausedMap.Put(key, stateJson);
            }

            public static bool Get()
            {
                string stateJson = TransferGuardianPausedMap.Get(key);
                if (stateJson == null)
                {
                    return true;
                }
                Object state = StdLib.Deserialize(stateJson);
                bool result = (Boolean)state;
                return result;
            }


        }

    }
}
