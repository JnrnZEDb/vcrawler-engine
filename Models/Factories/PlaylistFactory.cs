﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAPI.Database;
using DataAPI.POCO;
using DataAPI.Videos;
using Extensions;
using Interfaces.Enums;
using Interfaces.Models;
using Models.BO.Channels;
using Models.BO.Playlists;

namespace Models.Factories
{
    public static class PlaylistFactory
    {
        #region Static and Readonly Fields

        private static readonly SqLiteDatabase db = CommonFactory.CreateSqLiteDatabase();

        #endregion

        #region Static Methods

        public static IPlaylist CreatePlaylist(SiteType site)
        {
            switch (site)
            {
                case SiteType.YouTube:
                    return new YouPlaylist();
                default:
                    return null;
            }
        }

        public static IPlaylist CreatePlaylist(PlaylistPOCO poco, SiteType site)
        {
            if (poco == null)
            {
                return null;
            }
            switch (site)
            {
                case SiteType.YouTube:
                    var pl = new YouPlaylist
                    {
                        ID = poco.ID,
                        Title = poco.Title,
                        SubTitle = poco.SubTitle,
                        Thumbnail = poco.Thumbnail,
                        ChannelId = poco.ChannelID,
                        PlItems = poco.PlaylistItems,
                        IsDefault = false
                    };
                    return pl;

                default:
                    return null;
            }
        }

        public static IPlaylist CreateUploadPlaylist(IChannel ch, List<string> channel, byte[] thumbnail)
        {
            var pl = new YouPlaylist
            {
                Title = "Uploads",
                PlItems = channel,
                Thumbnail = thumbnail,
                ChannelId = ch.ID,
                ID = YouChannel.MakePlaylistUploadId(ch.ID),
                SubTitle = "All items",
                IsDefault = true
            };
            return pl;
        }

        public static async Task DownloadPlaylist(IPlaylist playlist,
            IChannel selectedChannel,
            string youPath,
            PlaylistMenuItem dtype)
        {
            switch (playlist.Site)
            {
                case SiteType.YouTube:

                    foreach (IVideoItem item in
                        selectedChannel.ChannelItems.Where(item => playlist.PlItems.Contains(item.ID))
                            .Where(item => item.FileState == ItemState.LocalNo))
                    {
                        item.FileState = ItemState.Planned;
                    }

                    foreach (IVideoItem item in selectedChannel.ChannelItems.Where(item => item.FileState == ItemState.Planned))
                    {
                        await item.DownloadItem(youPath, selectedChannel.DirPath, dtype).ConfigureAwait(false);
                    }

                    break;
            }
        }

        public static async Task UpdatePlaylist(IPlaylist playlist, IChannel selectedChannel)
        {
            switch (playlist.Site)
            {
                case SiteType.YouTube:

                    HashSet<string> dbids = selectedChannel.ChannelItems.Select(x => x.ID).ToHashSet();
                    List<string> plitemsIdsNet = await YouTubeSite.GetPlaylistItemsIdsListNetAsync(playlist.ID, 0).ConfigureAwait(false);
                    List<string> ids = plitemsIdsNet.Where(netid => !playlist.PlItems.Contains(netid)).ToList();
                    if (!ids.Any())
                    {
                        return;
                    }
                    var lstInDb = new List<string>();
                    var lstNoInDb = new List<string>();
                    foreach (string id in ids)
                    {
                        if (dbids.Contains(id))
                        {
                            lstInDb.Add(id);
                        }
                        else
                        {
                            lstNoInDb.Add(id);
                        }
                    }
                    foreach (string id in lstInDb)
                    {
                        await db.UpdatePlaylistAsync(playlist.ID, id, selectedChannel.ID).ConfigureAwait(false);
                        playlist.PlItems.Add(id);
                    }

                    IEnumerable<List<string>> chanks = lstNoInDb.SplitList();
                    foreach (List<string> list in chanks)
                    {
                        List<VideoItemPOCO> res = await YouTubeSite.GetVideosListByIdsAsync(list).ConfigureAwait(false); // получим скопом

                        foreach (IVideoItem vi in res.Select(poco => VideoItemFactory.CreateVideoItem(poco, SiteType.YouTube)))
                        {
                            vi.SyncState = SyncState.Added;
                            if (vi.ParentID == selectedChannel.ID)
                            {
                                selectedChannel.AddNewItem(vi);
                                await db.InsertItemAsync(vi).ConfigureAwait(false);
                                await db.UpdatePlaylistAsync(playlist.ID, vi.ID, selectedChannel.ID).ConfigureAwait(false);
                            }
                            else
                            {
                                selectedChannel.ChannelItems.Add(vi);
                            }
                            playlist.PlItems.Add(vi.ID);
                        }
                    }

                    break;
            }
        }

        #endregion
    }
}
