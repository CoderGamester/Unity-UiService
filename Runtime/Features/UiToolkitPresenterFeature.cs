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

		internal void SetDocument(UIDocument document) => _document = document;

		/// <summary>
		/// The attached <see cref="UIDocument"/>.
		/// </summary>
		public UIDocument Document => _document;

		/// <summary>
		/// The root <see cref="VisualElement"/> of the UIDocument.
		/// </summary>
		public VisualElement Root => _document?.rootVisualElement;

		/// <summary>
		/// The <see cref="PanelSettings"/> driving this document's panel. <b>Read-only by contract: never
		/// mutate it at runtime.</b>
		/// </summary>
		/// <remarks>
		/// <para>
		/// A <see cref="PanelSettings"/> asset <i>is</i> a panel. Every <see cref="UIDocument"/> pointing at
		/// the same asset shares one panel -- which is what makes cross-presenter ordering via
		/// <see cref="UIDocument.sortingOrder"/> (set by <see cref="UiService"/> from <c>UiConfig.Layer</c>)
		/// work at all. Writing to this asset therefore reconfigures every other presenter sharing it, and in
		/// the Editor persists that change to disk.
		/// </para>
		/// <para>
		/// If a presenter needs different panel settings -- a different render mode, reference resolution, or
		/// clear behaviour -- <b>author a second <see cref="PanelSettings"/> asset</b> and point this
		/// presenter's <see cref="UIDocument"/> at it. That is a designer-time decision, and it is how UI
		/// Toolkit is meant to be used. For world-space UI specifically, set
		/// <see cref="PanelSettings.renderMode"/> to <see cref="PanelRenderMode.WorldSpace"/> on that asset
		/// and position per-document via <see cref="UIDocument.position"/> / <c>pivot</c> /
		/// <c>worldSpaceSize</c>.
		/// </para>
		/// </remarks>
		public PanelSettings PanelSettings => _document?.panelSettings;

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

