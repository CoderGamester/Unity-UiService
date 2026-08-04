using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameLovers.UiService.Views
{
    /// <summary>
    /// This view is responsible to handle all types of links (ex: hyperlinks) set in the <see cref="TMP_Text"/>
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class InteractableTextView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Which link a click resolves to.
        /// </summary>
        public enum InteractableTextType
        {
            IntersectingLink,
            NearestLink
        }

        public UnityEvent<TMP_LinkInfo> OnLinkedInfoClicked;
        public InteractableTextType InteractableType;

        [SerializeField] private TMP_Text _text;

        /// <summary>The text component whose links this view resolves.</summary>
        public TMP_Text Text => _text;

        private void OnValidate()
        {
            _text = _text == null ? GetComponent<TMP_Text>() : _text;
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            var camera = ResolveEventCamera();
            var linkedText = -1;

            if (InteractableType == InteractableTextType.IntersectingLink)
            {
                linkedText = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, camera);
            }
            else
            {
                linkedText = TMP_TextUtilities.FindNearestLink(_text, eventData.position, camera);
            }

            if (linkedText > -1)
            {
                OnLinkedInfoClicked.Invoke(_text.textInfo.linkInfo[linkedText]);
            }
        }

        private Camera ResolveEventCamera()
        {
            var canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                return null;
            }

            var isOverlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            var isCameraless = canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null;

            return isOverlay || isCameraless ? null : canvas.worldCamera;
        }
    }
}