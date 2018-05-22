using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Vision.Contract;
using Face = Microsoft.ProjectOxford.Face.Contract.Face;

namespace ComputerVisionAzure.Service {
    internal class Result {
        public Microsoft.ProjectOxford.Face.Contract.Face[] Faces { get; set; } = null;
        public Microsoft.ProjectOxford.Common.Contract.EmotionScores[] EmotionScores { get; set; } = null;
        public string[] CelebrityNames { get; set; } = null;
        public Tag[] Tags { get; set; } = null;
        public VideoFrame Frame { get; set; }
    }
}
