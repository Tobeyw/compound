using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Compiler.CSharp.UnitTests.Utils;
using System.IO;
using Neo;
using Neo.VM;
using Neo.VM.Types;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using Neo.IO;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using System.Collections.Generic;

namespace Neo.SmartContract.Framework.UnitTests.Services
{
    [TestClass]
    public class testCtoken
    {

        private TestEngine _engine;

        class DummyVerificable : IVerifiable
        {
            public Witness[] Witnesses { get; set; }

            public int Size => 0;

            public void Deserialize(BinaryReader reader) { }

            public void DeserializeUnsigned(BinaryReader reader) { }

            public UInt160[] GetScriptHashesForVerifying(DataCache snapshot)
            {
                return new UInt160[]
                {
                    UInt160.Parse("0xb312313659b5da91e7662b603bdaad6329cb557a")
                };
            }

            public void Serialize(BinaryWriter writer) { }

            public void SerializeUnsigned(BinaryWriter writer) { }
        }
        [TestInitialize]
        public void Init()
        {

            StackItem scripthash = "b312313659b5da91e7662b603bdaad6329cb557a".HexToBytes();
            UInt160 defaultSender = UInt160.Parse("0xb312313659b5da91e7662b603bdaad6329cb557a");

            _engine = new TestEngine(TriggerType.Application, new DummyVerificable(), snapshot: new TestDataCache(), persistingBlock: new Block()
            {
                Header = new Header()
                {
                    Index = 123,
                    Timestamp = 1234,
                    Witness = new Witness()
                    {
                        InvocationScript = System.Array.Empty<byte>(),
                        VerificationScript = System.Array.Empty<byte>()
                    },
                    NextConsensus = UInt160.Zero,
                    MerkleRoot = UInt256.Zero,
                    PrevHash = UInt256.Zero
                },

                Transactions = new Transaction[]
                    {
                     new Transaction()
                     {
                          Attributes = System.Array.Empty<TransactionAttribute>(),
                          Signers = new Signer[]{ new Signer() { Account = defaultSender } },
                          Witnesses = System.Array.Empty<Witness>(),
                          Script = System.Array.Empty<byte>()
                     }
                    }

            });
            string path = "../../../../CToken/";

            string[] files = Directory.GetFiles(path, "*.cs");


            _engine.AddEntryScript(files);

            _engine.Snapshot.ContractAdd(new ContractState()
            {
                Hash = _engine.EntryScriptHash,
                Nef = _engine.Nef,
                Manifest = new Manifest.ContractManifest()

            });
        }

        private static Transaction BuildTransaction(UInt160 sender, byte[] script)
        {
            Transaction tx = new()
            {
                Script = script,
                Nonce = (uint)new Random().Next(1000, 9999),
                Signers = new Signer[]
                {
                    new() { Account = sender, Scopes = WitnessScope.Global }
                },
                Attributes = System.Array.Empty<TransactionAttribute>()
            };
            return tx;
        }

        [TestMethod]
        public void Test1()
        {
            var contract = _engine.EntryScriptHash;
            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(contract, " ");
            var sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion); //owner
            var tx = BuildTransaction(sender, sb.ToArray());

            //var system = TestBlockchain.TheNeoSystem;
            //var snapshot = system.GetSnapshot().CreateSnapshot();
            var engine = new TestEngine(TriggerType.Application, tx, snapshot: new TestDataCache(), persistingBlock: new Block()
            {
                Header = new Header()
                {
                    Index = 123,
                    Timestamp = 1234,
                    Witness = new Witness()
                    {
                        InvocationScript = System.Array.Empty<byte>(),
                        VerificationScript = System.Array.Empty<byte>()
                    },
                    NextConsensus = UInt160.Zero,
                    MerkleRoot = UInt256.Zero,
                    PrevHash = UInt256.Zero
                },

                Transactions = new Transaction[]
               {
                     new Transaction()
                     {
                          Attributes = System.Array.Empty<TransactionAttribute>(),
                          Signers = new Signer[]{ new Signer() { Account = sender, Scopes=WitnessScope.Global } },
                          Witnesses = System.Array.Empty<Witness>(),
                          Script = System.Array.Empty<byte>()
                     }
               }

            });
            string path = "../../../../CToken/";

            string[] files = Directory.GetFiles(path, "*.cs");


            engine.AddEntryScript(files);
            engine.Snapshot.ContractAdd(new ContractState()
            {
                Hash = _engine.EntryScriptHash,
                Nef = _engine.Nef,
                Manifest = ContractManifest.FromJson(_engine.Manifest),
            });

            //putMarketIsList
            engine.Reset();
            UInt160 asset1 = "NajuJa8fyvFrT9dAnu3QmoFSNZFWJbAa8p".ToScriptHash(ProtocolSettings.Default.AddressVersion);
            UInt160 asset2 = "NNdT88fL6YRjZYc6B3sAx5vwA5T4Bidi6p".ToScriptHash(ProtocolSettings.Default.AddressVersion);
            UInt160 tourist = "NQXmjb22aHYo2Hph8vzw7d7eiwbnwNvqEL".ToScriptHash(ProtocolSettings.Default.AddressVersion);
            engine.Reset();
            var stack = engine.ExecuteTestCaseStandard("initialize", 5000,"name","symbol",500);
            //Assert.AreEqual(VMState.HALT, engine.State);
            Assert.AreEqual(stack.Pop(), 0);



        }

    }
}
