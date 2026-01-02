using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating UI Toolkit integration with UiService.
	/// Uses UI Toolkit for both the example controls and the presenter.
	/// </summary>
	public class UiToolkitExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;
		[SerializeField] private UIDocument _uiDocument;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _explanationText;

		private IUiServiceInit _uiService;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners from UIDocument
			var root = _uiDocument.rootVisualElement;
			
			root.Q<Button>("load-button")?.RegisterCallback<ClickEvent>(_ => LoadUiToolkit());
			root.Q<Button>("open-button")?.RegisterCallback<ClickEvent>(_ => OpenUiToolkit());
			root.Q<Button>("close-button")?.RegisterCallback<ClickEvent>(_ => CloseUiToolkit());
			root.Q<Button>("unload-button")?.RegisterCallback<ClickEvent>(_ => UnloadUiToolkit());
			
			// Pre-load presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<UiToolkitExamplePresenter>();
			presenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));

			UpdateUiVisibility(false);
		}

		private void OnDestroy()
		{
			_uiService?.Dispose();
		}

		/// <summary>
		/// Loads the UI Toolkit presenter
		/// </summary>
		public void LoadUiToolkit()
		{
			_uiService.LoadUiAsync<UiToolkitExamplePresenter>().Forget();
		}

		/// <summary>
		/// Opens the UI Toolkit presenter
		/// </summary>
		public async void OpenUiToolkit()
		{
			await _uiService.OpenUiAsync<UiToolkitExamplePresenter>();
			UpdateUiVisibility(true);
		}

		/// <summary>
		/// Closes the UI Toolkit presenter
		/// </summary>
		public void CloseUiToolkit()
		{
			_uiService.CloseUi<UiToolkitExamplePresenter>(destroy: false);
			UpdateUiVisibility(false);
		}

		/// <summary>
		/// Unloads the UI Toolkit presenter
		/// </summary>
		public void UnloadUiToolkit()
		{
			_uiService.UnloadUi<UiToolkitExamplePresenter>();
			UpdateUiVisibility(false);
		}

		private void UpdateUiVisibility(bool isPresenterActive)
		{
			if (_explanationText != null)
			{
				_explanationText.gameObject.SetActive(!isPresenterActive);
			}
		}
	}
}

