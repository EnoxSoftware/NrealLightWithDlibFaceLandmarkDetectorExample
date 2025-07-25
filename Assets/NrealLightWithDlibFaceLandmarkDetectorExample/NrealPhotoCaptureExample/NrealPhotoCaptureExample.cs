using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using NRKernal;
using NRKernal.NRExamples;
using NRKernal.Record;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NrealLightWithDlibFaceLandmarkDetectorExample
{

#if UNITY_ANDROID && !UNITY_EDITOR
    using GalleryDataProvider = NativeGalleryDataProvider;
#else
    using GalleryDataProvider = MockGalleryDataProvider;
#endif

    /// <summary>
    /// Nreal PhotoCapture Example
    /// An example of holographic photo blending using the NRPhotocCapture class on Nreal Light.
    /// </summary>
    public class NrealPhotoCaptureExample : MonoBehaviour
    {
        /// <summary> The photo capture object. </summary>
        private NRPhotoCapture m_PhotoCaptureObject;
        /// <summary> The camera resolution. </summary>
        private Resolution m_CameraResolution;
        private bool isOnPhotoProcess = false;
        GalleryDataProvider galleryDataTool;


        GameObject m_Canvas = null;
        Renderer m_CanvasRenderer = null;
        Texture2D m_Texture = null;

        /// <summary>
        /// Determines if enable BlendMode Blend.
        /// </summary>
        public bool isBlendModeBlend;

        /// <summary>
        /// The is BlendMode Blend toggle.
        /// </summary>
        public Toggle isBlendModeBlendToggle;

        /// <summary>
        /// Determines if save texture to gallery.
        /// </summary>
        public bool saveTextureToGallery;

        /// <summary>
        /// The save texture to gallery toggle.
        /// </summary>
        public Toggle saveTextureToGalleryToggle;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName;// = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        //string dlibShapePredictorFilePath;

        /*
        protected void Start()
        {
            dlibShapePredictorFileName = NrealLightWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;

            isBlendModeBlendToggle.isOn = isBlendModeBlend;
            saveTextureToGalleryToggle.isOn = saveTextureToGallery;

            m_Canvas = GameObject.Find("PhotoCaptureCanvas");
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
            m_CanvasRenderer.enabled = false;
        }
        */

        ////
        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            Debug.Log("Preparing file access...");

            dlibShapePredictorFileName = NrealLightWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            string dlibShapePredictor_filepath = await DlibEnv.GetFilePathTaskAsync(dlibShapePredictorFileName, cancellationToken: cts.Token);

            Debug.Log("Preparing file access complete!");

            if (string.IsNullOrEmpty(dlibShapePredictor_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictor_filepath);


            isBlendModeBlendToggle.isOn = isBlendModeBlend;
            saveTextureToGalleryToggle.isOn = saveTextureToGallery;

            m_Canvas = GameObject.Find("PhotoCaptureCanvas");
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
            m_CanvasRenderer.enabled = false;
        }
        //////

        /// <summary> Use this for initialization. </summary>
        void Create(Action<NRPhotoCapture> onCreated)
        {
            if (m_PhotoCaptureObject != null)
            {
                NRDebugger.Info("The NRPhotoCapture has already been created.");
                return;
            }

            // Create a PhotoCapture object
            NRPhotoCapture.CreateAsync(false, delegate (NRPhotoCapture captureObject)
            {
                m_CameraResolution = NRPhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

                if (captureObject == null)
                {
                    NRDebugger.Error("Can not get a captureObject.");
                    return;
                }

                m_PhotoCaptureObject = captureObject;

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.cameraResolutionWidth = m_CameraResolution.width;
                cameraParameters.cameraResolutionHeight = m_CameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
                cameraParameters.frameRate = NativeConstants.RECORD_FPS_DEFAULT;
                cameraParameters.blendMode = (isBlendModeBlend) ? BlendMode.Blend : BlendMode.RGBOnly;

                // Activate the camera
                m_PhotoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (NRPhotoCapture.PhotoCaptureResult result)
                {
                    NRDebugger.Info("Start PhotoMode Async");
                    if (result.success)
                    {
                        onCreated?.Invoke(m_PhotoCaptureObject);
                    }
                    else
                    {
                        isOnPhotoProcess = false;
                        this.Close();
                        NRDebugger.Error("Start PhotoMode faild." + result.resultType);
                    }
                }, true);
            });
        }

        /// <summary> Take a photo. </summary>
        void TakeAPhoto()
        {
            if (isOnPhotoProcess)
            {
                NRDebugger.Warning("Currently in the process of taking pictures, Can not take photo .");
                return;
            }

            isOnPhotoProcess = true;
            if (m_PhotoCaptureObject == null)
            {
                this.Create((capture) =>
                {
                    capture.TakePhotoAsync(OnCapturedPhotoToMemory);
                });
            }
            else
            {
                m_PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
        }

        /// <summary> Executes the 'captured photo memory' action. </summary>
        /// <param name="result">            The result.</param>
        /// <param name="photoCaptureFrame"> The photo capture frame.</param>
        void OnCapturedPhotoToMemory(NRPhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Debug.Log("photoCaptureFrame.pixelFormat " + photoCaptureFrame.pixelFormat);
            Debug.Log("photoCaptureFrame.hasLocationData " + photoCaptureFrame.hasLocationData);
            Debug.Log("photoCaptureFrame.dataLength " + photoCaptureFrame.dataLength);

            Matrix4x4 cameraToWorldMatrix;
            Matrix4x4 projectionMatrix;

            if (photoCaptureFrame.hasLocationData)
            {
                photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
                photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);
            }
            else
            {
                Camera mainCamera = NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;
                cameraToWorldMatrix = mainCamera.cameraToWorldMatrix;

                bool _result;
                EyeProjectMatrixData pm = NRFrame.GetEyeProjectMatrix(out _result, 0.3f, 1000f);
                projectionMatrix = pm.RGBEyeMatrix;
            }

            Debug.Log("cameraToWorldMatrix:\n" + cameraToWorldMatrix.ToString());
            Debug.Log("projectionMatrix:\n" + projectionMatrix.ToString());

            Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;


            if (m_Texture == null)
            {
                m_Texture = new Texture2D(m_CameraResolution.width, m_CameraResolution.height, TextureFormat.RGB24, false);
                rgbMat = new Mat(m_Texture.height, m_Texture.width, CvType.CV_8UC3);

                //dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
                //if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
                //{
                //    Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
                //}
                //faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);
            }

            // Copy the raw image data into our target texture
            photoCaptureFrame.UploadImageDataToTexture(m_Texture);


            OpenCVMatUtils.Texture2DToMat(m_Texture, rgbMat);

            DlibOpenCVUtils.SetImage(faceLandmarkDetector, rgbMat);

            //detect face
            List<FaceLandmarkDetector.RectDetection> detectResult = faceLandmarkDetector.DetectRectDetection();

            // fill all black.
            //Imgproc.rectangle (rgbMat, new Point (0, 0), new Point (rgbMat.width (), rgbMat.height ()), new Scalar (0, 0, 0, 0), -1);
            // draw an edge lines.
            Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rgbMat.width(), rgbMat.height()), new Scalar(255, 0, 0, 255), 2);
            // draw a diagonal line.
            //Imgproc.line (rgbMat, new Point (0, 0), new Point (rgbMat.cols (), rgbMat.rows ()), new Scalar (255, 0, 0, 255));

            foreach (var r in detectResult)
            {
                Debug.Log("rect : " + r.rect);

                //detect landmark points
                List<Vector2> points = faceLandmarkDetector.DetectLandmark(r.rect);

                Debug.Log("face points count : " + points.Count);
                //draw landmark points
                DlibOpenCVUtils.DrawFaceLandmark(rgbMat, points, new Scalar(0, 255, 0, 255), 2);

                //draw face rect
                DlibOpenCVUtils.DrawFaceRect(rgbMat, r.rect, new Scalar(255, 0, 0, 255), 2);
            }

            Imgproc.putText(rgbMat, "W:" + rgbMat.width() + " H:" + rgbMat.height() + " SO:" + Screen.orientation, new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.5, new Scalar(0, 255, 0, 255), 2, Imgproc.LINE_AA, false);

            OpenCVMatUtils.MatToTexture2D(rgbMat, m_Texture);



            m_Texture.wrapMode = TextureWrapMode.Clamp;

            m_CanvasRenderer.enabled = true;
            m_CanvasRenderer.sharedMaterial.SetTexture("_MainTex", m_Texture);
            m_CanvasRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
            m_CanvasRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
            m_CanvasRenderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));
            m_CanvasRenderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);


            /*
            // Position the canvas object slightly in front
            // of the real world web camera.
            Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
            position *= 1.5f;

            // Rotate the canvas object so that it faces the user.
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

            m_Canvas.transform.position = position;
            m_Canvas.transform.rotation = rotation;
            */

            // Adjusting the position and scale of the display screen
            // to counteract the phenomenon of texture margins (transparent areas in MR space) being displayed as black when recording video using NRVideoCapture.
            //
            // Position the canvas object slightly in front
            // of the real world web camera.
            float overlayDistance = 1.5f;
            Vector3 ccCameraSpacePos = UnProjectVector(projectionMatrix, new Vector3(0.0f, 0.0f, overlayDistance));
            Vector3 tlCameraSpacePos = UnProjectVector(projectionMatrix, new Vector3(-overlayDistance, overlayDistance, overlayDistance));

            //position
            Vector3 position = cameraToWorldMatrix.MultiplyPoint3x4(ccCameraSpacePos);
            m_Canvas.transform.position = position;

            //scale
            Vector3 scale = new Vector3(Mathf.Abs(tlCameraSpacePos.x - ccCameraSpacePos.x) * 2, Mathf.Abs(tlCameraSpacePos.y - ccCameraSpacePos.y) * 2, 1);
            m_Canvas.transform.localScale = scale;

            // Rotate the canvas object so that it faces the user.
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
            m_Canvas.transform.rotation = rotation;
            //



            Debug.Log("Took picture!");

            if (saveTextureToGallery)
                SaveTextureToGallery(m_Texture);

            // Release camera resource after capture the photo.
            this.Close();
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

        /// <summary> Closes this object. </summary>
        void Close()
        {
            if (m_PhotoCaptureObject == null)
            {
                NRDebugger.Error("The NRPhotoCapture has not been created.");
                return;
            }
            // Deactivate our camera
            m_PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        /// <summary> Executes the 'stopped photo mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedPhotoMode(NRPhotoCapture.PhotoCaptureResult result)
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
            isOnPhotoProcess = false;
        }

        /// <summary> Executes the 'destroy' action. </summary>
        void OnDestroy()
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;


            if (m_Texture != null)
            {
                Texture2D.Destroy(m_Texture);
                m_Texture = null;
            }

            if (rgbMat != null)
                rgbMat.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (cts != null)
                cts.Dispose();
        }


        public void SaveTextureToGallery(Texture2D _texture)
        {
            try
            {
                string filename = string.Format("Nreal_Shot_{0}.png", NRTools.GetTimeStamp().ToString());
                byte[] _bytes = _texture.EncodeToPNG();
                NRDebugger.Info(_bytes.Length / 1024 + "Kb was saved as: " + filename);
                if (galleryDataTool == null)
                {
                    galleryDataTool = new GalleryDataProvider();
                }

                galleryDataTool.InsertImage(_bytes, filename, "Screenshots");
            }
            catch (Exception e)
            {
                NRDebugger.Error("[TakePicture] Save picture faild!");
                throw e;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NrealLightWithDlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the take photo button click event.
        /// </summary>
        public void OnTakePhotoButtonClick()
        {
            TakeAPhoto();
        }

        /// <summary>
        /// Raises the is BlendMode Blend toggle value changed event.
        /// </summary>
        public void OnIsBlendModeBlendToggleValueChanged()
        {
            isBlendModeBlend = isBlendModeBlendToggle.isOn;

            if (m_PhotoCaptureObject != null)
                this.Close();
        }

        /// <summary>
        /// Raises the save texture to gallery toggle value changed event.
        /// </summary>
        public void OnSaveTextureToGalleryToggleValueChanged()
        {
            saveTextureToGallery = saveTextureToGalleryToggle.isOn;
        }
    }
}