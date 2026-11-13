using System;

namespace MusicVisualizer.Core
{
    public class PlaybackErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public PlaybackErrorEventArgs(Exception exception) => Exception = exception;
    }
}