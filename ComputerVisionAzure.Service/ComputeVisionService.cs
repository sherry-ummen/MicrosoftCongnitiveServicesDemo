using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using FaceAPI = Microsoft.ProjectOxford.Face;
using VisionAPI = Microsoft.ProjectOxford.Vision;

namespace ComputerVisionAzure.Service {
    internal class ComputeVisionService {
        private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
        private FaceServiceClient _faceClient;
        private VisionServiceClient _visionClient;
        private VideoCapture _reader;
        private double _fps;
        private static readonly ImageEncodingParam[] g_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };

        private enum RecognitionMode {
            Faces,
            Emotions,
            EmotionsWithClientFaceDetect,
            Tags,
            Celebrities
        }

        public ComputeVisionService() {
            // Create local face detector. 
            _localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");
            _faceClient = new FaceServiceClient(MostSecuredKeys.FaceAPIKey, MostSecuredKeys.FaceAPIEndpoint);
            _visionClient = new VisionServiceClient(MostSecuredKeys.ComputerVisionAPIKey,
                MostSecuredKeys.ComputerVisionEndPoint);
            SetupCamera();
        }

        public void SetupCamera() {
            int cameraIndex = 0;
            double overrideFPS = 0;
            if (_reader != null && _reader.CaptureType == CaptureType.Camera) {
                return;
            }
            _reader = new VideoCapture(cameraIndex);
            _fps = overrideFPS;
            if (Math.Abs(_fps) < 0.001) {
                _fps = 30;
            }
            Width = _reader.FrameWidth;
            Height = _reader.FrameHeight;
        }

        public int Height { get; set; }

        public int Width { get; set; }

        public async Task<BitmapSource> Analyze(string type) {
            RecognitionMode mode;
            var result = Enum.TryParse(type, out mode);
            Result analysis = null;
            if (result) {
                switch (mode) {
                    case RecognitionMode.Faces: {
                            analysis = await FacesAnalysisFunction(GetCurrentFrame);
                            break;
                        }
                    case RecognitionMode.Emotions:
                        analysis = await EmotionAnalysisFunction(GetCurrentFrame);
                        break;
                    case RecognitionMode.Tags:
                        analysis = await TaggingAnalysisFunction(GetCurrentFrame);
                        break;
                    case RecognitionMode.Celebrities:
                        analysis = await CelebrityAnalysisFunction(GetCurrentFrame);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (analysis == null) return null;
            return VisualizeResult(analysis);
        }

        private VideoFrame GetCurrentFrame() {
            Mat image = new Mat();
            bool success = _reader.Read(image);
            if (!success) throw new Exception("Could not capture!");
            // Package the image for submission.
            VideoFrameMetadata meta;
            meta.Index = 0;
            meta.Timestamp = DateTime.Now;
            return new VideoFrame(image, meta);
        }

        private async Task<Result> FacesAnalysisFunction(Func<VideoFrame> videoFrame) {
            // Encode image.
            var frame = videoFrame();
            var jpg = frame.Image.ToMemoryStream(".jpg", g_jpegParams);
            // Submit image to API. 
            var attrs = new List<FaceAttributeType> {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Hair
            };
            var faces = await _faceClient.DetectAsync(jpg, returnFaceAttributes: attrs);
            return new Result() { Faces = faces, Frame = frame };
        }

        private async Task<Result> EmotionAnalysisFunction(Func<VideoFrame> videoFrame) {
            // Encode image. 
            var frame = videoFrame();
            var jpg = frame.Image.ToMemoryStream(".jpg", g_jpegParams);
            // Submit image to API. 
            FaceAPI.Contract.Face[] faces = null;

            // See if we have local face detections for this image.
            var localFaces = (OpenCvSharp.Rect[])frame.UserData;
            if (localFaces == null || localFaces.Count() > 0) {
                // If localFaces is null, we're not performing local face detection.
                // Use Cognigitve Services to do the face detection.
                faces = await _faceClient.DetectAsync(
                    jpg,
                    /* returnFaceId= */ false,
                    /* returnFaceLandmarks= */ false,
                    new FaceAttributeType[1] { FaceAttributeType.Emotion });
            } else {
                // Local face detection found no faces; don't call Cognitive Services.
                faces = new Face[0];
            }

            // Output. 
            return new Result() {
                Faces = faces.Select(e => CreateFace(e.FaceRectangle)).ToArray(),
                // Extract emotion scores from results. 
                EmotionScores = faces.Select(e => e.FaceAttributes.Emotion).ToArray(),
                Frame = frame
            };
        }

        private async Task<Result> TaggingAnalysisFunction(Func<VideoFrame> videoFrame) {
            // Encode image. 
            var frame = videoFrame();
            var jpg = frame.Image.ToMemoryStream(".jpg", g_jpegParams);
            // Submit image to API. 
            var analysis = await _visionClient.GetTagsAsync(jpg);
            // Count the API call. 
            // Output. 
            return new Result() { Tags = analysis.Tags, Frame = frame };
        }

        /// <summary> Function which submits a frame to the Computer Vision API for celebrity
        ///     detection. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the celebrities returned by the API. </returns>
        private async Task<Result> CelebrityAnalysisFunction(Func<VideoFrame> videoFrame) {
            // Encode image. 
            var frame = videoFrame();
            var jpg = frame.Image.ToMemoryStream(".jpg", g_jpegParams);
            // Submit image to API. 
            var result = await _visionClient.AnalyzeImageInDomainAsync(jpg, "celebrities");
            // Count the API call. 
            // Output. 
            var celebs = JsonConvert.DeserializeObject<CelebritiesResult>(result.Result.ToString()).Celebrities;
            return new Result() {
                // Extract face rectangles from results. 
                Faces = celebs.Select(c => CreateFace(c.FaceRectangle)).ToArray(),
                // Extract celebrity names from results. 
                CelebrityNames = celebs.Select(c => c.Name).ToArray(),
                Frame = frame
            };
        }

        private BitmapSource VisualizeResult(Result result) {
            // Draw any results on top of the image. 
            BitmapSource visImage = result.Frame.Image.ToBitmapSource();
            // See if we have local face detections for this image.
            var clientFaces = (OpenCvSharp.Rect[])result.Frame.UserData;
            if (clientFaces != null && result.Faces != null) {
                // If so, then the analysis results might be from an older frame. We need to match
                // the client-side face detections (computed on this frame) with the analysis
                // results (computed on the older frame) that we want to display. 
                MatchAndReplaceFaceRectangles(result.Faces, clientFaces);
            }

            visImage = Visualization.DrawFaces(visImage, result.Faces, result.EmotionScores, result.CelebrityNames);
            visImage = Visualization.DrawTags(visImage, result.Tags);

            return visImage;
        }

        private FaceAPI.Contract.Face CreateFace(FaceAPI.Contract.FaceRectangle rect) {
            return new FaceAPI.Contract.Face {
                FaceRectangle = new FaceAPI.Contract.FaceRectangle {
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = rect.Width,
                    Height = rect.Height
                }
            };
        }
        private FaceAPI.Contract.Face CreateFace(VisionAPI.Contract.FaceRectangle rect) {
            return new FaceAPI.Contract.Face {
                FaceRectangle = new FaceAPI.Contract.FaceRectangle {
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = rect.Width,
                    Height = rect.Height
                }
            };
        }

        private void MatchAndReplaceFaceRectangles(Face[] faces, OpenCvSharp.Rect[] clientRects) {
            // Use a simple heuristic for matching the client-side faces to the faces in the
            // results. Just sort both lists left-to-right, and assume a 1:1 correspondence. 

            // Sort the faces left-to-right. 
            var sortedResultFaces = faces
                .OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
                .ToArray();

            // Sort the clientRects left-to-right.
            var sortedClientRects = clientRects
                .OrderBy(r => r.Left + 0.5 * r.Width)
                .ToArray();

            // Assume that the sorted lists now corrrespond directly. We can simply update the
            // FaceRectangles in sortedResultFaces, because they refer to the same underlying
            // objects as the input "faces" array. 
            for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++) {
                // convert from OpenCvSharp rectangles
                OpenCvSharp.Rect r = sortedClientRects[i];
                sortedResultFaces[i].FaceRectangle = new FaceRectangle { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };
            }
        }
    }
}
