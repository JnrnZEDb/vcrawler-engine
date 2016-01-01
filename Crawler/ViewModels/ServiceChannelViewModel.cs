﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using Crawler.Common;
using DataAPI.POCO;
using DataAPI.Videos;
using Extensions.Helpers;
using Interfaces.Enums;
using Interfaces.Models;
using Models.Factories;

namespace Crawler.ViewModels
{
    public sealed class ServiceChannelViewModel : IChannel, INotifyPropertyChanged
    {
        #region Static and Readonly Fields

        private readonly MainWindowViewModel mainVm;
        private readonly Dictionary<string, List<IVideoItem>> popCountriesDictionary;

        #endregion

        #region Fields

        private RelayCommand fillPopularCommand;
        private string filterVideoKey;
        private RelayCommand searchCommand;
        private string selectedCountry;
        private IVideoItem selectedItem;
        private CredImage selectedSite;
        private RelayCommand siteChangedCommand;

        #endregion

        #region Constructors

        public ServiceChannelViewModel(MainWindowViewModel mainVm)
        {
            this.mainVm = mainVm;
            Title = "#Popular";
            Countries = new[] { "RU", "US", "CA", "FR", "DE", "IT", "JP" };
            popCountriesDictionary = new Dictionary<string, List<IVideoItem>>();
            SelectedCountry = Countries.First();
            SupportedSites = new List<CredImage>();
            ChannelItems = new ObservableCollection<IVideoItem>();
            Site = SiteType.NotSet;
            ChannelItemsCollectionView = CollectionViewSource.GetDefaultView(ChannelItems);
        }

        #endregion

        #region Properties

        public IEnumerable<string> Countries { get; private set; }

        public RelayCommand FillPopularCommand
        {
            get
            {
                return fillPopularCommand ?? (fillPopularCommand = new RelayCommand(async x => await FillPopular()));
            }
        }

        public RelayCommand SearchCommand
        {
            get
            {
                return searchCommand ?? (searchCommand = new RelayCommand(async x => await Search()));
            }
        }

        public string SearchKey { get; set; }

        public string SelectedCountry
        {
            get
            {
                return selectedCountry;
            }
            set
            {
                if (value == selectedCountry)
                {
                    return;
                }
                selectedCountry = value;
                OnPropertyChanged();
                List<IVideoItem> lst;
                if (!popCountriesDictionary.TryGetValue(selectedCountry, out lst))
                {
                    return;
                }
                if (!lst.Any())
                {
                    return;
                }
                ChannelItems.Clear();
                foreach (IVideoItem item in lst)
                {
                    AddNewItem(item);
                }
            }
        }

        public CredImage SelectedSite
        {
            get
            {
                return selectedSite;
            }
            private set
            {
                if (Equals(value, selectedSite))
                {
                    return;
                }
                selectedSite = value;
            }
        }

        public RelayCommand SiteChangedCommand
        {
            get
            {
                return siteChangedCommand ?? (siteChangedCommand = new RelayCommand(x => SiteChanged()));
            }
        }

        public List<CredImage> SupportedSites { get; private set; }

        #endregion

        #region Methods

        public void FillCredImages()
        {
            foreach (ICred cred in mainVm.SettingsViewModel.SupportedCreds.Where(x => x.Site != SiteType.NotSet))
            {
                switch (cred.Site)
                {
                    case SiteType.YouTube:
                        SupportedSites.Add(new CredImage(cred, "Crawler.Images.pop.png"));
                        break;

                    case SiteType.RuTracker:

                        SupportedSites.Add(new CredImage(cred, "Crawler.Images.rt.png"));
                        break;

                    case SiteType.Tapochek:

                        SupportedSites.Add(new CredImage(cred, "Crawler.Images.tap.png"));
                        break;
                }
            }
            SelectedSite = SupportedSites.First();
        }

        public async Task Search()
        {
            if (string.IsNullOrEmpty(SearchKey))
            {
                return;
            }

            mainVm.SetStatus(1);

            switch (SelectedSite.Cred.Site)
            {
                case SiteType.YouTube:

                    List<VideoItemPOCO> lst = (await YouTubeSite.SearchItemsAsync(SearchKey, SelectedCountry, 50)).ToList();
                    if (lst.Any())
                    {
                        for (int i = ChannelItems.Count; i > 0; i--)
                        {
                            if (
                                !(ChannelItems[i - 1].FileState == ItemState.LocalYes
                                  || ChannelItems[i - 1].FileState == ItemState.Downloading))
                            {
                                ChannelItems.RemoveAt(i - 1);
                            }
                        }
                        foreach (IVideoItem item in lst.Select(VideoItemFactory.CreateVideoItem))
                        {
                            AddNewItem(item);
                            item.IsHasLocalFileFound(mainVm.SettingsViewModel.DirPath);
                        }
                    }
                    break;
            }

            // SelectedChannel = channel;
            mainVm.SelectedChannel.ChannelItemsCount = ChannelItems.Count;
            mainVm.SetStatus(0);
        }

        private async Task FillPopular()
        {
            if (ChannelItems.Any())
            {
                // чтоб не удалять список отдельных закачек, но почистить прошлые популярные
                for (int i = ChannelItems.Count; i > 0; i--)
                {
                    if (!(ChannelItems[i - 1].FileState == ItemState.LocalYes || ChannelItems[i - 1].FileState == ItemState.Downloading))
                    {
                        ChannelItems.RemoveAt(i - 1);
                    }
                }
            }

            mainVm.SetStatus(1);
            switch (SelectedSite.Cred.Site)
            {
                case SiteType.YouTube:

                    IEnumerable<VideoItemPOCO> lst = await YouTubeSite.GetPopularItemsAsync(SelectedCountry, 30);
                    var lstemp = new List<IVideoItem>();
                    foreach (VideoItemPOCO poco in lst)
                    {
                        IVideoItem item = VideoItemFactory.CreateVideoItem(poco);
                        AddNewItem(item);
                        item.IsHasLocalFileFound(mainVm.SettingsViewModel.DirPath);
                        if (mainVm.Channels.Select(x => x.ID).Contains(item.ParentID))
                        {
                            // подсветим видео, если канал уже есть в подписке
                            item.SyncState = SyncState.Added;
                        }
                        lstemp.Add(item);
                    }
                    if (popCountriesDictionary.ContainsKey(SelectedCountry))
                    {
                        popCountriesDictionary.Remove(SelectedCountry);
                    }
                    popCountriesDictionary.Add(SelectedCountry, lstemp);
                    break;
            }

            mainVm.SetStatus(0);
        }

        private bool FilterVideo(object item)
        {
            var value = (IVideoItem)item;
            if (value == null || value.Title == null)
            {
                return false;
            }

            return value.Title.ToLower().Contains(FilterVideoKey.ToLower());
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void SiteChanged()
        {
            mainVm.SelectedChannel = this;
        }

        #endregion

        #region IChannel Members

        public CookieContainer ChannelCookies { get; set; }
        public ObservableCollection<IVideoItem> ChannelItems { get; set; }
        public ICollectionView ChannelItemsCollectionView { get; set; }
        public int ChannelItemsCount { get; set; }
        public ObservableCollection<IPlaylist> ChannelPlaylists { get; set; }
        public ChannelState ChannelState { get; set; }
        public ObservableCollection<ITag> ChannelTags { get; set; }
        public int CountNew { get; set; }
        public string DirPath { get; set; }

        public string FilterVideoKey
        {
            get
            {
                return filterVideoKey;
            }
            set
            {
                if (value == filterVideoKey)
                {
                    return;
                }

                filterVideoKey = value;
                ChannelItemsCollectionView.Filter = FilterVideo;
                OnPropertyChanged();
            }
        }

        public string ID { get; set; }
        public bool IsShowSynced { get; set; }
        public int PlaylistCount { get; set; }

        public IVideoItem SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                if (Equals(value, selectedItem))
                {
                    return;
                }
                selectedItem = value;
                OnPropertyChanged();
            }
        }

        public SiteType Site { get; set; }
        public string SubTitle { get; set; }
        public byte[] Thumbnail { get; set; }
        public string Title { get; set; }

        public void AddNewItem(IVideoItem item)
        {
            item.FileState = ItemState.LocalNo;

            if (item.SyncState == SyncState.Added)
            {
                ChannelItems.Insert(0, item);
            }
            else
            {
                ChannelItems.Add(item);
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Nested type: CredImage

        public class CredImage
        {
            #region Constructors

            public CredImage(ICred cred, string resourcepic)
            {
                Cred = cred;
                Stream img = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcepic);
                Thumbnail = StreamHelper.ReadFully(img);
            }

            #endregion

            #region Properties

            public ICred Cred { get; private set; }
            public byte[] Thumbnail { get; private set; }

            #endregion
        }

        #endregion
    }
}
