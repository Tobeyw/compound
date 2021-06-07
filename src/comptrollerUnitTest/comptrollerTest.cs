using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.SmartContract;
using Neo.Compiler.CSharp.UnitTests.Utils;
using Neo.VM.Types;
using Neo.VM;
using Neo.IO.Json;
using System.Numerics;
using Neo;
using Neo.Wallets;
using Neo.Cryptography;
using Neo.Network.P2P;

namespace CrossChainProxy_Contract.Tests
{
    [TestClass()]
    public class ProxyTests
    {
        private TestEngine _engine;

        [TestInitialize]
        public void Init()
        {
            _engine = new TestEngine(snapshot: new TestDataCache());
            _engine.AddEntryScript(@"D:\project\CrossChain\3.0version\flamingo_contract_crosschain\CrossChainProxy_Contract\Proxy.cs"); //CCMC 合约文件路径
            _engine.Snapshot.ContractAdd(new ContractState()
            {
                Hash = _engine.EntryScriptHash,
                Nef = _engine.Nef,
                Manifest = new Neo.SmartContract.Manifest.ContractManifest()
            });
        }
        [TestMethod()]
        public void UnlockTest()
        {
            _engine.Reset();
            StackItem toChainId = new byte[] { 0x06 };
            StackItem proxyHash = "4c10b303ae347d7c0fb57fb5c3404b8c1c903505".HexToBytes();
            _engine.ExecuteTestCaseStandard("bindProxyHash", toChainId, proxyHash);
            var rawResultOfBind = _engine.ResultStack.Pop();

            _engine.Reset();
            StackItem inputBytes = "14ef4073a0f2b305a38ec4050e4d3d28bc40ea63f5140ef656d72483fab3804c41ea0f052dab8138da171300000000000000000000000000000000000000000000000000000000000000".HexToBytes();
            _engine.ExecuteTestCaseStandard("unlock", inputBytes, proxyHash, toChainId);
            var rawResultOfUnlock = _engine.ResultStack.Pop();
        }
    }
}