
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework.Native;
using System.Numerics;


namespace comptroller
{
    public partial class comptroller
    {

        #region Token Settings

        [InitialValue("NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r", ContractParameterType.Hash160)]
        static readonly UInt160 Owner = default;




        #endregion
        public static string Version() => "0.1.0";
        public static string Symbol() => "Comptroller";

        private static bool IsOwner() => Runtime.CheckWitness(Owner);

        public static bool Verify() => IsOwner();
        public static BigInteger Decimal() => 8;


        public static void Update(ByteString nefFile, string manifest)
        {
            //if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Update(nefFile, manifest);
        }

        public static void Destroy()
        {
            //if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Destroy();
        }


    }
}
