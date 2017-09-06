
namespace Amoeba.Messages
{
    interface IHash
    {
        HashAlgorithm Algorithm { get; }
        byte[] Value { get; }
    }
}
