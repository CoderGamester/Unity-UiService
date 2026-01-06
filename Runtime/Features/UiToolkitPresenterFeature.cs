using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GameLovers.UiService
{
	/// <summary>
	/// Feature that provides UI Toolkit integration for a <see cref="UiPresenter"/>.
	/// Ensures a <see cref="UIDocument"/> is present and provides access to it.
	/// </summary>
	/// <remarks>
	/// UI Toolkit documents may not have their visual tree ready immediately after instantiation.
	/// Subscribe to <see cref="OnVisualTreeAttached"/> to safely query elements after the visual tree is attached to a panel.
	/// </remarks>
	[RequireComponent(typeof(UIDocument))]
	public class UiToolkitPresenterFeature : PresenterFeatureBase
	{
		[SerializeField] private UIDocument _document;

		/// <summary>
		/// Provides access to the attached <see cref="UIDocument"/>
		/// </summary>
		public UIDocument Document => _document;

		/// <summary>
		/// The root element of the <see cref="UIDocument"/>.
		/// May be null or empty if the visual tree is not yet attached to a panel.
		/// Check <see cref="IsVisualTreeAttached"/> or subscribe to <see cref="OnVisualTreeAttached"/> for safe element queries.
		/// </summary>
		public VisualElement Root => _document?.rootVisualElement;

		/// <summary>
		/// Returns true if the visual tree is attached to a panel and ready for element queries.
		/// </summary>
		public bool IsVisualTreeAttached { get; private set; }

		/// <summary>
		/// Event invoked when the visual tree is attached to a panel and ready for element queries.
		/// The <see cref="Root"/> element is passed as a parameter.
		/// </summary>
		/// <remarks>
		/// If the visual tree is already attached when subscribing, the event will be invoked immediately.
		/// Use <see cref="AddVisualTreeAttachedListener"/> for safe subscription that handles this case.
		/// </remarks>
		public UnityEvent<VisualElement> OnVisualTreeAttached { get; } = new UnityEvent<VisualElement>();

		private bool _callbacksRegistered;

		private void OnValidate()
		{
			_document = _document ?? GetComponent<UIDocument>();
		}

		private void OnDestroy()
		{
			OnVisualTreeAttached.RemoveAllListeners();
			UnregisterPanelCallbacks();
		}

		/// <summary>
		/// Adds a listener to <see cref="OnVisualTreeAttached"/> and invokes it immediately if already attached.
		/// This is a convenience method for safe element queries that handles the visual tree timing issue.
		/// </summary>
		/// <param name="callback">The callback to invoke with the root <see cref="VisualElement"/>.</param>
		public void AddVisualTreeAttachedListener(UnityAction<VisualElement> callback)
		{
			if (callback == null)
			{
				return;
			}

			OnVisualTreeAttached.AddListener(callback);

			if (IsVisualTreeAttached && Root != null)
			{
				callback(Root);
			}
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			RegisterPanelCallbacks();
			TrySetReady();
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			base.OnPresenterOpened();
			
			// Retry registration if Root wasn't available during initialization
			RegisterPanelCallbacks();
			TrySetReady();
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
			root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
				root.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			}
			_callbacksRegistered = false;
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			TrySetReady();
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			IsVisualTreeAttached = false;
		}

		private void TrySetReady()
		{
			var root = Root;
			if (root?.panel == null || IsVisualTreeAttached)
			{
				return;
			}

			IsVisualTreeAttached = true;
			OnVisualTreeAttached.Invoke(root);
		}
	}
}

