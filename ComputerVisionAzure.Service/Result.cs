using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Vision.Contract;
using Face = Microsoft.ProjectOxford.Face.Contract.Face;

namespace ComputerVisionAzure.Service {
    internal class Result {

        public Face[] Faces { get; set; } = null;
        public EmotionScores[] EmotionScores { get; set; } = null;
        public string[] CelebrityNames { get; set; } = null;
        public Tag[] Tags { get; set; } = null;

        public VideoFrame Frame { get; set; }
    }
}
