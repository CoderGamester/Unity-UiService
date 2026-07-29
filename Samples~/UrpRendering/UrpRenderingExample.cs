using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Drives the URP rendering sample: opens a blur-backed modal, a URP camera-stacked presenter, and a
	/// plain Overlay HUD that demonstrates the layer-ordering hazard between the two render modes.
	/// </summary>
	public class UrpRenderingExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _configs;
		[SerializeField] private Transform _spinningContent;

		[Header("Actions")]
		[SerializeField] private Button _openBlurredModalButton;
		[SerializeField] private Button _openStackedButton;
		[SerializeField] private Button _openOverlayHudButton;
		[SerializeField] private Button _closeAllButton;

		[Header("Status")]
		[SerializeField] private TMP_Text _log;
		[SerializeField] private ScrollRect _logScrollRect;

		private readonly StringBuilder _logBuilder = new();

		private IUiServiceInit _uiService;

		private void Awake()
		{
			WireButton(_openBlurredModalButton, OpenBlurredModal);
			WireButton(_openStackedButton, OpenStacked);
			WireButton(_openOverlayHudButton, OpenOverlayHud);
			WireButton(_closeAllButton, CloseAll);
			EnsureInputModuleOnEventSystem();
		}

		private void Start()
		{
			if (_configs == null)
			{
				Append("No UiConfigs assigned on the sample driver -- nothing can open.");
				return;
			}

			_uiService = new UiService(new PrefabRegistryUiAssetLoader(_configs));
			_uiService.Init(_configs);

			Append("Ready. Open the blurred modal first; if nothing blurs, read the Console.");
		}

		private void Update()
		{
			if (_spinningContent != null)
			{
				_spinningContent.Rotate(new Vector3(12f, 30f, 0f) * Time.deltaTime);
			}
		}

		private void OnDestroy()
		{
			_uiService?.Dispose();
		}

		private void OpenBlurredModal() => OpenAsync<BlurredModalPresenter>().Forget();

		private void OpenStacked() => OpenAsync<StackedWorldPresenter>().Forget();

		private void OpenOverlayHud() => OpenAsync<OverlayHudPresenter>().Forget();

		private async UniTaskVoid OpenAsync<T>() where T : UiPresenter
		{
			if (_uiService == null)
			{
				return;
			}

			await _uiService.OpenUiAsync<T>();
			Append($"Opened {typeof(T).Name}.");
		}

		private void CloseAll()
		{
			if (_uiService == null)
			{
				return;
			}

			_uiService.CloseAllUi();
			Append("Closed everything. The blur lifts once the last blur presenter is hidden.");
		}

		private void Append(string line)
		{
			if (_log == null)
			{
				return;
			}

			var wasAtBottom = _logScrollRect == null || _logScrollRect.verticalNormalizedPosition < 0.05f;

			_logBuilder.AppendLine(line);
			_log.text = _logBuilder.ToString();

			if (_logScrollRect != null && wasAtBottom)
			{
				Canvas.ForceUpdateCanvases();
				_logScrollRect.verticalNormalizedPosition = 0f;
			}
		}

		private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
		{
			if (button != null)
			{
				button.onClick.AddListener(action);
			}
		}

		// Samples ship their own EventSystem, so the input module has to match whichever Active Input
		// Handling the consumer project uses or the legacy module throws on Input.mousePosition.
		private static void EnsureInputModuleOnEventSystem()
		{
			var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
			if (eventSystem == null)
			{
				return;
			}

			var go = eventSystem.gameObject;
#if ENABLE_INPUT_SYSTEM
			if (go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null)
			{
				return;
			}

			var legacy = go.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
			if (legacy != null)
			{
				// DestroyImmediate, so the swap lands before EventSystem.Update first ticks the legacy module.
				DestroyImmediate(legacy);
			}

			go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
			if (go.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
			{
				go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
			}
#endif
		}
	}
}
