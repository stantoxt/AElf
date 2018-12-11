﻿    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AElf.ChainController;
    using AElf.ChainController.CrossChain;
    using AElf.Execution;
    using AElf.Kernel;
    using AElf.Kernel.Managers;
    using AElf.Kernel.Storages;
    using AElf.SmartContract;
    using Google.Protobuf;
    using ServiceStack;
using    AElf.Common;
    using AElf.Database;
    using AElf.Execution.Execution;
    using AElf.Kernel.Persistence;
    using AElf.Miner.TxMemPool;
    using AElf.Runtime.CSharp;
    using AElf.SmartContract.Metadata;
    using NLog;

namespace AElf.Contracts.SideChain.Tests
    {
        public class MockSetup
        {
            // IncrementId is used to differentiate txn
            // which is identified by From/To/IncrementId
            private static int _incrementId = 0;
            public ulong NewIncrementId()
            {
                var n = Interlocked.Increment(ref _incrementId);
                return (ulong)n;
            }
    
            public Hash ChainId1 { get; } = Hash.FromString("ChainId1");
            public IStateDao StateDao { get; private set; }
            public ISmartContractDao SmartContractDao;
            public ISmartContractService SmartContractService;
            public IChainService ChainService;
            private IFunctionMetadataService _functionMetadataService;
    
            private IChainCreationService _chainCreationService;
    
            private ISmartContractRunnerFactory _smartContractRunnerFactory;
            private ILogger _logger;
            private IDataStore _dataStore;
            private IKeyValueDatabase _database;
            private ILightChainCanonicalDao _lightChainCanonicalDao;

            public MockSetup(ILogger logger)
            {
                _logger = logger;
                Initialize();
            }
    
            private void Initialize()
            {
                NewStorage();
                var transactionManager = new TransactionDao(_database);
                var transactionTraceManager = new TransactionTraceDao(_database);
                var callingGraphDao = new CallingGraphDao(_database);
                var functionMetadataDao = new FunctionMetadataDao(_database);
                _functionMetadataService = new FunctionMetadataService(callingGraphDao, functionMetadataDao, _logger);
                var chainManagerBasic = new ChainDao(_database);
                _lightChainCanonicalDao =new LightChainCanonicalDao(_database);
                ChainService = new ChainService(chainManagerBasic, new BlockDao(_database),
                    transactionManager, transactionTraceManager, _dataStore, StateDao, _lightChainCanonicalDao);
                _smartContractRunnerFactory = new SmartContractRunnerFactory();
                var runner = new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
                _smartContractRunnerFactory.AddRunner(0, runner);
                _chainCreationService = new ChainCreationService(ChainService,
                    new SmartContractService(new SmartContractDao(_database), _smartContractRunnerFactory,
                        StateDao, _functionMetadataService), _logger);
                SmartContractDao = new SmartContractDao(_database);
                Task.Factory.StartNew(async () =>
                {
                    await Init();
                }).Unwrap().Wait();
                SmartContractService = new SmartContractService(SmartContractDao, _smartContractRunnerFactory, StateDao, _functionMetadataService);
                ChainService = new ChainService(new ChainDao(_database), new BlockDao(_database), new TransactionDao(_database), new TransactionTraceDao(_database), _dataStore, StateDao,_lightChainCanonicalDao);
            }

            private void NewStorage()
            {
                _database = new InMemoryDatabase();
                StateDao = new StateDao(_database);
                _dataStore = new DataStore(_database);
            }
            
            public byte[] SideChainCode
            {
                get
                {
                    byte[] code = null;
                    using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.SideChain/bin/Debug/netstandard2.0/AElf.Contracts.SideChain.dll")))
                    {
                        code = file.ReadFully();
                    }
                    return code;
                }
            }
            
            public byte[] SCZeroContractCode
            {
                get
                {
                    byte[] code = null;
                    using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll")))
                    {
                        code = file.ReadFully();
                    }
                    return code;
                }
            }
            
            private async Task Init()
            {
                var reg1 = new SmartContractRegistration
                {
                    Category = 0,
                    ContractBytes = ByteString.CopyFrom(SideChainCode),
                    ContractHash = Hash.FromRawBytes(SideChainCode),
                    Type = (int)SmartContractType.SideChainContract
                };
                var reg0 = new SmartContractRegistration
                {
                    Category = 0,
                    ContractBytes = ByteString.CopyFrom(SCZeroContractCode),
                    ContractHash = Hash.FromRawBytes(SCZeroContractCode),
                    Type = (int)SmartContractType.BasicContractZero
                };
    
                var chain1 =
                    await _chainCreationService.CreateNewChainAsync(ChainId1,
                        new List<SmartContractRegistration> {reg0, reg1});
            }
            
            public async Task<IExecutive> GetExecutiveAsync(Address address)
            {
                var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
                return executive;
            }
        }
    }
