using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

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

		private IUiServiceInit _uiService;

		private void Start()
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
			
			Debug.Log("=== UI Toolkit Example Started ===");
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
			Debug.Log("Loading UI Toolkit UI...");
			_uiService.LoadUiAsync<UiToolkitExamplePresenter>().Forget();
		}

		/// <summary>
		/// Opens the UI Toolkit presenter
		/// </summary>
		public void OpenUiToolkit()
		{
			Debug.Log("Opening UI Toolkit UI...");
			_uiService.OpenUiAsync<UiToolkitExamplePresenter>().Forget();
		}

		/// <summary>
		/// Closes the UI Toolkit presenter
		/// </summary>
		public void CloseUiToolkit()
		{
			Debug.Log("Closing UI Toolkit UI...");
			_uiService.CloseUi<UiToolkitExamplePresenter>(destroy: false);
		}

		/// <summary>
		/// Unloads the UI Toolkit presenter
		/// </summary>
		public void UnloadUiToolkit()
		{
			Debug.Log("Unloading UI Toolkit UI...");
			_uiService.UnloadUi<UiToolkitExamplePresenter>();
		}
	}
}
