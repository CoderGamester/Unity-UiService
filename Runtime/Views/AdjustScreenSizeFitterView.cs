using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLovers.UiService.Views
{ 
    /// <summary>
    /// Resizes a RectTransform to fit the size of its content.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="ContentSizeFitter"/>, but rounds the size of its content between min and preferred sizes.
    /// Works better with explicit size anchors and not fitting size anchors
    /// </remarks>
    [AddComponentMenu("Layout/Adjust Size Fitter", 141)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(LayoutElement))]
    public class AdjustScreenSizeFitterView : UIBehaviour, ILayoutSelfController
    {
        [SerializeField] private RectOffset _padding = new RectOffset();
        [SerializeField] private RectTransform _canvasTransform;
        [SerializeField] private RectTransform _rectTransform;
        
        private Vector2 _previousSize;
        
        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }
        
        private RectTransform CanvasRectTransform
        {
            get
            {
                if (_canvasTransform == null) _canvasTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
                
                _previousSize = _canvasTransform.sizeDelta;
                
                return _canvasTransform;
            }
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            base.OnDisable();
        }

        private void LateUpdate()
        {
            var previousSize = _previousSize;

            if (previousSize == CanvasRectTransform.sizeDelta)
            {
                return;
            }
            
            _previousSize = CanvasRectTransform.sizeDelta;
            SetDirty();
        }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutHorizontal() {}

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutVertical() {}
        
        /// <inheritdoc />
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            SetDirty();
        }

        private void SetDirty()
        {
            if (!IsActive())
                return;
            
            var resolution = CanvasRectTransform.sizeDelta;
            var oldSize = RectTransform.sizeDelta;
            var newWidth = GetSize((int) RectTransform.Axis.Horizontal, resolution);
            var newHeight = GetSize((int) RectTransform.Axis.Vertical, resolution);

            if (!Mathf.Approximately(newWidth, oldSize.x))
            {
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            }

            if (!Mathf.Approximately(newHeight, oldSize.y))
            {
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
            }
        }

        private float GetSize(int axis, Vector2 resolution)
        {
            var minSize = LayoutUtility.GetMinSize(RectTransform, axis);
            var maxSize = LayoutUtility.GetFlexibleSize(RectTransform, axis);
            var resolutionX = resolution.x - _padding.left - _padding.right;
            var resolutionY = resolution.y - _padding.top - _padding.bottom;
            
            if (axis == 0)
            {
                if(resolutionX < minSize && minSize > 0) return minSize;
                if(resolutionX >= maxSize && maxSize > minSize && maxSize > 1) return maxSize;

                return RectTransform.sizeDelta.x > resolutionX && resolutionX > 0 ? resolutionX : RectTransform.sizeDelta.x;
            }
            
            if(resolutionY < minSize && minSize > 0) return minSize;
            if(resolutionY >= maxSize && maxSize > minSize && maxSize > 1) return maxSize;
            
            return RectTransform.sizeDelta.y > resolutionY && resolutionY > 0 ? resolutionY : RectTransform.sizeDelta.y;
        }

    #if UNITY_EDITOR
        /// <inheritdoc />
        protected override void OnValidate()
        {
            SetDirty();
        }
    #endif
    }
}