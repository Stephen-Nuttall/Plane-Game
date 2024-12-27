using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor.Bridge
{
    class ContainerWindowProxy
    {
        readonly ContainerWindow m_BackingObject;

        public ContainerWindowProxy(ContainerWindow backingObject)
        {
            m_BackingObject = backingObject;
            // Doing after the cast so that we cover both NULL and UnityNULL
            Debug.Assert(m_BackingObject != null);
        }

        public void Close() => m_BackingObject.Close();
        public int GetInstanceID() => m_BackingObject.GetInstanceID();

        public static void SetMppmCanCloseCallback(Func<bool> mppmCanCloseCallback)
        {
            ContainerWindow.SetMppmCanCloseCallback(mppmCanCloseCallback);
        }

        public string title
        {
            set => m_BackingObject.title = value;
        }

        public static ContainerWindowProxy FromInstanceID(int instanceId)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId) as ContainerWindow;
            return obj == null ? null : new ContainerWindowProxy(obj);
        }

        public static implicit operator bool(ContainerWindowProxy proxy) => proxy?.m_BackingObject;
    }
}
