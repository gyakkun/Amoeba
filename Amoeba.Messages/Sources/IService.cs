using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Configuration;
using Omnius.Security;

namespace Amoeba.Messages
{
    public interface IService : ISettings
    {
        ServiceReport Report { get; }
        IEnumerable<NetworkConnectionReport> GetNetworkConnectionReports();
        IEnumerable<CacheContentReport> GetCacheContentReports();
        IEnumerable<DownloadContentReport> GetDownloadContentReports();
        ServiceConfig Config { get; }
        void SetConfig(ServiceConfig config);
        void SetCloudLocations(IEnumerable<Location> locations);
        long Size { get; }
        void Resize(long size);
        Task CheckBlocks(Action<CheckBlocksProgressReport> progress, CancellationToken token);
        Task<Metadata> AddContent(string path, DateTime creationTime, CancellationToken token);
        void RemoveContent(string path);
        void DiffuseContent(string path);
        void AddDownload(Metadata metadata, string path, long maxLength);
        void RemoveDownload(Metadata metadata, string path);
        void ResetDownload(Metadata metadata, string path);
        Task SetProfile(Profile profile, DigitalSignature digitalSignature, CancellationToken token);
        Task SetStore(Store store, DigitalSignature digitalSignature, CancellationToken token);
        Task SetMailMessage(Signature targetSignature, MailMessage mailMessage, AgreementPublicKey agreementPublicKey, DigitalSignature digitalSignature, CancellationToken token);
        Task SetChatMessage(Tag tag, ChatMessage chatMessage, DigitalSignature digitalSignature, TimeSpan miningTime, CancellationToken token);
        Task<BroadcastMessage<Profile>> GetProfile(Signature signature, CancellationToken token);
        Task<BroadcastMessage<Store>> GetStore(Signature signature, CancellationToken token);
        Task<IEnumerable<UnicastMessage<MailMessage>>> GetMailMessages(Signature signature, AgreementPrivateKey agreementPrivateKey, CancellationToken token);
        Task<IEnumerable<MulticastMessage<ChatMessage>>> GetChatMessages(Tag tag, CancellationToken token);
    }
}
