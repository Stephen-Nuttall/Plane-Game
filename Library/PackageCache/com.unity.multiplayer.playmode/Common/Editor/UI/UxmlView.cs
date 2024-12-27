using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    /// <remarks>
    /// Base class that handles binding uxml elements to fields in the view and loading the uxml file.
    /// Add [LoadUxmlView("path/to/uxml")] to the class to load the uxml file from a different path.
    /// </remarks>
    abstract class UxmlView<TView> : VisualElement
    {
        protected UxmlView()
        {
            InitializeAsync().Forget();
        }

        async Task InitializeAsync()
        {
            await LoadUxmlAsync();
            ResolveQueryFields();
            Initialized();
        }

        async Task LoadUxmlAsync()
        {
            var uxmlPath = (LoadUxmlViewAttribute)Attribute.GetCustomAttribute(typeof(TView), typeof(LoadUxmlViewAttribute));
            if (uxmlPath == null || string.IsNullOrWhiteSpace(uxmlPath.RootPath))
            {
                uxmlPath = new(Constants.UxmlRootPath);
            }

            var viewName = string.IsNullOrWhiteSpace(uxmlPath.ViewName) ? typeof(TView).Name : uxmlPath.ViewName;
            var filePath = $"{uxmlPath.RootPath}{viewName}.uxml";
            var loadAssetAtPath = await AssetDatabaseHelper.LoadAssetAtPathAsync<VisualTreeAsset>(filePath);
            loadAssetAtPath.CloneTree(this);
        }

        void ResolveQueryFields()
        {
            foreach (var field in UxmlViewCache.GetQueryFields<TView>())
            {
                var attribute = (UxmlElementAttribute)field.GetCustomAttribute(typeof(UxmlElementAttribute));
                var queryName = string.IsNullOrWhiteSpace(attribute.Name) ? field.Name : attribute.Name;
                var element = this.Q(queryName);
                field.SetValue(this, element);
            }
        }

        protected virtual void Initialized()
        {
            if (hierarchy.parent != null)
            {
                OnAttach(default);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected virtual void OnAttach([MaybeNull] AttachToPanelEvent evt) { }
        protected virtual void OnDetach([MaybeNull] DetachFromPanelEvent evt) { }
    }

    /// <remarks>
    /// Leveraging InitializeOnLoad allows us to compute this when the domain is reloaded. The alternative is getting
    /// the fields we need whenever the view is first created, which results in a noticeable hang.
    /// </remarks>
    [InitializeOnLoad]
    static class UxmlViewCache
    {
        static readonly Dictionary<Type, List<FieldInfo>> s_QueryAttributeFields;

        static UxmlViewCache()
        {
            s_QueryAttributeFields = TypeCache.GetFieldsWithAttribute<UxmlElementAttribute>()
                .GroupBy(x => x.ReflectedType)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        public static IReadOnlyCollection<FieldInfo> GetQueryFields<TView>()
        {
            if (!s_QueryAttributeFields.TryGetValue(typeof(TView), out var fields))
            {
                fields = new List<FieldInfo>();
            }

            return fields;
        }
    }
}
