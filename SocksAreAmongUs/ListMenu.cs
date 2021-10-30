using Reactor.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace SocksAreAmongUs
{
    public abstract class ListMenu
    {
        public virtual Vector2 Size { get; } = new Vector2(300, 180);

        public virtual Vector2 ButtonSize { get; } = new Vector2(130, 30);
        public virtual int Columns => 2;

        public Canvas Canvas { get; private set; }
        public bool IsShown => Canvas;

        public GameObject Content { get; private set; }

        public virtual void Show()
        {
            if (IsShown)
            {
                Hide();
            }

            Canvas = GUIExtensions.CreateCanvas().DontDestroy().GetComponent<Canvas>();

            if (!GameObject.Find("EventSystem"))
            {
                GUIExtensions.CreateEventSystem().DontDestroy();
            }

            var scrollView = DefaultControls.CreateScrollView(GUIExtensions.StandardResources).GetComponent<ScrollRect>();
            scrollView.transform.FindChild("Scrollbar Horizontal").Destroy();
            scrollView.horizontal = false;
            scrollView.scrollSensitivity = 32;
            scrollView.rectTransform.SetSize(Size.x, Size.y);
            scrollView.transform.SetParent(Canvas.transform, false);

            Content = scrollView.content.gameObject;

            var contentTransform = Content.GetComponent<RectTransform>();
            contentTransform.anchorMax = contentTransform.anchorMin = contentTransform.pivot = new Vector2(0.5f, 1f);

            var gridLayoutGroup = Content.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.cellSize = ButtonSize;
            gridLayoutGroup.spacing = new Vector2(5, 5);
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = Columns;
            gridLayoutGroup.padding.left = gridLayoutGroup.padding.right = gridLayoutGroup.padding.top = gridLayoutGroup.padding.bottom = 5;
            var contentSizeFitter = Content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.verticalScrollbar.value = 0;
        }

        public void Hide()
        {
            Canvas.Destroy();
            Canvas = null;
        }

        public void Toggle()
        {
            if (IsShown)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }
}
