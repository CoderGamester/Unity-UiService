using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// A Screen Space - Camera presenter stacked onto the scene's base camera by
	/// <c>UiCameraStackFeature</c>, so it renders inside the URP frame rather than over it.
	/// </summary>
	/// <remarks>
	/// The prefab carries the overlay Camera as a child; the feature inserts it into the base camera's
	/// stack on open and removes it on disable. That insertion is the part that cannot be pre-authored,
	/// because the stack list lives on a scene camera and this presenter only exists at runtime.
	/// </remarks>
	public class StackedWorldPresenter : UiPresenter
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
				_body.text = "Stacked via URP camera stacking.\n\n" +
					"Open the Overlay HUD while this is up: the HUD draws ON TOP even at a lower " +
					"UiConfig.Layer, because Screen Space - Overlay composites after all URP rendering.";
			}
		}
	}
}
