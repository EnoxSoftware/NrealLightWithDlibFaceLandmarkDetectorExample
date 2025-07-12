using DlibFaceLandmarkDetector.UnityIntegration;
using NRKernal;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NrealLightWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// NrealLightWithDlibFaceLandmarkDetector Example
    /// </summary>
    public class NrealLightWithDlibFaceLandmarkDetectorExample : MonoBehaviour
    {
        public Text exampleTitle;
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        public enum DlibShapePredictorNamePreset : int
        {
            sp_human_face_68,
            sp_human_face_68_for_mobile,
            sp_human_face_17,
            sp_human_face_17_for_mobile,
            sp_human_face_6,
        }

        public Dropdown dlibShapePredictorNameDropdown;

        static DlibShapePredictorNamePreset dlibShapePredictorName = DlibShapePredictorNamePreset.sp_human_face_17_for_mobile;

        /// <summary>
        /// The name of dlib shape predictor file to use in the example scenes.
        /// </summary>
        public static string dlibShapePredictorFileName
        {
            get
            {
                return "DlibFaceLandmarkDetector/" + dlibShapePredictorName.ToString() + ".dat";
            }
        }

        // Use this for initialization
        protected void Start()
        {
            exampleTitle.text = "NrealLightWithDlibFaceLandmarkDetector Example " + Application.version;

            versionInfo.text = Core.NATIVE_LIBRARY_NAME + " " + OpenCVEnv.GetVersion() + " (" + Core.VERSION + ")";
            versionInfo.text += " / " + "dlibfacelandmarkdetector" + " " + DlibEnv.GetVersion();
            versionInfo.text += " / UnityEditor " + Application.unityVersion;
#if UNITY_ANDROID && !UNITY_EDITOR
            versionInfo.text += " / NRSDK " + NRVersionInfo.GetVersion();
#endif
            versionInfo.text += " / ";

#if UNITY_EDITOR
            versionInfo.text += "Editor";
#elif UNITY_STANDALONE_WIN
            versionInfo.text += "Windows";
#elif UNITY_STANDALONE_OSX
            versionInfo.text += "Mac OSX";
#elif UNITY_STANDALONE_LINUX
            versionInfo.text += "Linux";
#elif UNITY_ANDROID
            versionInfo.text += "Android";
#elif UNITY_IOS
            versionInfo.text += "iOS";
#elif UNITY_WSA
            versionInfo.text += "WSA";
#elif UNITY_WEBGL
            versionInfo.text += "WebGL";
#endif
            versionInfo.text += " ";
#if ENABLE_MONO
            versionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            versionInfo.text += ".NET";
#endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;

            dlibShapePredictorNameDropdown.value = (int)dlibShapePredictorName;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnScrollRectValueChanged()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }


        public void OnShowLicenseButtonClick()
        {
            SceneManager.LoadScene("ShowLicense");
        }

        public void OnNrealPhotoCaptureExampleButtonClick()
        {
            SceneManager.LoadScene("NrealPhotoCaptureExample");
        }

        public void OnNrealFaceLandmarkDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("NrealFaceLandmarkDetectionExample");
        }

        public void OnNrealARHeadExampleButtonClick()
        {
            SceneManager.LoadScene("NrealARHeadExample");
        }


        /// <summary>
        /// Raises the dlib shape predictor name dropdown value changed event.
        /// </summary>
        public void OnDlibShapePredictorNameDropdownValueChanged(int result)
        {
            dlibShapePredictorName = (DlibShapePredictorNamePreset)result;
        }
    }
}