﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System;
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
using DataAPI.Database;
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

        private readonly List<IVideoItem> addedList;
        private readonly SqLiteDatabase db;
        private readonly List<IVideoItem> plannedList;
        private readonly Dictionary<string, List<IVideoItem>> popCountriesDictionary;
        private readonly List<IVideoItem> watchedList;

        #endregion

        #region Fields

        private string filterVideoKey;
        private string selectedCountry;
        private IVideoItem selectedItem;
        private CredImage selectedSite;

        #endregion

        #region Constructors

        public ServiceChannelViewModel(SqLiteDatabase db)
        {
            this.db = db;
            Title = "#Popular";
            Countries = new[] { "RU", "US", "CA", "FR", "DE", "IT", "JP" };
            popCountriesDictionary = new Dictionary<string, List<IVideoItem>>();
            SelectedCountry = Countries.First();
            SupportedSites = new List<CredImage>();
            ChannelItems = new ObservableCollection<IVideoItem>();
            ChannelItemsCollectionView = CollectionViewSource.GetDefaultView(ChannelItems);
            ChannelPlaylists = new ObservableCollection<IPlaylist>();
            addedList = new List<IVideoItem>();
            plannedList = new List<IVideoItem>();
            watchedList = new List<IVideoItem>();
        }

        #endregion

        #region Properties

        public IEnumerable<string> Countries { get; private set; }
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

        public List<CredImage> SupportedSites { get; private set; }

        #endregion

        #region Methods

        public void AddToStateList(object state, IVideoItem item)
        {
            if (state is WatchState)
            {
                var st = (WatchState)state;
                switch (st)
                {
                    case WatchState.Watched:
                        if (!watchedList.Contains(item))
                        {
                            watchedList.Add(item);
                        }
                        break;
                    case WatchState.Planned:
                        if (!plannedList.Contains(item))
                        {
                            plannedList.Add(item);
                        }

                        if (watchedList.Contains(item))
                        {
                            watchedList.Remove(item);
                        }
                        break;

                    case WatchState.Notset:
                        if (plannedList.Contains(item))
                        {
                            plannedList.Remove(item);
                        }
                        break;
                }
            }
            else if (state is SyncState)
            {
                var st = (SyncState)state;
                switch (st)
                {
                    case SyncState.Added:
                        addedList.Add(item);
                        break;

                    case SyncState.Notset:
                        if (addedList.Contains(item))
                        {
                            addedList.Remove(item);
                        }
                        break;
                }
            }
        }

        public async Task FillPopular(HashSet<string> ids)
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

            switch (SelectedSite.Cred.Site)
            {
                case SiteType.YouTube:

                    List<VideoItemPOCO> lst = await YouTubeSite.GetPopularItemsAsync(SelectedCountry, 30);
                    var lstemp = new List<IVideoItem>();
                    foreach (IVideoItem item in lst.Select(poco => VideoItemFactory.CreateVideoItem(poco, SiteType.YouTube)))
                    {
                        AddNewItem(item);
                        item.IsHasLocalFileFound(DirPath);
                        if (ids.Contains(item.ParentID))
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
        }

        public async void Init(IEnumerable<ICred> supportedCreds, string dirPath)
        {
            DirPath = dirPath;

            foreach (ICred cred in supportedCreds.Where(x => x.Site != SiteType.NotSet))
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
            IPlaylist pla = PlaylistFactory.CreatePlaylist(SiteType.NotSet);
            pla.Title = SyncState.Added.ToString();
            pla.Thumbnail = StreamHelper.ReadFully(Assembly.GetExecutingAssembly().GetManifestResourceStream("Crawler.Images.new_48.png"));
            pla.PlItems = await Task.Run(() => db.GetWatchStateListItemsAsync(SyncState.Added));
            pla.State = SyncState.Added;
            ChannelPlaylists.Add(pla);

            var servpl = new List<WatchState> { WatchState.Planned, WatchState.Watched };
            foreach (WatchState state in servpl)
            {
                IPlaylist pl = PlaylistFactory.CreatePlaylist(SiteType.NotSet);
                pl.WatchState = state;
                pl.Title = state.ToString();
                pl.PlItems = await Task.Run(() => db.GetWatchStateListItemsAsync(pl.WatchState));
                string path = state == WatchState.Planned ? "Crawler.Images.time_48.png" : "Crawler.Images.done_48.png";
                Stream img = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                if (img != null)
                {
                    pl.Thumbnail = StreamHelper.ReadFully(img);
                }
                ChannelPlaylists.Add(pl);
            }
            PlaylistCount = ChannelPlaylists.Count;
        }

        public bool IsAllItemsExist(object state, List<string> ids)
        {
            if (state is WatchState)
            {
                var st = (WatchState)state;
                switch (st)
                {
                    case WatchState.Watched:
                        return watchedList.Any(x => ids.Contains(x.ID));
                    case WatchState.Planned:
                        return plannedList.Any(x => ids.Contains(x.ID));
                }
            }
            else if (state is SyncState)
            {
                var st = (SyncState)state;
                switch (st)
                {
                    case SyncState.Added:
                        return addedList.Any(x => ids.Contains(x.ID));
                }
            }
            return false;
        }

        public void ReloadFilteredLists(object state)
        {
            if (state == null)
            {
                List<IVideoItem> lst;
                if (!popCountriesDictionary.TryGetValue(SelectedCountry, out lst))
                {
                    return;
                }
                if (!lst.Any())
                {
                    return;
                }
                ChannelItems.Clear();
                lst.ForEach(x => ChannelItems.Add(x));
            }

            else
            {
                ChannelItems.Clear();
                if (state is WatchState)
                {
                    var st = (WatchState)state;
                    switch (st)
                    {
                        case WatchState.Watched:
                            watchedList.ForEach(x => ChannelItems.Add(x));
                            break;
                        case WatchState.Planned:
                            plannedList.ForEach(x => ChannelItems.Add(x));
                            break;
                    }
                }
                else if (state is SyncState)
                {
                    var st = (SyncState)state;
                    switch (st)
                    {
                        case SyncState.Added:
                            addedList.ForEach(x => ChannelItems.Add(x));
                            break;
                    }
                }    
            }
        }

        public async Task Search(HashSet<string> ids)
        {
            if (string.IsNullOrEmpty(SearchKey))
            {
                return;
            }

            switch (SelectedSite.Cred.Site)
            {
                case SiteType.YouTube:

                    List<VideoItemPOCO> lst = await YouTubeSite.SearchItemsAsync(SearchKey, SelectedCountry, 50);
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
                        foreach (IVideoItem item in lst.Select(poco => VideoItemFactory.CreateVideoItem(poco, SiteType.YouTube)))
                        {
                            AddNewItem(item);
                            item.IsHasLocalFileFound(DirPath);
                            if (ids.Contains(item.ParentID))
                            {
                                // подсветим видео, если канал уже есть в подписке
                                item.SyncState = SyncState.Added;
                            }
                        }
                    }
                    break;
            }
        }

        public void SiteChanged()
        {
            // TODO
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
        public bool IsHasNewFromSync { get; set; }
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

        public SiteType Site
        {
            get
            {
                return SiteType.NotSet;
            }
        }

        public string SubTitle { get; set; }
        public byte[] Thumbnail { get; set; }
        public string Title { get; set; }
        public bool UseFast { get; set; }

        public void AddNewItem(IVideoItem item)
        {
            if (item == null)
            {
                throw new ArgumentException("item");
            }

            if (ChannelItems.Select(x => x.ID).Contains(item.ID))
            {
                ChannelItems.Remove(ChannelItems.First(x => x.ID == item.ID));
            }

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

        public void DeleteItem(IVideoItem item)
        {
            if (item == null)
            {
                throw new ArgumentException("item");
            }

            ChannelItems.Remove(item);
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
                if (img != null)
                {
                    Thumbnail = StreamHelper.ReadFully(img);
                }
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
