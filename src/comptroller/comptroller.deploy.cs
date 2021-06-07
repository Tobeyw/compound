using Neo.SmartContract.Framework.Services;
using System;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework;
using Neo;
using Neo.SmartContract;

namespace comptroller
{

    public partial class comptroller
    {


        public static object deployCToken1()
        {

            Contract temp = ContractManagement.GetContract(Runtime.ExecutingScriptHash);
            ByteString nef = temp.Nef;
            string manifest = temp.Manifest;
            Contract contract = ContractManagement.Deploy(nef, manifest);
            return contract.Hash;

        }






    }
}
