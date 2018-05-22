using System;

namespace ComputerVisionAzure.Service {
    internal class ResultEventArgs : EventArgs {
        public ResultEventArgs(VideoFrame frame) {
            Frame = frame;
        }
        public VideoFrame Frame { get; }
        public Result Analysis { get; set; } = default(Result);
        public bool TimedOut { get; set; } = false;
        public Exception Exception { get; set; } = null;
    }
}