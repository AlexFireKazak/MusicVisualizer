using NAudio.Wave;
using System;

namespace MusicVisualizer.Core
{
    public class AudioEngine : IDisposable
    {
        private WaveStream? _waveStream;
        private WaveOutEvent? _waveOut;
        private VisualizationSampleProvider? _visualizerProvider;
        private float _volume = 0.5f;
        private bool _isDisposed;

        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
        public TimeSpan Position => _waveStream?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalTime => _waveStream?.TotalTime ?? TimeSpan.Zero;
        public VisualizationSampleProvider? VisualizerProvider => _visualizerProvider;

        public void LoadFile(string filePath)
        {
            Stop();
            DisposeWaveOut();
            _waveStream?.Dispose();

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            _waveStream = ext switch
            {
                ".wav" => new WaveFileReader(filePath),
                ".mp3" => new Mp3FileReader(filePath),
                ".aiff" or ".aif" => new AiffFileReader(filePath),
                _ => new MediaFoundationReader(filePath)
            };

            var sampleProvider = _waveStream.ToSampleProvider();
            _visualizerProvider = new VisualizationSampleProvider(sampleProvider);

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_visualizerProvider);
            _waveOut.Volume = _volume;
        }

        public void Play()
        {
            if (_waveOut != null && _waveOut.PlaybackState != PlaybackState.Playing)
                _waveOut.Play();
        }

        public void Pause()
        {
            if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
                _waveOut.Pause();
        }

        public void Stop()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                if (_waveStream != null)
                    _waveStream.CurrentTime = TimeSpan.Zero;
            }
        }

        public void SetVolume(float volume)
        {
            _volume = Math.Clamp(volume, 0f, 1f);
            if (_waveOut != null)
                _waveOut.Volume = _volume;
        }

        public void Seek(TimeSpan position)
        {
            if (_waveStream == null) return;
            var clamped = position < TimeSpan.Zero ? TimeSpan.Zero
                        : position > _waveStream.TotalTime ? _waveStream.TotalTime
                        : position;
            _waveStream.CurrentTime = clamped;
        }

        private void DisposeWaveOut()
        {
            _waveOut?.Dispose();
            _waveOut = null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Stop();
            DisposeWaveOut();
            _waveStream?.Dispose();
            _isDisposed = true;
        }
    }
}