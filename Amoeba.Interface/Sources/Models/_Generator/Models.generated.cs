using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using Amoeba.Service;
using Amoeba.Core;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(StoreCategoryInfo))]
    partial class StoreCategoryInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        { 
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        { 
            get
            {
                return _isExpanded;
            }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private ObservableCollection<StoreInfo> _storeInfos;

        [DataMember(Name = nameof(StoreInfos))]
        public ObservableCollection<StoreInfo> StoreInfos
        { 
            get
            {
                if(_storeInfos == null)
                    _storeInfos = new ObservableCollection<StoreInfo>();

                return _storeInfos;
            }
        }

        private ObservableCollection<StoreCategoryInfo> _categoryInfos;

        [DataMember(Name = nameof(CategoryInfos))]
        public ObservableCollection<StoreCategoryInfo> CategoryInfos
        { 
            get
            {
                if(_categoryInfos == null)
                    _categoryInfos = new ObservableCollection<StoreCategoryInfo>();

                return _categoryInfos;
            }
        }
    }
    [DataContract(Name = nameof(StoreInfo))]
    partial class StoreInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        { 
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        { 
            get
            {
                return _isExpanded;
            }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private bool _isUpdated;

        [DataMember(Name = nameof(IsUpdated))]
        public bool IsUpdated
        { 
            get
            {
                return _isUpdated;
            }
            set
            {
                if (value != _isUpdated)
                {
                    _isUpdated = value;
                    this.OnPropertyChanged(nameof(IsUpdated));
                }
            }
        }

        private ObservableCollection<BoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<BoxInfo> BoxInfos
        { 
            get
            {
                if(_boxInfos == null)
                    _boxInfos = new ObservableCollection<BoxInfo>();

                return _boxInfos;
            }
        }
    }
    [DataContract(Name = nameof(BoxInfo))]
    partial class BoxInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        { 
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        { 
            get
            {
                return _isExpanded;
            }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private ObservableCollection<SeedInfo> _seedInfos;

        [DataMember(Name = nameof(SeedInfos))]
        public ObservableCollection<SeedInfo> SeedInfos
        { 
            get
            {
                if(_seedInfos == null)
                    _seedInfos = new ObservableCollection<SeedInfo>();

                return _seedInfos;
            }
        }

        private ObservableCollection<BoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<BoxInfo> BoxInfos
        { 
            get
            {
                if(_boxInfos == null)
                    _boxInfos = new ObservableCollection<BoxInfo>();

                return _boxInfos;
            }
        }
    }
    [DataContract(Name = nameof(SeedInfo))]
    partial class SeedInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        { 
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        { 
            get
            {
                return _length;
            }
            set
            {
                if (value != _length)
                {
                    _length = value;
                    this.OnPropertyChanged(nameof(Length));
                }
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        { 
            get
            {
                return _creationTime;
            }
            set
            {
                if (value != _creationTime)
                {
                    _creationTime = value;
                    this.OnPropertyChanged(nameof(CreationTime));
                }
            }
        }

        private Metadata _metadata;

        [DataMember(Name = nameof(Metadata))]
        public Metadata Metadata
        { 
            get
            {
                return _metadata;
            }
            set
            {
                if (value != _metadata)
                {
                    _metadata = value;
                    this.OnPropertyChanged(nameof(Metadata));
                }
            }
        }
    }
}
