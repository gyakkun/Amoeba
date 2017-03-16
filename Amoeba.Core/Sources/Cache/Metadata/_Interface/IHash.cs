
namespace Amoeba.Core
{
    interface IHash
    {
        HashAlgorithm Algorithm { get; }
        byte[] Value { get; }
    }
}
