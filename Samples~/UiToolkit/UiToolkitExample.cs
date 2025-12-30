using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating UI Toolkit integration with UiService.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class UiToolkitExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _closeButton;
		
		private IUiServiceInit _uiService;

		private void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openButton?.onClick.AddListener(OpenUiToolkit);
			_closeButton?.onClick.AddListener(CloseUiToolkit);
			
			Debug.Log("=== UI Toolkit Example Started ===");
			Debug.Log("Use the UI buttons to open/close the UI Toolkit presenter.");
			Debug.Log("");
			Debug.Log("Note: Make sure to create a UXML document and assign it to the UIDocument component");
		}

		private void OnDestroy()
		{
			_openButton?.onClick.RemoveListener(OpenUiToolkit);
			_closeButton?.onClick.RemoveListener(CloseUiToolkit);
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
	}
}
