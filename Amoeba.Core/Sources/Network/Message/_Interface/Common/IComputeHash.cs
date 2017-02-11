
namespace Amoeba.Core
{
    interface IComputeHash
    {
        Hash CreateHash(HashAlgorithm hashAlgorithm);
        bool VerifyHash(Hash hash);
    }
}
