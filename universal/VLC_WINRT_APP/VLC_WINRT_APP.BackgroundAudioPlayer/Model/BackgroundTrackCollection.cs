﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using VLC_WINRT_APP.Database.DataRepository;

namespace VLC_WINRT_APP.BackgroundAudioPlayer
{
    public sealed class BackgroundTrackCollection
    {
        private SystemMediaTransportControls systemmediatransportcontrol;
        private BackgroundTrackRepository _backgroundTrackRepository = new BackgroundTrackRepository();
        #region public properties
        public int Id { get; set; }

        public string Name { get; set; }

        public int CurrentTrack { get; set; }
        public BackgroundTrackItem CurrentTrackItem { get; set; }

        public int CurrentTrackIndex
        {
            get { return Playlist.IndexOf(CurrentTrackItem); }
        }

        public bool CanGoPrevious
        {
            get
            {
                return (CurrentTrack > 0);
            }
        }

        public bool CanGoNext
        {
            get { return (Playlist.Count != 1) && (CurrentTrack < Playlist.Count - 1); }
        }

        public bool IsRunning { get; set; }

        public bool IsShuffled { get; set; }
        #endregion
        #region public fields

        static List<BackgroundTrackItem> Playlist
        {
            get;
            set;
        }
        #endregion
        #region events
        /// <summary>
        /// Invoked when the media player is ready to move to next track
        /// </summary>
        public event TypedEventHandler<object, object> TrackChanged;
        #endregion
        #region private objects
        private MediaPlayer mediaPlayer;
        #endregion

        #region ctors
        public BackgroundTrackCollection()
        {
            Playlist = new List<BackgroundTrackItem>();
            mediaPlayer = BackgroundMediaPlayer.Current;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayerOnMediaEnded;
            mediaPlayer.CurrentStateChanged += MediaPlayerOnCurrentStateChanged;
            mediaPlayer.MediaFailed += MediaPlayerOnMediaFailed;
            ResetCollection(null);
        }

        private void MediaPlayerOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine("Failed to open the file with Background Media Player");
            if (CurrentTrackItem != null)
                BackgroundMediaPlayer.SendMessageToForeground(new ValueSet()
                {
                    new KeyValuePair<string, object>(BackgroundAudioConstants.MFFailed, CurrentTrackItem.Id)
                });
        }

        private void MediaPlayerOnCurrentStateChanged(MediaPlayer sender, object args)
        {

        }

        private void MediaPlayerOnMediaEnded(MediaPlayer sender, object args)
        {
            if (CanGoNext)
            {
                SkipToNext();
            }
        }

        /// <summary>
        /// Fired when MediaPlayer is ready to play the track
        /// </summary>
        void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // wait for media to be ready
            sender.Play();
            TrackChanged.Invoke(this, null);
        }
        #endregion

        #region methods

        public void ResetCollection(string args)
        {
            if (string.IsNullOrEmpty(args))
                CurrentTrack = -1;
            ClearPlaylist();
        }

        private void ClearPlaylist()
        {
            Playlist.Clear();
            ApplicationSettingsHelper.ReadResetSettingsValue("SavedPlaylist");
            Debug.WriteLine("Background audio : reset playlist");
            _backgroundTrackRepository.Clear();
        }
        public async void PopulatePlaylist()
        {
            Debug.WriteLine("Background audio : Populating playlist");
            var playlist = await _backgroundTrackRepository.LoadPlaylist();
            foreach (var item in playlist)
            {
                Playlist.Add(new BackgroundTrackItem(item.Id, item.AlbumId, item.ArtistId, item.ArtistName,
                    item.AlbumName, item.Name, item.Path));
            }
            Debug.WriteLine("Background audio : playlist tracks count : " + Playlist.Count);
        }

        public void SetActiveTrackProperty()
        {
            foreach (var BackgroundTrackItem in Playlist)
            {
                if (Playlist[CurrentTrack].Id == BackgroundTrackItem.Id)
                {
                    BackgroundTrackItem.IsCurrentPlaying = true;
                }
                else
                {
                    BackgroundTrackItem.IsCurrentPlaying = false;
                }
            }
        }

        public void SkipToPrevious()
        {
            if (!CanGoPrevious) return;
            CurrentTrack--;
            Play();
        }

        public void SkipToNext()
        {
            if (!CanGoNext) return;
            CurrentTrack++;
            Play();
        }

        public void PlayTrack(int trackId)
        {
            var trackCol = Playlist.FirstOrDefault(node => node.Id == trackId);
            if (trackCol == null) return;
            CurrentTrack = Playlist.IndexOf(trackCol);
            Play();
        }

        public async void Play()
        {
            IsRunning = true;
            CurrentTrackItem = Playlist[CurrentTrack];
            var file = await StorageFile.GetFileFromPathAsync(CurrentTrackItem.Path);
            mediaPlayer.SetFileSource(file);

            systemmediatransportcontrol = SystemMediaTransportControls.GetForCurrentView();
            systemmediatransportcontrol.IsNextEnabled = CanGoNext;
            systemmediatransportcontrol.IsPreviousEnabled = CanGoPrevious;
        }

        public void AddTracks(object trackItems)
        {
            if (!(trackItems is List<BackgroundTrackItem>)) return;
            var trackItemsList = trackItems as List<BackgroundTrackItem>;
            Debug.WriteLine("Background audio : adding " + trackItemsList.Count + " tracks to the playlist");
            foreach (var backgroundTrackItem in trackItemsList)
            {
                _backgroundTrackRepository.Add(new Database.Model.BackgroundTrackItem(backgroundTrackItem.Id, backgroundTrackItem.AlbumId, backgroundTrackItem.ArtistId, backgroundTrackItem.ArtistName, backgroundTrackItem.AlbumName, backgroundTrackItem.Name, backgroundTrackItem.Path, backgroundTrackItem.Index));
                Playlist.Add(backgroundTrackItem);
            }
        }
        #endregion
    }
}
