using NAudio.Wave;
using System;

namespace MusicVisualizer.Core
{
    public class VisualizationSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _waveformBuffer = new float[8192];
        private readonly float[] _fftInputBuffer = new float[1024];
        private readonly float[] _stereoLeft = new float[2048];
        private readonly float[] _stereoRight = new float[2048];
        private int _waveformIndex, _fftIndex, _stereoIndex;
        private readonly object _lock = new();

        public WaveFormat WaveFormat => _source.WaveFormat;
        public VisualizationSampleProvider(ISampleProvider source) => _source = source;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);
            if (samplesRead <= 0) return samplesRead;

            lock (_lock)
            {
                int channels = WaveFormat.Channels;
                int frames = samplesRead / channels;

                for (int f = 0; f < frames; f++)
                {
                    float mono;
                    if (channels == 1)
                    {
                        mono = buffer[offset + f];
                        _stereoLeft[_stereoIndex] = mono;
                        _stereoRight[_stereoIndex] = mono;
                    }
                    else
                    {
                        float sum = 0f;
                        for (int c = 0; c < channels; c++)
                            sum += buffer[offset + f * channels + c];
                        mono = sum / channels;
                        _stereoLeft[_stereoIndex] = buffer[offset + f * channels];
                        _stereoRight[_stereoIndex] = buffer[offset + f * channels + 1];
                    }
                    _stereoIndex = (_stereoIndex + 1) % _stereoLeft.Length;

                    _fftInputBuffer[_fftIndex] = mono;
                    _fftIndex = (_fftIndex + 1) % _fftInputBuffer.Length;

                    _waveformBuffer[_waveformIndex] = mono;
                    _waveformIndex = (_waveformIndex + 1) % _waveformBuffer.Length;
                }
            }

            return samplesRead;
        }

        public float[] GetLatestWaveformData() => Unwrap(_waveformBuffer, _waveformIndex);
        public float[] GetLatestFFTBuffer() => Unwrap(_fftInputBuffer, _fftIndex);

        public (float[] left, float[] right) GetLatestStereoData()
        {
            lock (_lock)
            {
                return (UnwrapLocked(_stereoLeft, _stereoIndex),
                        UnwrapLocked(_stereoRight, _stereoIndex));
            }
        }

        private float[] Unwrap(float[] buf, int idx)
        {
            lock (_lock) return UnwrapLocked(buf, idx);
        }

        private static float[] UnwrapLocked(float[] buf, int idx)
        {
            var result = new float[buf.Length];
            int firstChunk = buf.Length - idx;
            Array.Copy(buf, idx, result, 0, firstChunk);
            Array.Copy(buf, 0, result, firstChunk, idx);
            return result;
        }
    }
}