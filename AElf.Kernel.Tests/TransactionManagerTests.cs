﻿using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Miner.TxMemPool;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TransactionManagerTests
    {
        private ITransactionManager _manager;

        public TransactionManagerTests(ITransactionManager manager)
        {
            _manager = manager;
        }

        [Fact]
        public async Task TestInsert()
        {
            await _manager.AddTransactionAsync(new Transaction
            {
                From = Address.Generate(),
                To = Address.Generate()
            });
        }

        [Fact]
        public async Task GetTest()
        {
            var t = BuildTransaction();
            var key = await _manager.AddTransactionAsync(t);
            var td = await _manager.GetTransaction(key);
            Assert.Equal(t, td);
        }
        
        public static Transaction BuildTransaction(Address adrTo = null, ulong nonce = 0, ECKeyPair keyPair = null)
        {
            keyPair = keyPair ?? new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = Address.Generate();
            tx.To = adrTo ?? Address.Generate();
            tx.IncrementId = nonce;
            
            //todo review probably useless - or a proper sig is needed
            //            var sig = new Sig
            //            {
            //                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded())
            //            };
            //            tx.Sigs.Add(sig);
            
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";
            tx.Params = ByteString.CopyFrom(new Parameters
            {
                Params = { new Param
                {
                    IntVal = 1
                }}
            }.ToByteArray());

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.DumpByteArray());
            
            // Update the signature
            //todo review probably useless - or a proper sig is needed
            //tx.Sig = ByteString.CopyFrom(signature.SigBytes);
            
            return tx;
        }
    }
}