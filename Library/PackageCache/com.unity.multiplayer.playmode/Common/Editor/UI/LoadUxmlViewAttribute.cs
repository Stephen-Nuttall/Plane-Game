using System;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    sealed class LoadUxmlViewAttribute : Attribute
    {
        public LoadUxmlViewAttribute(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public string ViewName { get; set; }
    }
}
