using System;
using System.Reflection;

namespace Unity.Multiplayer.PlayMode.Editor.Bridge
{
    class ApplicationTitleDescriptorProxy
    {
        readonly object m_Obj;
        readonly FieldInfo m_FieldInfo;

        const string FieldName = "title";

        internal ApplicationTitleDescriptorProxy(object obj)
        {
            m_Obj = obj
                    ?? throw new ArgumentNullException(nameof(obj));
            m_FieldInfo = obj.GetType().GetField(FieldName, BindingFlags.Public | BindingFlags.Instance)
                          ?? throw new InvalidOperationException($"Unable to get field info for {FieldName}");
        }

        public string title
        {
            get => m_FieldInfo.GetValue(m_Obj).ToString();
            set => m_FieldInfo.SetValue(m_Obj, value);
        }

        public override string ToString()
        {
            return $"{nameof(title)}: {title}";
        }
    }
}
