using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    class PopupWindowContent<TView> : PopupWindowContent
        where TView : VisualElement, new()
    {
        readonly TView m_View;
        readonly Vector2 m_WindowContentSize;

        public PopupWindowContent(int width, int height)
        {
            m_View = new TView
            {
                style =
                {
                    paddingBottom = new StyleLength(new Length(4, LengthUnit.Pixel)),
                    paddingLeft = new StyleLength(new Length(4, LengthUnit.Pixel)),
                    paddingRight = new StyleLength(new Length(4, LengthUnit.Pixel)),
                    paddingTop = new StyleLength(new Length(4, LengthUnit.Pixel)),
                },
            };
            m_WindowContentSize = new Vector2(width, height);
        }

        public override Vector2 GetWindowSize()
        {
            return m_WindowContentSize;
        }

        public override void OnGUI(Rect rect)
        {
            editorWindow.maxSize = new Vector2(m_WindowContentSize.x, m_View.resolvedStyle.height);
        }

        public override void OnOpen()
        {
            editorWindow.rootVisualElement.Add(m_View);
        }
    }
}
