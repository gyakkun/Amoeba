using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(Index))]
    sealed class Index : ItemBase<Index>, IIndex
    {
        private enum SerializeId
        {
            Groups = 0,
        }

        private GroupCollection _groups;

        public Index(IEnumerable<Group> groupes)
        {
            if (groupes != null) this.ProtectedGroups.AddRange(groupes);
        }

        protected override void Initialize()
        {

        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                while (reader.Available > 0)
                {
                    int id = (int)reader.GetUInt32();

                    if (id == (int)SerializeId.Groups)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedGroups.Add(Group.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Groups
                if (this.ProtectedGroups.Count > 0)
                {
                    writer.Write((uint)SerializeId.Groups);
                    writer.Write((uint)this.ProtectedGroups.Count);

                    foreach (var item in this.ProtectedGroups)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.ProtectedGroups.Count == 0) return 0;
            else return this.ProtectedGroups[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Index)) return false;

            return this.Equals((Index)obj);
        }

        public override bool Equals(Index other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (!CollectionUtils.Equals(this.Groups, other.Groups))
            {
                return false;
            }

            return true;
        }

        #region IIndex

        private volatile ReadOnlyCollection<Group> _readOnlyGroups;

        public IEnumerable<Group> Groups
        {
            get
            {
                if (_readOnlyGroups == null)
                    _readOnlyGroups = new ReadOnlyCollection<Group>(this.ProtectedGroups);

                return _readOnlyGroups;
            }
        }

        [DataMember(Name = nameof(Groups))]
        private GroupCollection ProtectedGroups
        {
            get
            {
                if (_groups == null)
                    _groups = new GroupCollection();

                return _groups;
            }
        }

        #endregion
    }
}
