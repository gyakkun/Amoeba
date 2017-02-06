using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = "Link")]
    public sealed class Link : ItemBase<Link>, ILink
    {
        private enum SerializeId
        {
            TrustSignature = 0,
            DeleteSignature = 1,
        }

        private SignatureCollection _trustSignatures;
        private SignatureCollection _deleteSignatures;

        public static readonly int MaxTrustSignatureCount = 1024;
        public static readonly int MaxDeleteSignatureCount = 1024;

        public Link(IEnumerable<Signature> trustSignatures, IEnumerable<Signature> deleteSignatures)
        {
            if (trustSignatures != null) this.ProtectedTrustSignatures.AddRange(trustSignatures);
            if (deleteSignatures != null) this.ProtectedDeleteSignatures.AddRange(deleteSignatures);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.TrustSignature)
                    {
                        this.ProtectedTrustSignatures.Add(Signature.Import(reader.GetStream(), bufferManager));
                    }
                    else if (id == (int)SerializeId.DeleteSignature)
                    {
                        this.ProtectedDeleteSignatures.Add(Signature.Import(reader.GetStream(), bufferManager));
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // TrustSignatures
                foreach (var value in this.TrustSignatures)
                {
                    writer.Write((int)SerializeId.TrustSignature);
                    writer.Write(value.Export(bufferManager));
                }
                // DeleteSignatures
                foreach (var value in this.DeleteSignatures)
                {
                    writer.Write((int)SerializeId.DeleteSignature);
                    writer.Write(value.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.ProtectedTrustSignatures.Count == 0) return 0;
            else return this.ProtectedTrustSignatures[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Link)) return false;

            return this.Equals((Link)obj);
        }

        public override bool Equals(Link other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (!CollectionUtils.Equals(this.TrustSignatures, other.TrustSignatures)
                || !CollectionUtils.Equals(this.DeleteSignatures, other.DeleteSignatures))
            {
                return false;
            }

            return true;
        }

        #region ILink

        private volatile ReadOnlyCollection<Signature> _readOnlyTrustSignatures;

        public IEnumerable<Signature> TrustSignatures
        {
            get
            {
                if (_readOnlyTrustSignatures == null)
                    _readOnlyTrustSignatures = new ReadOnlyCollection<Signature>(this.ProtectedTrustSignatures.ToArray());

                return _readOnlyTrustSignatures;
            }
        }

        [DataMember(Name = "TrustSignatures")]
        private SignatureCollection ProtectedTrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new SignatureCollection(Link.MaxTrustSignatureCount);

                return _trustSignatures;
            }
        }

        private volatile ReadOnlyCollection<Signature> _readOnlyDeleteSignatures;

        public IEnumerable<Signature> DeleteSignatures
        {
            get
            {
                if (_readOnlyDeleteSignatures == null)
                    _readOnlyDeleteSignatures = new ReadOnlyCollection<Signature>(this.ProtectedDeleteSignatures.ToArray());

                return _readOnlyDeleteSignatures;
            }
        }

        [DataMember(Name = "DeleteSignatures")]
        private SignatureCollection ProtectedDeleteSignatures
        {
            get
            {
                if (_deleteSignatures == null)
                    _deleteSignatures = new SignatureCollection(Link.MaxDeleteSignatureCount);

                return _deleteSignatures;
            }
        }

        #endregion
    }
}
