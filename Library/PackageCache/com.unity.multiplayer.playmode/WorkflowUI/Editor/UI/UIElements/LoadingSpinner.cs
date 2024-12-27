using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Playmode.WorkflowUI.Editor
{
    [UxmlElement]
    internal partial class LoadingSpinner : VisualElement
    {

        const int k_RotationSpeed = 360; // Euler degrees per second

        readonly VisualElement m_Spinner;

        [UxmlAttribute]
        double m_LastRotationTime;
        [UxmlAttribute]
        int m_Rotation;
        [UxmlAttribute]
        bool m_Started;

        public LoadingSpinner()
        {
            m_Started = false;

            // add child elements to set up centered spinner rotation
            m_Spinner = new VisualElement
            {
                style =
                {
                    backgroundImage = Images.GetImage(Images.ImageName.Loading),
                },
            };
            m_Spinner.AddToClassList("innerSpinner");

            Add(m_Spinner);

            this.AddEventLifecycle(OnAttach, OnDetach);
        }

        void OnAttach(AttachToPanelEvent _)
        {
            if (m_Started)
            {
                EditorApplication.update += UpdateProgress;
            }
        }

        void OnDetach(DetachFromPanelEvent _)
        {
            if (m_Started)
            {
                EditorApplication.update -= UpdateProgress;
            }
        }

        internal void Start()
        {
            if (m_Started)
                return;

            m_Rotation = 0;
            m_LastRotationTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += UpdateProgress;

            m_Started = true;
        }

        internal void Stop()
        {
            if (!m_Started)
                return;

            EditorApplication.update -= UpdateProgress;

            m_Started = false;
        }

        void UpdateProgress()
        {
            if (m_Spinner.style.backgroundImage.value == null)
            {
                // Under some circumstances, like loading a new scene or during an assembly reload,
                // this visual element's background image can be reset to null.
                // Refresh it if this has occurred.
                m_Spinner.style.backgroundImage = Images.GetImage(Images.ImageName.Loading);
            }

            var currentTime = EditorApplication.timeSinceStartup;
            var deltaTime = currentTime - m_LastRotationTime;

#if UNITY_2021_2_OR_NEWER
            m_Spinner.transform.rotation = Quaternion.Euler(0, 0, m_Rotation);
#else
            transform.rotation = Quaternion.Euler(0, 0, mRotation);
#endif

            m_Rotation += (int)(k_RotationSpeed * deltaTime);
            m_Rotation = m_Rotation % 360;
            if (m_Rotation < 0)
            {
                m_Rotation += 360;
            }

            m_LastRotationTime = currentTime;
        }
    }
}
