﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using VLC_WINRT_APP.BackgroundAudioPlayer;
using VLC_WINRT_APP.BackgroundAudioPlayer.Model;
using VLC_WINRT_APP.Helpers;
using VLC_WINRT_APP.Model.Music;
using VLC_WINRT_APP.ViewModels;

namespace VLC_WINRT_APP.BackgroundHelpers
{
    public class BackgroundAudioHelper
    {
        private bool isMyBackgroundTaskRunning = false;
        DispatcherTimer dispatchTimer = new DispatcherTimer();

        public void InitBackgroundAudio()
        {
            App.Current.Suspending += ForegroundApp_Suspending;
            App.Current.Resuming += ForegroundApp_Resuming;
            ApplicationSettingsHelper.SaveSettingsValue(BackgroundAudioConstants.AppState, BackgroundAudioConstants.ForegroundAppActive);
            dispatchTimer.Interval = TimeSpan.FromSeconds(1);
            dispatchTimer.Tick += DispatchTimerOnTick;
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
            {
                if (!dispatchTimer.IsEnabled)
                {
                    App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Locator.MusicPlayerVM.IsPlaying = true;
                        dispatchTimer.Start();
                    });
                }
            }
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ||
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Paused)
            {
                Locator.MusicLibraryVM.ContinueIndexing = new TaskCompletionSource<bool>();
            }
            AddMediaPlayerEventHandlers();
        }

        /// <summary>
        /// Gets the information about background task is running or not by reading the setting saved by background task
        /// </summary>
        public bool IsMyBackgroundTaskRunning
        {
            get
            {
                if (isMyBackgroundTaskRunning)
                    return true;
                object value = ApplicationSettingsHelper.ReadSettingsValue(BackgroundAudioConstants.BackgroundTaskState);
                if (value == null)
                {
                    return false;
                }
                else
                {
                    isMyBackgroundTaskRunning = ((string)value).Equals(BackgroundAudioConstants.BackgroundTaskRunning);
                    return isMyBackgroundTaskRunning;
                }
            }
        }

        /// <summary>
        /// Unsubscribes to MediaPlayer events. Should run only on suspend
        /// </summary>
        private void RemoveMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= this.MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= this.BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        public void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaOpened += CurrentOnMediaOpened;
            BackgroundMediaPlayer.MessageReceivedFromBackground += this.BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        private void CurrentOnMediaOpened(MediaPlayer sender, object args)
        {
            Locator.MusicPlayerVM.UpdateTrackFromMF();
        }

        private void DispatchTimerOnTick(object sender, object o)
        {
            Locator.MusicPlayerVM.UpdateTimeFromMF();
        }

        /// <summary>
        /// MediaPlayer state changed event handlers. 
        /// Note that we can subscribe to events even if Media Player is playing media in background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Playing:
                    Debug.WriteLine("Media State Changed: Playing");
                    await App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Locator.MusicPlayerVM.IsPlaying = true;
                        dispatchTimer.Start();
                    });
                    break;
                case MediaPlayerState.Paused:
                    Debug.WriteLine("Media State Changed: Paused");
                    await App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Locator.MusicPlayerVM.IsPlaying = false;
                        dispatchTimer.Stop();
                    });
                    break;
            }
        }

        /// <summary>
        /// This event fired when a message is recieved from Background Process
        /// </summary>
        async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case BackgroundAudioConstants.PingBackground:
                        //When foreground app is active change track based on background message
                        Debug.WriteLine("Ping from background received");
                        break;
                    case BackgroundAudioConstants.Trackchanged:
                        //When foreground app is active change track based on background message
                        Debug.WriteLine("Track Changed" + (int)e.Data[key]);
                        break;
                    case BackgroundAudioConstants.BackgroundTaskStarted:
                        //Wait for Background Task to be initialized before starting playback
                        Debug.WriteLine("Background Task started");
                        break;
                    case BackgroundAudioConstants.BackgroundTaskCancelled:
                        await App.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            Locator.MusicPlayerVM.IsPlaying = false;
                        });
                        break;
                    case BackgroundAudioConstants.MFFailed:
                        Debug.WriteLine("VLC process is aware MF Background Media Player failed to open the file : " + e.Data[key]);
                        Locator.MusicPlayerVM.Play(true);
                        break;
                }
            }
        }

        /// <summary>
        /// Sends message to background informing app has resumed
        /// Subscribe to MediaPlayer events
        /// </summary>
        void ForegroundApp_Resuming(object sender, object e)
        {
            ApplicationSettingsHelper.SaveSettingsValue(BackgroundAudioConstants.AppState, BackgroundAudioConstants.ForegroundAppActive);

            // Verify if the task was running before
            if (IsMyBackgroundTaskRunning)
            {
                //if yes, reconnect to media play handlers
                AddMediaPlayerEventHandlers();

                //send message to background task that app is resumed, so it can start sending notifications
                ValueSet messageDictionary = new ValueSet();
                messageDictionary.Add(BackgroundAudioConstants.AppResumed, DateTime.Now.ToString());
                BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);

                Debug.WriteLine(BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing
                    ? "Resuming Media State Playing"
                    : "Resuming Media State Not Playing");
            }
            else
            {
                Debug.WriteLine("Resuming Media State Not Playing");
            }

        }

        /// <summary>
        /// Send message to Background process that app is to be suspended
        /// Stop clock and slider when suspending
        /// Unsubscribe handlers for MediaPlayer events
        /// </summary>
        void ForegroundApp_Suspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            ValueSet messageDictionary = new ValueSet();
            messageDictionary.Add(BackgroundAudioConstants.AppSuspended, DateTime.Now.ToString());
            BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
            RemoveMediaPlayerEventHandlers();
            ApplicationSettingsHelper.SaveSettingsValue(BackgroundAudioConstants.AppState, BackgroundAudioConstants.ForegroundAppSuspended);
            deferral.Complete();
        }

        public async Task RestorePlaylist()
        {
            if (IsMyBackgroundTaskRunning)
            {
                var msgDictionanary = new ValueSet();
                msgDictionanary.Add(BackgroundAudioConstants.RestorePlaylist, "");
                BackgroundMediaPlayer.SendMessageToBackground(msgDictionanary);
            }
        }

        public async Task AddToPlaylist(List<BackgroundTrackItem> trackItems)
        {
            if (IsMyBackgroundTaskRunning)
            {
                var msgDictionary = new ValueSet();
                string tracksString = AudioBackgroundInterface.SerializeAudioTracks(trackItems);
                msgDictionary.Add(BackgroundAudioConstants.AddTrack, tracksString);
                BackgroundMediaPlayer.SendMessageToBackground(msgDictionary);
            }
            await Task.Delay(500);
        }

        public async Task AddToPlaylist(BackgroundTrackItem trackItem)
        {
            var list = new List<BackgroundTrackItem> {trackItem};
            await AddToPlaylist(list);
        }

        public void PlayAudio(int trackIndex)
        {
            Debug.WriteLine("Play button pressed from App");
            if (IsMyBackgroundTaskRunning)
            {
                try
                {
                    ValueSet messageDictionary = new ValueSet();
                    string ts = trackIndex.ToString();
                    messageDictionary.Add(BackgroundAudioConstants.PlayTrack, ts);
                    BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        public async Task ResetCollection(string args = null)
        {
            if (IsMyBackgroundTaskRunning)
            {
                ValueSet messageDictionary = new ValueSet();
                messageDictionary.Add(BackgroundAudioConstants.ResetPlaylist, string.IsNullOrEmpty(args) ? "" : BackgroundAudioConstants.ShuffleReset);
                BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
                await Task.Delay(500);
            }
        }
    }
}
