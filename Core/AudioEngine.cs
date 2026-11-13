using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicVisualizer.Core
{
    public class AudioEngine : IDisposable
    {
        private readonly object _loadLock = new();
        private CancellationTokenSource _loadCts = new();

        private WaveStream? _waveStream;
        private WaveOutEvent? _waveOut;
        private VisualizationSampleProvider? _visualizerProvider;
        private float _volume = 0.5f;
        private bool _userStop;
        private bool _isDisposed;

        public event EventHandler? TrackEnded;
        public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;

        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
        public TimeSpan Position => _waveStream?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalTime => _waveStream?.TotalTime ?? TimeSpan.Zero;
        public VisualizationSampleProvider? VisualizerProvider => _visualizerProvider;

        /// <summary>
        /// Асинхронная загрузка: тяжёлое сканирование Mp3FileReader / MFReader уходит
        /// в Task.Run. Предыдущая незавершённая загрузка отменяется.
        /// </summary>
        public async Task LoadFileAsync(string filePath)
        {
            CancellationToken token;
            lock (_loadLock)
            {
                _loadCts.Cancel();
                _loadCts.Dispose();
                _loadCts = new CancellationTokenSource();
                token = _loadCts.Token;
            }

            Stop();
            DisposeWaveOut();
            _waveStream?.Dispose();
            _waveStream = null;

            WaveStream? stream = null;
            try
            {
                stream = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                    return ext switch
                    {
                        ".wav" => (WaveStream)new WaveFileReader(filePath),
                        ".mp3" => new Mp3FileReader(filePath),
                        ".aiff" or ".aif" => new AiffFileReader(filePath),
                        _ => new MediaFoundationReader(filePath)
                    };
                }, token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                stream?.Dispose();
                return;
            }

            _waveStream = stream;
            _visualizerProvider = new VisualizationSampleProvider(_waveStream.ToSampleProvider());
            _waveOut = new WaveOutEvent();
            _waveOut.PlaybackStopped += OnPlaybackStopped;
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
            if (_waveOut == null) return;
            _userStop = true;
            try { _waveOut.Stop(); }
            finally { _userStop = false; }
            if (_waveStream != null) _waveStream.CurrentTime = TimeSpan.Zero;
        }

        public void Unload()
        {
            Stop();
            DisposeWaveOut();
            _waveStream?.Dispose();
            _waveStream = null;
            _visualizerProvider = null;
        }

        public void SetVolume(float volume)
        {
            _volume = Math.Clamp(volume, 0f, 1f);
            if (_waveOut != null) _waveOut.Volume = _volume;
        }

        public void Seek(TimeSpan position)
        {
            if (_waveStream == null) return;
            var total = _waveStream.TotalTime;
            if (total.TotalSeconds <= 0) return;

            if (total - position < TimeSpan.FromSeconds(2))
            {
                _waveStream.CurrentTime = TimeSpan.Zero;
                return;
            }

            var clamped = position < TimeSpan.Zero ? TimeSpan.Zero : position;
            if (clamped > total) clamped = total;

            _waveStream.CurrentTime = clamped;

            var actual = _waveStream.CurrentTime;
            if ((actual - clamped).Duration() > TimeSpan.FromSeconds(2))
                _waveStream.CurrentTime = TimeSpan.Zero;
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (_isDisposed) return;

            if (e.Exception != null)
            {
                PlaybackError?.Invoke(this, new PlaybackErrorEventArgs(e.Exception));
                return;
            }

            if (_userStop) return;
            if (_waveStream == null) return;

            var remaining = _waveStream.TotalTime - _waveStream.CurrentTime;
            if (remaining.TotalSeconds > 0.5) return;

            TrackEnded?.Invoke(this, EventArgs.Empty);
        }

        private void DisposeWaveOut()
        {
            if (_waveOut == null) return;
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Dispose();
            _waveOut = null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _loadCts.Cancel();
            _loadCts.Dispose();
            Stop();
            DisposeWaveOut();
            _waveStream?.Dispose();
        }
    }
}