using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// A modal whose backdrop is blurred by <c>UiBackdropBlurPresenterFeature</c> on the same prefab.
	/// </summary>
	/// <remarks>
	/// Nothing here touches the blur: the feature holds it open for as long as this presenter is
	/// visible, and the look is authored on the URP Renderer asset. That is the whole point of the
	/// sample -- the presenter's own code is unaware a blur exists.
	/// </remarks>
	public class BlurredModalPresenter : UiPresenter
	{
		[SerializeField] private Button _closeButton;
		[SerializeField] private TMP_Text _body;

		private void Awake()
		{
			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(() => Close(false));
			}
		}

		protected override void OnOpened()
		{
			if (_body != null)
			{
				_body.text = "Everything behind this panel is blurred by the URP renderer feature.\n\n" +
					"If it is NOT blurred, UiBackdropBlurRendererFeature is missing from your Renderer asset -- " +
					"check the Console for the error naming the setup step.";
			}
		}
	}
}
