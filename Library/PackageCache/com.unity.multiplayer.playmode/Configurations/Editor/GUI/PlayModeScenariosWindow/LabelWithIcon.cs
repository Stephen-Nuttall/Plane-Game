using System;
using System.Linq;
using Unity.Multiplayer.PlayMode.Common.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Configurations.Editor.Gui
{
    /// <summary>
    /// Used in the PlayModeListView as ListItem
    /// </summary>
    class LabelWithIcon : VisualElement
    {
        static readonly string k_BaseClassName = "label-with-icon";

        static string s_VisualTreePath = "Configurations/Editor/GUI/PlayModeScenariosWindow/LabelWithIcon.uxml";
        static string s_StylePath = "Configurations/Editor/GUI/PlayModeScenariosWindow/LabelWithIcon.uss";

        /// <summary>
        /// Invoked if the edit was successful and no InputWarning was present.
        /// </summary>
        public event Action<string> OnFinishEdit;

        /// <summary>
        /// Invoked each time the TextField value changes while editing.
        /// </summary>
        public event Action<string> OnEdit;

        /// <summary>
        /// Invoked when editing was canceled (ESC) or an InputWarning was present.
        /// </summary>
        public event Action OnCancel;

        public string Text { get => Label.text; set => Label.text = value; }

        Label Label => this.Q<Label>();
        TextField TextField => this.Q<TextField>();
        VisualElement Icon => this.Q<VisualElement>("icon");

        bool m_InputIsValid;
        readonly VisualElement m_WarnIcon;

        public bool InputIsValid
        {
            get => m_InputIsValid;
            set
            {
                m_InputIsValid = value;
                if (m_InputIsValid)
                    RemoveFromClassList("input-warning");
                else
                    AddToClassList("input-warning");
            }
        }

        internal void ShowWarningIcon(bool show, string tooltipText)
        {
            m_WarnIcon.tooltip = tooltipText;
            m_WarnIcon.style.display = show ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        public LabelWithIcon(string text, string styleClass = "", string iconName = "")
        {
            VisualElement item = new VisualElement();

            UIUtils.LoadUxmlAsync(s_VisualTreePath, item).Forget();
            UIUtils.ApplyStyleSheetAsync(s_StylePath, this).Forget();
            var element = item.Q("label-with-icon");
            name = k_BaseClassName;
            AddToClassList(k_BaseClassName);
            AddToClassList(styleClass);

            var icon = element.Q<VisualElement>("icon");
            if (string.IsNullOrEmpty(iconName))
                element.Q<VisualElement>("icon-container").style.display = DisplayStyle.None;
            else
            {
                var iconTexture = EditorGUIUtility.IconContent(iconName).image as Texture2D;
                icon.style.backgroundImage = iconTexture;
            }

            m_WarnIcon = element.Q<VisualElement>("warn-icon");
            m_WarnIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_WarnIcon.style.backgroundImage = EditorGUIUtility.FindTexture("console.warnicon"); ;

            var label = element.Q<Label>("label");
            label.text = text;

            // copy just the content of the uxml and not the template container etc. to prevent extra nesting
            while (element.Children().Any())
            {
                Add(element.Children().First());
            }
        }

        public void EnableEditMode()
        {
            TextField.SetValueWithoutNotify(Label.text);
            TextField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            TextField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);

            TextField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            TextField.UnregisterCallback<BlurEvent>(OnTextFieldFocusOut);
            TextField.RegisterCallback<BlurEvent>(OnTextFieldFocusOut);

            AddToClassList("editable");

            // use scheduler again, because we have to wait until the textfield has display=flex
            // before we can focus it.
            schedule.Execute(() =>
            {
                TextField.Focus();
                OnEdit?.Invoke(TextField.text);
            }).ExecuteLater(50);
        }

        void DisableEditMode(bool cancel = false)
        {
            RemoveFromClassList("input-warning");
            RemoveFromClassList("editable");

            TextField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            TextField.UnregisterCallback<BlurEvent>(OnTextFieldFocusOut);

            if (cancel || !InputIsValid)
            {
                OnCancel?.Invoke();
                return;
            }

            OnFinishEdit?.Invoke(TextField.text);
        }

        void OnTextFieldFocusOut(BlurEvent evt)
        {
            DisableEditMode();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                DisableEditMode(true);
                evt.StopPropagation();
            }
        }

        void OnTextFieldChange(ChangeEvent<string> evt)
        {
            OnEdit?.Invoke(evt.newValue);
        }

        public static LabelWithIcon Create(string text, string styleClass = "", string iconClass = "")
        {
            return new LabelWithIcon(text, styleClass, iconClass);
        }

        public void SetIcon(string iconClass)
        {
            var classToRemove = Icon.GetClasses().FirstOrDefault(c => c.StartsWith("icon-"));
            if (classToRemove != null)
                Icon.RemoveFromClassList(classToRemove);
            Icon.AddToClassList(iconClass);
        }

        public void SetIcon(Texture2D iconTexture)
        {
            Icon.style.backgroundImage = iconTexture;
        }
    }
}
