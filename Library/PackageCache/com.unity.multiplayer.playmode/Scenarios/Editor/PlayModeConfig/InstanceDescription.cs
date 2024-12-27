using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    /// <summary>
    /// Base class for all instance descriptions.
    /// An instance description contains information about how to build and run an instance.
    /// </summary>
    [Serializable]
    abstract class InstanceDescription
    {
        public string Name;

        //[TODO]: we should remove this because one instance could have multiple nodes
        /// <summary>
        /// The nodeID that corresponds to this instance description.
        /// Useful to get the node for querying the status.
        /// </summary>
        [field: SerializeField]
        internal string CorrespondingNodeId { get; set; }

        /// <summary>
        /// The nodeIDs that correspond to this instance description.
        /// Useful to get the nodes for querying the status.
        /// </summary>
        [field: SerializeField, HideInInspector]
        private string[] m_Nodes;

        internal void SetCorrespondingNodes(params string[] nodeIds)
        {
            m_Nodes = nodeIds;
        }

        internal void SetCorrespondingNodes(params Node[] nodes)
        {
            SetCorrespondingNodes(nodes.Select(node => node.Name).ToArray());
        }

        internal ReadOnlyCollection<string> GetCorrespondingNodes()
        {
            m_Nodes ??= Array.Empty<string>();
            return Array.AsReadOnly(m_Nodes);
        }
    }

    interface IBuildableInstanceDescription
    {
        /// <summary>
        /// The build profile that this instance will be based on.
        /// </summary>
        BuildProfile BuildProfile { get; set; }
    }
}
