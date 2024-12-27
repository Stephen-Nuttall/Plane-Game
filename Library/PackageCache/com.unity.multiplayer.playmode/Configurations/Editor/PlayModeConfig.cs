using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor
{
    /// <summary>
    /// Base class for play mode config assets. This is used to configure the play mode behavior.
    /// </summary>
    internal abstract class PlayModeConfig : ScriptableObject
    {
        [SerializeField]
        string m_Description;

        /// <summary>
        /// Returns the description of the play mode config.
        /// </summary>
        public virtual string Description
        {
            get => m_Description;
            protected set => m_Description = value;
        }

        /// <summary>
        /// Returns true if the play mode config supports pause and step.
        /// </summary>
        public abstract bool SupportsPauseAndStep { get; }

        /// <summary>
        /// Implement this method to prepare and execute the play mode. The method will be called when the play mode is requested to start.
        /// </summary>
        /// <param name="cancellationToken">The cancelation token that can be used for detecting when the engine request for the task to be canceled.</param>
        /// <returns>Returns the asynchronous task.</returns>
        public abstract Task ExecuteStartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Implement this method to resume the execution of the play mode after a domain reload.
        /// </summary>
        /// <param name="cancellationToken">The cancelation token that can be used for detecting when the engine request for the task to be canceled.</param>
        public virtual void ExecuteResume(CancellationToken cancellationToken) { }

        /// <summary>
        /// Implement this method to exit the play mode. The method will be called when the play mode is requested to stop.
        /// </summary>
        public abstract void ExecuteStop();

        /// <summary>
        /// Implement this method to create additional UI elements for the play mode top bar when this play mode config is active.
        /// </summary>
        /// <returns>
        /// Returns the visual element that will be added to the top bar.
        /// </returns>
        public virtual VisualElement CreateTopbarUI() => null;

        /// <summary>
        /// Provide an icon to be shown in the playmode dropdown.
        /// </summary>
        /// <returns>
        /// Returns an icon that will show next to the configuration.
        /// </returns>
        public virtual Texture2D Icon => EditorGUIUtility.FindTexture("d_UnityLogo");

        public virtual bool IsConfigurationValid(out string reasonForInvalidConfiguration)
        {
            reasonForInvalidConfiguration = null;
            return true;
        }

    }
}
