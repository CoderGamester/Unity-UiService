using System.Collections;
using GameLovers.UiService.Views;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class InteractableTextViewTests
	{
		private GameObject _canvasGo;
		private GameObject _textGo;
		private TextMeshProUGUI _tmp;
		private InteractableTextView _view;
		private int _onLinkedInfoClickedCount;
		private TMP_LinkInfo _lastLinkInfo;

		[SetUp]
		public void Setup()
		{
			_canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
			var canvas = _canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			_textGo = new GameObject("Text", typeof(RectTransform));
			_textGo.transform.SetParent(_canvasGo.transform, false);

			_tmp = _textGo.AddComponent<TextMeshProUGUI>();
			_view = _textGo.AddComponent<InteractableTextView>();

			// InteractableTextView wires its `_text` field via OnValidate (an editor-only callback).
			// At runtime the field starts null; SendMessage triggers the same wiring path safely
			// without resorting to BindingFlags reflection on a private member.
			_view.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);

			_onLinkedInfoClickedCount = 0;
			_lastLinkInfo = default;
			_view.OnLinkedInfoClicked = new UnityEngine.Events.UnityEvent<TMP_LinkInfo>();
			_view.OnLinkedInfoClicked.AddListener(info =>
			{
				_onLinkedInfoClickedCount++;
				_lastLinkInfo = info;
			});
		}

		[TearDown]
		public void TearDown()
		{
			if (_textGo != null)
			{
				Object.DestroyImmediate(_textGo);
			}
			if (_canvasGo != null)
			{
				Object.DestroyImmediate(_canvasGo);
			}
		}

		[UnityTest]
		public IEnumerator OnPointerClick_OnLinkedTextHit_FiresOnLinkedInfoClickedWithLinkInfo()
		{
			_tmp.text = "click <link=\"my_link_id\">here</link> now";
			_tmp.ForceMeshUpdate();
			yield return null;

			Assume.That(_tmp.textInfo.linkInfo.Length, Is.GreaterThan(0),
				"TMP failed to register link info — likely missing default font asset in test runtime");
			Assume.That(_tmp.textInfo.linkInfo[0].GetLinkID(), Is.EqualTo("my_link_id"));

			var rectTransform = _textGo.GetComponent<RectTransform>();
			var firstLinkChar = _tmp.textInfo.linkInfo[0].linkTextfirstCharacterIndex;
			var charInfo = _tmp.textInfo.characterInfo[firstLinkChar];
			var localCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
			var worldPos = rectTransform.TransformPoint(localCenter);
			var screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);

			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(1, _onLinkedInfoClickedCount);
			Assert.AreEqual("my_link_id", _lastLinkInfo.GetLinkID());
		}

		[UnityTest]
		public IEnumerator OnPointerClick_NoLinkInText_DoesNotFireOnLinkedInfoClicked()
		{
			_tmp.text = "no links here at all";
			_tmp.ForceMeshUpdate();
			yield return null;

			var screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

			_view.OnPointerClick(eventData);

			Assert.AreEqual(0, _onLinkedInfoClickedCount);
		}
	}
}
