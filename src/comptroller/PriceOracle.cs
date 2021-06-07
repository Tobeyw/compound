
using Neo;
using Neo.SmartContract.Framework.Services;
using System.Numerics;

namespace comptroller
{
    public static class PriceOracle
    {

        public static class prices
        {
            public static StorageMap pricesMap = new StorageMap(Storage.CurrentContext, "prices");

            public static void Put(UInt160 account, BigInteger cap) => pricesMap.Put(account, cap);

            public static BigInteger Get(UInt160 account) => (BigInteger)pricesMap.Get(account);

            
        }

        public static BigInteger getUnderlyingPrice(UInt160 cToken)
        {
            return 100;
        }











    }
}
