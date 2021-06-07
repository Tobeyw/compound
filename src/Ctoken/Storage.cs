using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace Ctoken
{
    public partial class Ctoken : SmartContract
    {
        public static class accountTokens
        {
            public static StorageMap accountTokensMap = new StorageMap(Storage.CurrentContext, "accountTokens");
            public static void Put(UInt160 account,BigInteger tokens)
            {

                
                accountTokensMap.Put(account, tokens);
            }
            public static BigInteger Get(UInt160 account) => (BigInteger)accountTokensMap.Get(account);

            public static void Increase(UInt160 account, BigInteger tokens)
            {
                accountTokensMap.Put(account, Get(account)+ tokens);
            }

            public static void Reduce(UInt160 account, BigInteger tokens)
            {
                if(Get(account) < tokens) throw new Exception("not sufficient cTokens");
                accountTokensMap.Put(account, Get(account) - tokens);
            }

        }

        public static class transferAllowance
        {
            public static StorageMap transferAllowanceMap = new StorageMap(Storage.CurrentContext, "transferAllowance");

            public static void Put(UInt160 account,Map<UInt160, BigInteger> signalAllowance)
            {
                string allowanceJson = StdLib.Serialize(signalAllowance);
                transferAllowanceMap.Put(account, allowanceJson);
            }

            public static Map<UInt160, BigInteger> Get(UInt160 account)
            {
                string allowanceJson = transferAllowanceMap.Get(account);
                Object result = StdLib.Deserialize(allowanceJson);
                Map<UInt160, BigInteger> resultReturn = (Map<UInt160, BigInteger>)result;
                return resultReturn;
            }
        }


        public struct cTokenAtrributes
        {
            public string name;
            public string symbol;
            public ulong decimals;
            public uint initialExchangeRateMantissa_;

        }

        public struct InterestAtrributes
        {
            //The multiplier of utilization rate that gives the slope of the interest rate
            public uint multiplierPerBlock;

            //The base interest rate which is the y-intercept when utilization rate is 0
            public uint baseRatePerBlock;

            //The multiplierPerBlock after hitting a specified utilization point
            public uint jumpMultiplierPerBlock;

            //The utilization point at which the jump multiplier is applied
            public uint kink;
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


        public struct BorrowSnapshot
        {
            public uint principal;
            public uint interestIndex;
        }

        public struct AccountSnapshot
        {
            public uint reservesFactorMantissa;
            public uint initialExchangeRateMantissa;
            public uint accrualBlockNumber;
            public ulong borrowIndex;
            public uint totalBorrows;
            public uint totalReserves;
            public uint totalSupply;
            public bool _notEntered;
            public string name;
            public string symbol;
            public ulong decimals;
            public ulong borrowRateMaxMantissa;
            public ulong reservesFactorMaxMantissa;
        }

        public struct AdminSnapshot
        {
            public UInt160 admin;
            public UInt160 pendingAdmin;
        }

        public class defaultAdmin
        {
            static string key = "defaultAdmin";

            public static void Put(AdminSnapshot adminSnapshot)
            {
                string adSnapshot = StdLib.Serialize(adminSnapshot);
                Storage.Put(Storage.CurrentContext, key, adSnapshot);
            }

            public static AdminSnapshot Get()
            {
                if (Storage.Get(Storage.CurrentContext, key)  ==  null)
                {
                    return new AdminSnapshot {
                        admin = Owner,
                        pendingAdmin = Owner

                    };
                }
                string adSnapshot = Storage.Get(Storage.CurrentContext, key);
                Object adSnapshotObj = StdLib.Deserialize(adSnapshot);
                AdminSnapshot result = (AdminSnapshot)adSnapshotObj;
                return result;
            }
        }

        public class defaultMessage
        {
            static string key = "defaultMessage";

            public static void Put(AccountSnapshot accountSnapshot)
            {
                string accSnapshot = StdLib.Serialize(accountSnapshot);
                Storage.Put(Storage.CurrentContext,key, accSnapshot);
            }

            public static AccountSnapshot Get()
            {
                if (Storage.Get(Storage.CurrentContext, key) == null)
                {
                    return new AccountSnapshot {
                        accrualBlockNumber=0,
                           borrowIndex=0
        };
                }
                string accSnapshot = Storage.Get(Storage.CurrentContext,key);
                Object accountSnapshotObj = StdLib.Deserialize(accSnapshot);
                AccountSnapshot result = (AccountSnapshot)accountSnapshotObj;
                return result;
            }
        }

        public static class accountBorrows
        {
            public static StorageMap accountBorowsMap = new StorageMap(Storage.CurrentContext, "accountSnapshot");
            public static void Put(UInt160 account,BorrowSnapshot borrowsnapshot)
            {
                string snapshot = StdLib.Serialize(borrowsnapshot);
                accountBorowsMap.Put(account, snapshot);
            }
            public static BorrowSnapshot Get(UInt160 account)
            {
                string snapshot = accountBorowsMap.Get(account);
                Object borrowsnapshot = StdLib.Deserialize(snapshot);
                BorrowSnapshot result = (BorrowSnapshot)borrowsnapshot;
                return result;
            }
        }


        public static class InterestModel
        {

            static string key = "InterestModel";

            public static StorageMap InterestModelMap = new StorageMap(Storage.CurrentContext, key);
            public static void Put(UInt160 address)
            {
                object isInterestModelObj = Contract.Call(address, "isInterestModel", CallFlags.All, new object[] { });
                if (!(bool)isInterestModelObj) throw new Exception("This is not a InterestModel");
                InterestModelMap.Put(key, address);
            }

            public static UInt160 Get()
            {
                UInt160 address = (UInt160)InterestModelMap.Get(key);
                object isInterestModelObj = Contract.Call(address, "isInterestModel", CallFlags.All, new object[] { });
                if (!(bool)isInterestModelObj) throw new Exception("This is not a InterestModel");
                return address;
            }


        }





    }
}
