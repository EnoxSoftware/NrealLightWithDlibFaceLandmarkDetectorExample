#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using DlibFaceLandmarkDetector;
using NrealLightWithOpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using NrealLightWithDlibFaceLandmarkDetectorExample.RectangleTrack;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;
using System.Threading;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using DlibFaceLandmarkDetector.UnityIntegration;

namespace NrealLightWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Nreal Face Landmark Detection Example
    /// An example of face landmark detection using OpenCVForUnity and DlibLandmarkDetector on Nreal Light.
    /// Referring to https://github.com/Itseez/opencv/blob/master/modules/objdetect/src/detection_based_tracker.cpp.
    /// </summary>
    [RequireComponent(typeof(NRCamTexture2MatHelper), typeof(ImageOptimizationHelper))]
    public class NrealFaceLandmarkDetectionExample : MonoBehaviour
    {

        /// <summary>
        /// Determines if enables the detection.
        /// </summary>
        public bool enableDetection = true;

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle enableDownScaleToggle;

        /// <summary>
        /// Determines if uses separate detection.
        /// </summary>
        public bool useSeparateDetection = false;

        /// <summary>
        /// The use separate detection toggle.
        /// </summary>
        public Toggle useSeparateDetectionToggle;

        /// <summary>
        /// Determines if use OpenCV FaceDetector for face detection.
        /// </summary>
        public bool useOpenCVDetector;

        /// <summary>
        /// The use OpenCV FaceDetector toggle.
        /// </summary>
        public Toggle useOpenCVDetectorToggle;

        /// <summary>
        /// Determines if displays camera image.
        /// </summary>
        public bool displayCameraImage = false;

        /// <summary>
        /// The display camera image toggle.
        /// </summary>
        public Toggle displayCameraImageToggle;

        /// <summary>
        /// Determines if displays detected face rect.
        /// </summary>
        public bool displayDetectedFaceRect = false;

        /// <summary>
        /// The is  display detected face rect toggle.
        /// </summary>
        public Toggle displayDetectedFaceRectToggle;

        /// <summary>
        /// The min detection size ratio.
        /// </summary>
        public float minDetectionSizeRatio = 0.07f;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        NRCamTexture2MatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The quad renderer.
        /// </summary>
        Renderer quad_renderer;

        /// <summary>
        /// The detection result.
        /// </summary>
        List<Rect> detectionResult = new List<Rect>();

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        Scalar COLOR_WHITE = new Scalar(255, 255, 255, 255);
        Scalar COLOR_GRAY = new Scalar(128, 128, 128, 255);

        Mat grayMat4Thread;
        CascadeClassifier cascade4Thread;
        FaceLandmarkDetector faceLandmarkDetector4Thread;
        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

        bool _isThreadRunning = false;
        bool isThreadRunning
        {
            get
            {
                lock (sync)
                    return _isThreadRunning;
            }
            set
            {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        RectangleTracker rectangleTracker;
        float coeffTrackingWindowSize = 2.0f;
        float coeffObjectSizeToTrack = 0.85f;
        List<Rect> detectedObjectsInRegions = new List<Rect>();
        List<Rect> resultObjects = new List<Rect>();
        List<List<Vector2>> resultFaceLandmarkPoints = new List<List<Vector2>>();

        bool _isDetecting = false;
        bool isDetecting
        {
            get
            {
                lock (sync)
                    return _isDetecting;
            }
            set
            {
                lock (sync)
                    _isDetecting = value;
            }
        }

        bool _hasUpdatedDetectionResult = false;
        bool hasUpdatedDetectionResult
        {
            get
            {
                lock (sync)
                    return _hasUpdatedDetectionResult;
            }
            set
            {
                lock (sync)
                    _hasUpdatedDetectionResult = value;
            }
        }

        /// <summary>
        /// the main camera.
        /// </summary>
        Camera mainCamera;

        /*
        // Use this for initialization
        protected void Start()
        {
            enableDownScaleToggle.isOn = enableDownScale;
            useSeparateDetectionToggle.isOn = useSeparateDetection;
            useOpenCVDetectorToggle.isOn = useOpenCVDetector;
            displayCameraImageToggle.isOn = displayCameraImage;
            displayDetectedFaceRectToggle.isOn = displayDetectedFaceRect;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTexture2MatHelper>();
            webCamTextureToMatHelper.OutputColorFormat = Source2MatHelperColorFormat.GRAY;
            webCamTextureToMatHelper.Initialize();

            rectangleTracker = new RectangleTracker();

            dlibShapePredictorFileName = NrealLightWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            dlibShapePredictorFilePath = DlibEnv.GetFilePath(dlibShapePredictorFileName);
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);


            dlibShapePredictorFilePath = DlibEnv.GetFilePath("DlibFaceLandmarkDetector/sp_human_face_6.dat");
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector4Thread = new FaceLandmarkDetector(dlibShapePredictorFilePath);
        }
        */

        /////
        string cascade_filepath;
        string cascade4Thread_filepath;
        string dlibShapePredictor_filepath;
        string dlibShapePredictor4Thread_filepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            enableDownScaleToggle.isOn = enableDownScale;
            useSeparateDetectionToggle.isOn = useSeparateDetection;
            useOpenCVDetectorToggle.isOn = useOpenCVDetector;
            displayCameraImageToggle.isOn = displayCameraImage;
            displayDetectedFaceRectToggle.isOn = displayDetectedFaceRect;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTexture2MatHelper>();

            rectangleTracker = new RectangleTracker();


            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            Debug.Log("Preparing file access...");

            cascade_filepath = await DlibEnv.GetFilePathTaskAsync("OpenCVForUnityExample/objdetect/lbpcascade_frontalface.xml", cancellationToken: cts.Token);
            cascade4Thread_filepath = await DlibEnv.GetFilePathTaskAsync("OpenCVForUnityExample/objdetect/haarcascade_frontalface_alt.xml", cancellationToken: cts.Token);
            dlibShapePredictorFileName = NrealLightWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            dlibShapePredictor_filepath = await DlibEnv.GetFilePathTaskAsync(dlibShapePredictorFileName, cancellationToken: cts.Token);
            dlibShapePredictor4Thread_filepath = await DlibEnv.GetFilePathTaskAsync("DlibFaceLandmarkDetector/sp_human_face_6.dat", cancellationToken: cts.Token);

            Debug.Log("Preparing file access complete!");

            Run();
        }

        // Use this for initialization
        void Run()
        {
            cascade = new CascadeClassifier();
            cascade.load(cascade_filepath);
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/OpenCVForUnityExample/objdetect/” to “Assets/StreamingAssets/OpenCVForUnityExample/objdetect/” folder. ");
            }

            cascade4Thread = new CascadeClassifier();
            cascade4Thread.load(cascade4Thread_filepath);
            if (cascade4Thread.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/OpenCVForUnityExample/objdetect/” to “Assets/StreamingAssets/OpenCVForUnityExample/objdetect/” folder. ");
            }

            if (string.IsNullOrEmpty(dlibShapePredictor_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictor_filepath);

            if (string.IsNullOrEmpty(dlibShapePredictor4Thread_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector4Thread = new FaceLandmarkDetector(dlibShapePredictor4Thread_filepath);

            webCamTextureToMatHelper.OutputColorFormat = Source2MatHelperColorFormat.GRAY;
            webCamTextureToMatHelper.Initialize();
        }
        //////

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat grayMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(grayMat.cols(), grayMat.rows(), TextureFormat.Alpha8, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));
            quad_renderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);

            //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

#if UNITY_ANDROID && !UNITY_EDITOR
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", webCamTextureToMatHelper.GetProjectionMatrix());
#else
            mainCamera = NRKernal.NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", mainCamera.projectionMatrix);
#endif


            /*
            cascade = new CascadeClassifier();
            cascade.load(OpenCVEnv.GetFilePath("OpenCVForUnityExample/objdetect/lbpcascade_frontalface.xml"));
#if !UNITY_WSA_10_0 || UNITY_EDITOR
            // "empty" method is not working on the UWP platform.
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/OpenCVForUnityExample/objdetect/” to “Assets/StreamingAssets/OpenCVForUnityExample/objdetect/” folder. ");
            }
#endif

            grayMat4Thread = new Mat();
            cascade4Thread = new CascadeClassifier();
            cascade4Thread.load(OpenCVEnv.GetFilePath("OpenCVForUnityExample/objdetect/haarcascade_frontalface_alt.xml"));
#if !UNITY_WSA_10_0 || UNITY_EDITOR
            // "empty" method is not working on the UWP platform.
            if (cascade4Thread.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/OpenCVForUnityExample/objdetect/” to “Assets/StreamingAssets/OpenCVForUnityExample/objdetect/” folder. ");
            }
#endif
            */

            ////
            grayMat4Thread = new Mat();
            ////
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            StopThread();
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Clear();
            }
            isDetecting = false;


            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            //if (cascade != null)
            //    cascade.Dispose();

            if (grayMat4Thread != null)
                grayMat4Thread.Dispose();

            //if (cascade4Thread != null)
            //    cascade4Thread.Dispose();

            rectangleTracker.Reset();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode + ":" + message);
        }

        // Update is called once per frame
        void Update()
        {
            lock (ExecuteOnMainThread)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat grayMat = webCamTextureToMatHelper.GetMat();

                Mat downScaleMat = null;
                float DOWNSCALE_RATIO;
                if (enableDownScale)
                {
                    downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
                    DOWNSCALE_RATIO = imageOptimizationHelper.DownscaleRatio;
                }
                else
                {
                    downScaleMat = grayMat;
                    DOWNSCALE_RATIO = 1.0f;
                }

                if (useOpenCVDetector)
                    Imgproc.equalizeHist(downScaleMat, downScaleMat);

                if (enableDetection && !isDetecting)
                {
                    isDetecting = true;

                    downScaleMat.copyTo(grayMat4Thread);

                    StartThread(ThreadWorker);
                }

                if (!useSeparateDetection)
                {

                    if (hasUpdatedDetectionResult)
                    {
                        hasUpdatedDetectionResult = false;

                        rectangleTracker.UpdateTrackedObjects(detectionResult);
                    }

                    rectangleTracker.GetObjects(resultObjects, true);

                    // set original size image
                    DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                    resultFaceLandmarkPoints.Clear();
                    foreach (Rect rect in resultObjects)
                    {

                        // restore to original size rect
                        rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                        rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                        rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                        rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));
                        resultFaceLandmarkPoints.Add(points);
                    }

                    if (!displayCameraImage)
                    {
                        // fill all black.
                        Imgproc.rectangle(grayMat, new Point(0, 0), new Point(grayMat.width(), grayMat.height()), new Scalar(0, 0, 0, 0), -1);
                    }

                    if (displayDetectedFaceRect)
                    {
                        // draw face rects
                        foreach (Rect rect in resultObjects)
                        {
                            DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);
                        }
                    }

                    // draw face landmark points
                    foreach (List<Vector2> points in resultFaceLandmarkPoints)
                    {
                        DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                    }
                }
                else
                {

                    Rect[] rectsWhereRegions;

                    if (hasUpdatedDetectionResult)
                    {
                        hasUpdatedDetectionResult = false;

                        //Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                        rectsWhereRegions = detectionResult.ToArray();
                    }
                    else
                    {
                        //Debug.Log("process: get rectsWhereRegions from previous positions");
                        if (useOpenCVDetector)
                        {
                            rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects();
                        }
                        else
                        {
                            rectsWhereRegions = rectangleTracker.CreateRawRects();
                        }
                    }

                    detectedObjectsInRegions.Clear();
                    foreach (Rect rect in rectsWhereRegions)
                    {
                        if (useOpenCVDetector)
                        {
                            DetectInRegion(downScaleMat, rect, detectedObjectsInRegions, cascade, true);
                        }
                        else
                        {
                            DetectInRegion(downScaleMat, rect, detectedObjectsInRegions, faceLandmarkDetector);
                        }
                    }

                    rectangleTracker.UpdateTrackedObjects(detectedObjectsInRegions);
                    rectangleTracker.GetObjects(resultObjects, false);

                    // set original size image
                    DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                    resultFaceLandmarkPoints.Clear();
                    foreach (Rect rect in resultObjects)
                    {

                        // restore to original size rect
                        rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                        rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                        rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                        rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));
                        resultFaceLandmarkPoints.Add(points);
                    }

                    if (!displayCameraImage)
                    {
                        // fill all black.
                        Imgproc.rectangle(grayMat, new Point(0, 0), new Point(grayMat.width(), grayMat.height()), new Scalar(0, 0, 0, 0), -1);
                    }

                    if (displayDetectedFaceRect)
                    {
                        // draw previous rects
                        DrawDownScaleFaceRects(grayMat, rectsWhereRegions, DOWNSCALE_RATIO, COLOR_GRAY, 1);

                        // draw face rects
                        foreach (Rect rect in resultObjects)
                        {
                            DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);
                        }
                    }

                    // draw face landmark points
                    foreach (List<Vector2> points in resultFaceLandmarkPoints)
                    {
                        DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                    }
                }

                OpenCVMatUtils.MatToTexture2D(grayMat, texture);
            }

            if (webCamTextureToMatHelper.IsPlaying())
            {

#if UNITY_ANDROID && !UNITY_EDITOR
                Matrix4x4 cameraToWorldMatrix = webCamTextureToMatHelper.GetCameraToWorldMatrix();
#else
                Matrix4x4 cameraToWorldMatrix = mainCamera.cameraToWorldMatrix;
#endif

                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                /*
                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                position *= 1.5f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
                */

                //
                // Adjusting the position and scale of the display screen
                // to counteract the phenomenon of texture margins (transparent areas in MR space) being displayed as black when recording video using NRVideoCapture.
                //
                // Position the canvas object slightly in front
                // of the real world web camera.
                float overlayDistance = 1.5f;
                Vector3 ccCameraSpacePos = UnProjectVector(webCamTextureToMatHelper.GetProjectionMatrix(), new Vector3(0.0f, 0.0f, overlayDistance));
                Vector3 tlCameraSpacePos = UnProjectVector(webCamTextureToMatHelper.GetProjectionMatrix(), new Vector3(-overlayDistance, overlayDistance, overlayDistance));

                //position
                Vector3 position = cameraToWorldMatrix.MultiplyPoint3x4(ccCameraSpacePos);
                gameObject.transform.position = position;

                //scale
                Vector3 scale = new Vector3(Mathf.Abs(tlCameraSpacePos.x - ccCameraSpacePos.x) * 2, Mathf.Abs(tlCameraSpacePos.y - ccCameraSpacePos.y) * 2, 1);
                gameObject.transform.localScale = scale;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
                gameObject.transform.rotation = rotation;
                //
            }
        }

        //
        private Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
        {
            Vector3 from = new Vector3(0, 0, 0);
            var axsX = proj.GetRow(0);
            var axsY = proj.GetRow(1);
            var axsZ = proj.GetRow(2);
            from.z = to.z / axsZ.z;
            from.y = (to.y - (from.z * axsY.z)) / axsY.y;
            from.x = (to.x - (from.z * axsX.z)) / axsX.x;
            return from;
        }
        //

        private void StartThread(Action action)
        {
#if WINDOWS_UWP || (!UNITY_WSA_10_0 && (NET_4_6 || NET_STANDARD_2_0))
            System.Threading.Tasks.Task.Run(() => action());
#else
            System.Threading.ThreadPool.QueueUserWorkItem(_ => action());
#endif
        }

        private void StopThread()
        {
            if (!isThreadRunning)
                return;

            while (isThreadRunning)
            {
                //Wait threading stop
            }
        }

        private void ThreadWorker()
        {
            isThreadRunning = true;

            if (useOpenCVDetector)
            {
                DetectObject(grayMat4Thread, out detectionResult, cascade4Thread, true);
            }
            else
            {
                DetectObject(grayMat4Thread, out detectionResult, faceLandmarkDetector4Thread);
            }

            lock (ExecuteOnMainThread)
            {
                if (ExecuteOnMainThread.Count == 0)
                {
                    ExecuteOnMainThread.Enqueue(() =>
                    {
                        OnDetectionDone();
                    });
                }
            }

            isThreadRunning = false;
        }

        private void DetectObject(Mat img, out List<Rect> detectedObjects, FaceLandmarkDetector landmarkDetector)
        {
            DlibOpenCVUtils.SetImage(landmarkDetector, img);

            List<UnityEngine.Rect> detectResult = landmarkDetector.Detect();

            detectedObjects = new List<Rect>();

            int len = detectResult.Count;
            for (int i = 0; i < len; i++)
            {
                UnityEngine.Rect r = detectResult[i];
                detectedObjects.Add(new Rect((int)r.x, (int)r.y, (int)r.width, (int)r.height));
            }
        }

        private void DetectObject(Mat img, out List<Rect> detectedObjects, CascadeClassifier cascade, bool correctToDlibResult = false)
        {
            int d = Mathf.Min(img.width(), img.height());
            d = (int)Mathf.Round(d * minDetectionSizeRatio);

            MatOfRect objects = new MatOfRect();
            if (cascade != null)
                cascade.detectMultiScale(img, objects, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, new Size(d, d), new Size());

            detectedObjects = objects.toList();

            if (correctToDlibResult)
            {
                int len = detectedObjects.Count;
                for (int i = 0; i < len; i++)
                {
                    Rect r = detectedObjects[i];
                    // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                    r.x += (int)(r.width * 0.05f);
                    r.y += (int)(r.height * 0.1f);
                    r.width = (int)(r.width * 0.9f);
                    r.height = (int)(r.height * 0.9f);
                }
            }
        }

        private void OnDetectionDone()
        {
            hasUpdatedDetectionResult = true;

            isDetecting = false;
        }

        private void DetectInRegion(Mat img, Rect region, List<Rect> detectedObjectsInRegions, FaceLandmarkDetector landmarkDetector)
        {
            Rect r0 = new Rect(new Point(), img.size());
            Rect r1 = new Rect(region.x, region.y, region.width, region.height);
            Rect.inflate(r1, (int)((r1.width * coeffTrackingWindowSize) - r1.width) / 2,
                (int)((r1.height * coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if ((r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("detectInRegion: Empty intersection");
                return;
            }

            using (Mat img1_roi = new Mat(img, r1))
            using (Mat img1 = new Mat(r1.size(), img.type()))
            {
                img1_roi.copyTo(img1);

                DlibOpenCVUtils.SetImage(landmarkDetector, img1);

                List<UnityEngine.Rect> detectResult = landmarkDetector.Detect();

                int len = detectResult.Count;
                for (int i = 0; i < len; i++)
                {
                    UnityEngine.Rect tmp = detectResult[i];
                    Rect r = new Rect((int)(tmp.x + r1.x), (int)(tmp.y + r1.y), (int)tmp.width, (int)tmp.height);
                    detectedObjectsInRegions.Add(r);
                }
            }
        }

        private void DetectInRegion(Mat img, Rect region, List<Rect> detectedObjectsInRegions, CascadeClassifier cascade, bool correctToDlibResult = false)
        {
            Rect r0 = new Rect(new Point(), img.size());
            Rect r1 = new Rect(region.x, region.y, region.width, region.height);
            Rect.inflate(r1, (int)((r1.width * coeffTrackingWindowSize) - r1.width) / 2,
                (int)((r1.height * coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if ((r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("detectInRegion: Empty intersection");
                return;
            }

            int d = Math.Min(region.width, region.height);
            d = (int)Math.Round(d * coeffObjectSizeToTrack);

            using (MatOfRect tmpobjects = new MatOfRect())
            using (Mat img1 = new Mat(img, r1)) //subimage for rectangle -- without data copying
            {
                cascade.detectMultiScale(img1, tmpobjects, 1.1, 2, 0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE | Objdetect.CASCADE_FIND_BIGGEST_OBJECT, new Size(d, d), new Size());

                Rect[] tmpobjectsArray = tmpobjects.toArray();
                int len = tmpobjectsArray.Length;
                for (int i = 0; i < len; i++)
                {
                    Rect tmp = tmpobjectsArray[i];
                    Rect r = new Rect(new Point(tmp.x + r1.x, tmp.y + r1.y), tmp.size());

                    if (correctToDlibResult)
                    {
                        // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                        r.x += (int)(r.width * 0.05f);
                        r.y += (int)(r.height * 0.1f);
                        r.width = (int)(r.width * 0.9f);
                        r.height = (int)(r.height * 0.9f);
                    }

                    detectedObjectsInRegions.Add(r);
                }
            }
        }

        private void DrawDownScaleFaceRects(Mat img, Rect[] rects, float downscaleRatio, Scalar color, int thickness)
        {
            int len = rects.Length;
            for (int i = 0; i < len; i++)
            {
                Rect rect = new Rect(
                    (int)(rects[i].x * downscaleRatio),
                    (int)(rects[i].y * downscaleRatio),
                    (int)(rects[i].width * downscaleRatio),
                    (int)(rects[i].height * downscaleRatio)
                );
                Imgproc.rectangle(img, rect, color, thickness);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
            imageOptimizationHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (faceLandmarkDetector4Thread != null)
                faceLandmarkDetector4Thread.Dispose();

            if (rectangleTracker != null)
                rectangleTracker.Dispose();

            if (cascade != null)
                cascade.Dispose();

            if (cascade4Thread != null)
                cascade4Thread.Dispose();

            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NrealLightWithDlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.RequestedIsFrontFacing = !webCamTextureToMatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            enableDownScale = enableDownScaleToggle.isOn;

            if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
            {
                webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the use separate detection toggle value changed event.
        /// </summary>
        public void OnUseSeparateDetectionToggleValueChanged()
        {
            useSeparateDetection = useSeparateDetectionToggle.isOn;

            if (rectangleTracker != null)
            {
                lock (rectangleTracker)
                {
                    rectangleTracker.Reset();
                }
            }
        }

        /// <summary>
        /// Raises the use OpenCV Detector toggle value changed event.
        /// </summary>
        public void OnUseOpenCVDetectorToggleValueChanged()
        {
            useOpenCVDetector = useOpenCVDetectorToggle.isOn;
        }

        /// <summary>
        /// Raises the display camera image toggle value changed event.
        /// </summary>
        public void OnDisplayCameraImageToggleValueChanged()
        {
            displayCameraImage = displayCameraImageToggle.isOn;
        }

        /// <summary>
        /// Raises the display detected face rect toggle value changed event.
        /// </summary>
        public void OnDisplayDetectedFaceRectToggleValueChanged()
        {
            displayDetectedFaceRect = displayDetectedFaceRectToggle.isOn;
        }
    }
}

#endif