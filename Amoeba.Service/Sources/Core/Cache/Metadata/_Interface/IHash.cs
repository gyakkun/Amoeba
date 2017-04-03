
namespace Amoeba.Service
{
    interface IHash
    {
        HashAlgorithm Algorithm { get; }
        byte[] Value { get; }
    }
}
