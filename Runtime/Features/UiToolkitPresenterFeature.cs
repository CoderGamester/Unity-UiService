using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GameLovers.UiService
{
	/// <summary>
	/// Feature that provides UI Toolkit integration for a <see cref="UiPresenter"/>.
	/// Handles visual tree timing so subscribers don't need to worry about it.
	/// </summary>
	[RequireComponent(typeof(UIDocument))]
	public class UiToolkitPresenterFeature : PresenterFeatureBase
	{
		[SerializeField] private UIDocument _document;

		private readonly UnityEvent<VisualElement> _onVisualTreeReady = new UnityEvent<VisualElement>();
		private bool _callbacksRegistered;

		/// <summary>
		/// The attached <see cref="UIDocument"/>.
		/// </summary>
		public UIDocument Document => _document;

		/// <summary>
		/// The root <see cref="VisualElement"/> of the UIDocument.
		/// </summary>
		public VisualElement Root => _document?.rootVisualElement;

		private void OnValidate()
		{
			_document = _document ?? GetComponent<UIDocument>();
		}

		private void OnDestroy()
		{
			_onVisualTreeReady.RemoveAllListeners();
			UnregisterPanelCallbacks();
		}

		/// <summary>
		/// Registers a callback to be invoked when the visual tree is ready.
		/// Invokes on each open (UI Toolkit recreates elements on activate).
		/// Safe to call in OnInitialized().
		/// </summary>
		public void AddVisualTreeAttachedListener(UnityAction<VisualElement> callback)
		{
			if (callback == null)
			{
				return;
			}

			_onVisualTreeReady.AddListener(callback);

			// Visual tree already ready? Invoke immediately
			if (Root?.panel != null)
			{
				callback(Root);
			}
		}

		/// <summary>
		/// Removes a previously registered callback.
		/// </summary>
		public void RemoveVisualTreeAttachedListener(UnityAction<VisualElement> callback)
		{
			if (callback == null)
			{
				return;
			}

			_onVisualTreeReady.RemoveListener(callback);
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			RegisterPanelCallbacks();
			TryInvokeListeners();
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			base.OnPresenterOpened();
			RegisterPanelCallbacks();
			TryInvokeListeners();
		}

		private void RegisterPanelCallbacks()
		{
			if (_callbacksRegistered)
			{
				return;
			}

			var root = Root;
			if (root == null)
			{
				return;
			}

			root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			_callbacksRegistered = true;
		}

		private void UnregisterPanelCallbacks()
		{
			if (!_callbacksRegistered)
			{
				return;
			}

			var root = Root;
			if (root != null)
			{
				root.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			}
			_callbacksRegistered = false;
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			TryInvokeListeners();
		}

		private void TryInvokeListeners()
		{
			var root = Root;
			if (root?.panel == null)
			{
				return;
			}

			_onVisualTreeReady.Invoke(root);
		}
	}
}

