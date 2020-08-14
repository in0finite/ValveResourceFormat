using System;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace GUI.Controls
{
    internal partial class AudioPlaybackPanel : UserControl
    {
        private IWavePlayer waveOut;
        private WaveStream waveStream;
        private Action<float> setVolumeDelegate;

        public AudioPlaybackPanel(WaveStream inputStream)
        {
            Dock = DockStyle.Fill;

            InitializeComponent();

            waveStream = inputStream;

            try
            {
                waveOut = new WaveOutEvent();
                // TODO: This event does not actually fire.
                waveOut.PlaybackStopped += OnPlaybackStopped;
                waveOut.Init(CreateInputStream());
            }
            catch (Exception driverCreateException)
            {
                MessageBox.Show(driverCreateException.Message);
                return;
            }

            labelTotalTime.Text = waveStream.TotalTime.ToString("mm\\:ss\\.ff");
        }

        private void OnButtonPlayClick(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            setVolumeDelegate(volumeSlider1.Volume);
            waveOut.Play();
            playbackTimer.Enabled = true;
            UpdateTime();
        }

        private ISampleProvider CreateInputStream()
        {
            var sampleChannel = new SampleChannel(waveStream, true);
            sampleChannel.PreVolumeMeter += OnPreVolumeMeter;
            setVolumeDelegate = vol => sampleChannel.Volume = vol;
            var postVolumeMeter = new MeteringSampleProvider(sampleChannel);
            postVolumeMeter.StreamVolume += OnPostVolumeMeter;

            return postVolumeMeter;
        }

        void OnPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            waveformPainter1.AddMax(e.MaxSampleValues[0]);
            waveformPainter2.AddMax(e.MaxSampleValues[1]);
        }

        void OnPostVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            volumeMeter1.Amplitude = e.MaxSampleValues[0];
            volumeMeter2.Amplitude = e.MaxSampleValues[1];
        }

        void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show(e.Exception.Message, "Playback Device Error");
            }

            if (waveStream != null)
            {
                waveStream.Position = 0;
            }

            playbackTimer.Enabled = false;
            UpdateTime();
        }

        private void CloseWaveOut()
        {
            waveOut?.Stop();

            if (waveStream != null)
            {
                waveStream.Dispose();
                setVolumeDelegate = null;
                waveStream = null;
            }

            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
        }

        private void OnButtonPauseClick(object sender, EventArgs e)
        {
            if (waveOut?.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
            }

            playbackTimer.Enabled = false;
        }

        private void OnVolumeSliderChanged(object sender, EventArgs e)
        {
            setVolumeDelegate?.Invoke(volumeSlider1.Volume);
        }

        private void OnButtonStopClick(object sender, EventArgs e)
        {
            waveOut?.Stop();
            waveStream.Position = 0;
            playbackTimer.Enabled = false;
            UpdateTime();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        private void trackBarPosition_Scroll(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                waveStream.CurrentTime = TimeSpan.FromSeconds(waveStream.TotalTime.TotalSeconds * trackBarPosition.Value / 100.0);
                UpdateTime();
            }
        }

        private void UpdateTime()
        {
            var currentTime = waveStream.CurrentTime;
            trackBarPosition.Value = Math.Min(trackBarPosition.Maximum, (int)(100 * currentTime.TotalSeconds / waveStream.TotalTime.TotalSeconds));
            labelCurrentTime.Text = currentTime.ToString("mm\\:ss\\.ff");
        }
    }
}