﻿using libVLCX;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VLC.Helpers.VideoLibrary;
using VLC.Model;
using VLC.Model.Music;
using VLC.Model.Stream;
using VLC.Model.Video;
using VLC.ViewModels;
using Windows.Storage;
using Windows.Storage.Search;

namespace VLC.Helpers
{
    public static class MediaLibraryHelper
    {
        public static async Task<IReadOnlyList<StorageFile>> GetSupportedFiles(StorageFolder folder)
        {
            IReadOnlyList<StorageFile> files = null;
            try
            {
                var queryOptions = new QueryOptions { FolderDepth = FolderDepth.Deep };
                foreach (var type in VLCFileExtensions.Supported)
                    queryOptions.FileTypeFilter.Add(type);
                var fileQueryResult = folder.CreateFileQueryWithOptions(queryOptions);
                files = await fileQueryResult.GetFilesAsync();
            }
            catch (Exception e)
            {
            }
            return files;
        }

        public static async Task<VideoItem> GetVideoItem(StorageFile file)
        {
            var media = await Locator.VLCService.GetMediaFromPath(file.Path);
            var video = await GetVideoItem(media, string.IsNullOrEmpty(file.DisplayName) ? file.Name : file.DisplayName, file.Path);
            return video;
        }

        public static async Task<VideoItem> GetVideoItem(Media media, string name, string path)
        {
            // get basic media properties
            var mP = new MediaProperties();
            mP = await Locator.VLCService.GetVideoProperties(mP, media);

            // use title decrapifier
            if (string.IsNullOrEmpty(mP?.ShowTitle))
                mP = TitleDecrapifier.tvShowEpisodeInfoFromString(mP, name);

            var video = new VideoItem(
                name,
                path,
                mP.Duration,
                mP.Width,
                mP.Height,
                mP.ShowTitle,
                mP.Season,
                mP.Episode
                );
            video.IsAvailable = true;

            return video;
        }

        public static async Task<StreamMedia> GetStreamItem(VLCStorageFile file)
        {
            var video = new StreamMedia();
            video.Name = file.Name;
            video.VlcMedia = file.Media;
            video.Path = file.Media.mrl();

            return video;
        }
    }
}
