﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Execution;
using AElf.Types.CSharp;
using Google.Protobuf;
using ServiceStack;
using AElf.Common;
using AElf.Execution.Execution;
using AElf.Kernel.Persistence;

namespace AElf.Contracts.Token.Tests
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

        public IStateDao StateDao { get; }
        public Hash ChainId1 { get; } = Hash.Generate();
        public ISmartContractDao SmartContractDao;
        public ISmartContractService SmartContractService { get; }
        private IFunctionMetadataService _functionMetadataService;

        public Address TokenContractAddress { get; private set; }
        
        public IChainContextService ChainContextService;

        public ServicePack ServicePack;

        private IChainCreationService _chainCreationService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IStateDao stateDao, ISmartContractService smartContractService,
            IChainCreationService chainCreationService, IChainContextService chainContextService,
            IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            StateDao = stateDao;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = smartContractService;

            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = null,
                StateDao = StateDao
            };
        }

        public byte[] TokenCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll")))
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
                ContractBytes = ByteString.CopyFrom(TokenCode),
                ContractHash = Hash.FromRawBytes(TokenCode),
                Type = (int)SmartContractType.TokenContract
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
                    new List<SmartContractRegistration> {reg0});
        }
        
        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }
    }
}
