using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Compiler.CSharp.UnitTests.Utils;
using Neo.VM;
using Neo.VM.Types;
using Neo.Network.P2P.Payloads;
using System.IO;
using Neo.Persistence;
using System;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using Neo.IO;

namespace Neo.SmartContract.Framework.UnitTests.Services
{
    [TestClass]
    public class GrantTest
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
            _engine.AddEntryScript("./TestClasses/Contract_Grant.cs");

            _engine.Snapshot.ContractAdd(new ContractState()
        {
            Hash = _engine.EntryScriptHash,
                Nef = _engine.Nef,
                Manifest = new Manifest.ContractManifest()
            });
        }

    // only one person vote for a single project
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
        engine.AddEntryScript("./TestClasses/Contract_Grant.cs");
        engine.Snapshot.ContractAdd(new ContractState()
        {
            Hash = contract,
            Nef = _engine.Nef,
            Manifest = ContractManifest.FromJson(_engine.Manifest),
        });

        // set account   
        UInt160 account = Contract.GetBFTAddress(ProtocolSettings.Default.StandbyValidators);
        tx.Signers = new Signer[] { new Signer { Account = account, Scopes = WitnessScope.Global } };
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, account), 100_000_000);
        UInt160 to1 = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to1.ToArray(), 1000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to1), 1000);

        engine.Reset();
        UInt160 to2 = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to2.ToArray(), 1000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to2), 1000);

        engine.Reset();
        UInt160 to3 = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to3.ToArray(), 1000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to3), 1000);

        engine.Reset();
        engine.ExecuteTestCaseStandard("_deploy", " ", false);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.ExecuteTestCaseStandard("roundStart");
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        var result = engine.ExecuteTestCaseStandard("getRound");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, result.Pop());


        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 1);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 2);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjects", 1);  //By round
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        sender = "NSbBAtqDrwJbBNhw6SFbVGzKTpVu5vkUTa".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 3);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjects", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("donate", 100);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(1, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("allProjects", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        engine.Reset();
        var from = "0xe64f083fa1ce56fb27611d19be9c920836eba532".HexToBytes();
        result = engine.ExecuteTestCaseStandard("votingCost", from, 1, 3);
        Assert.AreEqual(VMState.HALT, engine.State);
        var arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(600, arr[0]);

        engine.Reset();
        sender = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 1, 3);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        from = "f8ed484eaf8c167e09760db35a4a1244aea28a08".HexToBytes();
        result = engine.ExecuteTestCaseStandard("votingCost", from, 1, 3);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(1500, arr[0]);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getSupportArea", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getSupportPool", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(99, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getPreTaxSupportPool", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(100, result.Pop());

        engine.Reset();
        StackItem acc = "7a55cb2963adda3b602b66e791dab559363112b3".HexToBytes();  //小端序
        result = engine.ExecuteTestCaseStandard("balance", acc);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("withdraw");
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(0, result.Pop());

        engine.Reset();
        acc = "7a55cb2963adda3b602b66e791dab559363112b3".HexToBytes();  //小端序
        result = engine.ExecuteTestCaseStandard("balance", acc);
        Assert.AreEqual(VMState.HALT, engine.State);


    }

    //multiple people vote on multiple projects
    [TestMethod]
    public void Test2()
    {
        var contract = _engine.EntryScriptHash;
        using ScriptBuilder sb = new();
        sb.EmitDynamicCall(contract, " ");
        var sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion); //owner
        var tx = BuildTransaction(sender, sb.ToArray());

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
        engine.AddEntryScript("./TestClasses/Contract_Grant.cs");
        engine.Snapshot.ContractAdd(new ContractState()
        {
            Hash = contract,
            Nef = _engine.Nef,
            Manifest = ContractManifest.FromJson(_engine.Manifest),
        });

        // set account   
        UInt160 account = Contract.GetBFTAddress(ProtocolSettings.Default.StandbyValidators);
        tx.Signers = new Signer[] { new Signer { Account = account, Scopes = WitnessScope.Global } };
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, account), 100_000_000);
        UInt160 to1 = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to1.ToArray(), 1000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to1), 1000);

        engine.Reset();
        UInt160 to2 = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to2.ToArray(), 1000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to2), 1000);

        engine.Reset();
        UInt160 to3 = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to3.ToArray(), 1000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to3), 1000);

        engine.Reset();
        engine.ExecuteTestCaseStandard("_deploy", " ", false);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.ExecuteTestCaseStandard("roundStart");
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        var result = engine.ExecuteTestCaseStandard("getRound");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, result.Pop());

        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 1);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjects", 1);  //By round
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        sender = "NSbBAtqDrwJbBNhw6SFbVGzKTpVu5vkUTa".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjects", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("donate", 100);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(1, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("allProjects", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);

        engine.Reset();
        var from = "f8ed484eaf8c167e09760db35a4a1244aea28a08".HexToBytes();
        result = engine.ExecuteTestCaseStandard("votingCost", from, 1, 3);
        Assert.AreEqual(VMState.HALT, engine.State);
        var arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(600, arr[0]);

        engine.Reset();
        sender = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 1, 3);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(7, result.Pop());

        engine.Reset();
        sender = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 2, 1);

        Assert.AreEqual(VMState.HALT, engine.State);
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(8, result.Pop());

        engine.Reset();
        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 2, 3);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(14, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjectInfoById", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(4, arr[3]);  // totalVotes
        Assert.AreEqual(693, arr[4]);  // grants
        Assert.AreEqual(3, arr[5]);    //supportArea
        Assert.AreEqual(0, arr[6]);   //withdraw

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getSupportArea", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(3, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("grantOf", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(0, arr[0]);
        Assert.AreEqual(0, arr[1]);

        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.ExecuteTestCaseStandard("roundOver");
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getRound");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(2, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("grantOf", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(792, arr[0]);
        Assert.AreEqual(792, arr[1]);

        engine.Reset();
        sender = "NSbBAtqDrwJbBNhw6SFbVGzKTpVu5vkUTa".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.ExecuteTestCaseStandard("takeOutGrants", 2, 10);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjectInfoById", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(4, arr[3]);  // totalVotes
        Assert.AreEqual(693, arr[4]);  // grants
        Assert.AreEqual(3, arr[5]);    //supportArea
        Assert.AreEqual(10, arr[6]);   //withdraw

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("grantOf", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(782, arr[0]);
        Assert.AreEqual(792, arr[1]);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTotalSupportArea", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(3, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getSupportPool", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(99, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getPreTaxSupportPool", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(100, result.Pop());

        engine.Reset();
        StackItem acc = "7a55cb2963adda3b602b66e791dab559363112b3".HexToBytes();  //小端序
        result = engine.ExecuteTestCaseStandard("balance", acc);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1000, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(14, result.Pop());


        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("withdraw");
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTax");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(0, result.Pop());

        engine.Reset();
        acc = "7a55cb2963adda3b602b66e791dab559363112b3".HexToBytes();  //小端序
        result = engine.ExecuteTestCaseStandard("balance", acc);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1014, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("roundInfo", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(99, arr[2]);
        Assert.AreEqual(100, arr[3]);

    }

    //banProject ---
    [TestMethod]
    public void Test3()
    {
        var contract = _engine.EntryScriptHash;
        using ScriptBuilder sb = new();
        sb.EmitDynamicCall(contract, " ");
        var sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion); //owner
        var tx = BuildTransaction(sender, sb.ToArray());


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
        engine.AddEntryScript("./TestClasses/Contract_Grant.cs");
        engine.Snapshot.ContractAdd(new ContractState()
        {
            Hash = contract,
            Nef = _engine.Nef,
            Manifest = ContractManifest.FromJson(_engine.Manifest),
        });

        // set account   
        UInt160 account = Contract.GetBFTAddress(ProtocolSettings.Default.StandbyValidators);
        tx.Signers = new Signer[] { new Signer { Account = account, Scopes = WitnessScope.Global } };
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, account), 100_000_000);
        UInt160 to1 = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to1.ToArray(), 10000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to1), 10000);

        engine.Reset();
        UInt160 to2 = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to2.ToArray(), 10000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to2), 10000);

        engine.Reset();
        UInt160 to3 = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        engine.ExecuteTestCaseStandard("transfer_NEO", account.ToArray(), to3.ToArray(), 10000);
        Assert.AreEqual(Native.NeoToken.NEO.BalanceOf(engine.Snapshot, to3), 10000);

        engine.Reset();
        engine.ExecuteTestCaseStandard("_deploy", " ", false);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.ExecuteTestCaseStandard("roundStart");
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        var result = engine.ExecuteTestCaseStandard("getRound");
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(1, result.Pop());

        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 1);
        Assert.AreEqual(VMState.HALT, engine.State);

        sender = "NSbBAtqDrwJbBNhw6SFbVGzKTpVu5vkUTa".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("uploadProject", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        sender = "NaJnLbdAxaZ4a5CeNRf88XUrnMRXhggEc6".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        engine.Reset();
        result = engine.ExecuteTestCaseStandard("donate", 100);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        sender = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 1, 3);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 1, 2);
        Assert.AreEqual(VMState.HALT, engine.State);

        engine.Reset();
        sender = "NicB21AML9K4TMT1qwFCjSkjCWTCVYfSTf".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 2, 1);
        Assert.AreEqual(VMState.HALT, engine.State);


        engine.Reset();
        sender = "NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r".ToScriptHash(ProtocolSettings.Default.AddressVersion);
        tx.Signers = new Signer[] { new Signer { Account = sender, Scopes = WitnessScope.Global } };
        result = engine.ExecuteTestCaseStandard("vote", 2, 5);
        Assert.AreEqual(VMState.HALT, engine.State);


        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjectInfoById", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        var arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(5, arr[3]);  // totalVotes
        Assert.AreEqual(891, arr[4]);  // grants
        Assert.AreEqual(6, arr[5]);    //supportArea
        Assert.AreEqual(0, arr[6]);   //withdraw

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getProjectInfoById", 2);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(6, arr[3]);  // totalVotes
        Assert.AreEqual(1584, arr[4]);  // grants
        Assert.AreEqual(5, arr[5]);    //supportArea
        Assert.AreEqual(0, arr[6]);   //withdraw

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTotalSupportArea", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(11, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("banProject", 1, true);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTotalSupportArea", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(5, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("banProject", 1, false);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(0, engine.ResultStack.Count);

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("getTotalSupportArea", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.AreEqual(11, result.Pop());

        engine.Reset();
        result = engine.ExecuteTestCaseStandard("rankingList", 1);
        Assert.AreEqual(VMState.HALT, engine.State);
        arr = (VM.Types.Array)result.Pop().ConvertTo(StackItemType.Array);
        Assert.AreEqual(9, arr[0]);

    }

    //----
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


}
}

