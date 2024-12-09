using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace tutu2
{
    public sealed partial class VideoPlayerPage : Page
    {
        private static readonly Random random = new Random();
        private static readonly string[] videoFiles = { "ms-appx:///Assets/1.mp4", "ms-appx:///Assets/2.mp4", "ms-appx:///Assets/3.mp4", "ms-appx:///Assets/4.mp4" };

        public VideoPlayerPage()
        {
            this.InitializeComponent();
            PlayRandomVideo();
        }

        private void PlayRandomVideo()
        {
            string selectedVideo = videoFiles[random.Next(videoFiles.Length)];
            var mediaPlayer = new MediaPlayer
            {
                Source = MediaSource.CreateFromUri(new Uri(selectedVideo)),
                AutoPlay = true
            };
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}
