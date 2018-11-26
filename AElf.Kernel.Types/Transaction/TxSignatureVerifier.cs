using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.Types.Transaction
{
    public class TxSignatureVerifier : ITxSignatureVerifier
    {
        public bool Verify(Kernel.Transaction tx)
        {
            if (tx.Sigs == null || tx.Sigs.Count == 0)
            {
                return false;
            }

            if (tx.Sigs.Count == 1)
            {
                // Check the address of signer if only one signer.
                var pubKey = tx.Sigs[0].P.ToByteArray();
                var addr = Address.FromRawBytes(pubKey);

                if (!addr.Equals(tx.From))
                    return false;
            }
            
            foreach (var sig in tx.Sigs)
            {
                var pubKey = sig.P.ToByteArray();
                var keyPair = ECKeyPair.FromPublicKey(pubKey);
                var verifier = new ECVerifier(keyPair);
                var signature = new ECSignature(sig.R.ToByteArray(), sig.S.ToByteArray());
                if(verifier.Verify(signature, tx.GetHash().DumpByteArray()))
                    continue;
                return false;
            }
            return true;
        }
    }
}