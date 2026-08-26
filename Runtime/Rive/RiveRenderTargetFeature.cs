using Rive.Components;
using UnityEngine;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Assigns a Rive panel to a dedicated, atlas, or pooled render-target policy.
	/// </summary>
	public sealed class RiveRenderTargetFeature : PresenterFeatureBase, IUiRenderActivity
	{
		[SerializeField]
		private RivePanel _panel;
		[SerializeField]
		private UiRenderTargetPolicy _policy;
		[SerializeField]
		private string _groupId;
		[SerializeField]
		private Vector2Int _textureSize = new Vector2Int(1024, 1024);
		[SerializeField]
		private int _initialPoolSize = 2;
		[SerializeField]
		private int _maxPoolSize = 6;

		private RiveRenderTargetLease _lease;
		private bool _presenterOpen;
		private bool _renderActivityEnabled = true;

		private void OnDestroy()
		{
			_lease?.Dispose();
			_lease = null;
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);

			if (_panel == null)
			{
				_panel = GetComponent<RivePanel>();
			}

			if (_policy == UiRenderTargetPolicy.SharedAtlas &&
				_panel.DrawOptimization == DrawOptimizationOptions.AlwaysDraw)
			{
				Debug.LogWarning(
					$"Rive panel '{name}' used AlwaysDraw in shared atlas '{_groupId}'. " +
					$"{nameof(DrawOptimizationOptions.DrawWhenChanged)} was enforced so one panel cannot invalidate the whole atlas.",
					this);
				_panel.DrawOptimization = DrawOptimizationOptions.DrawWhenChanged;
			}

			_lease = RiveRenderTargetRegistry.Acquire(
				_policy,
				_groupId,
				_textureSize,
				_initialPoolSize,
				_maxPoolSize);
			if (_lease.Strategy != null)
			{
				_panel.RenderTargetStrategy = _lease.Strategy;
			}
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			_presenterOpen = true;
			ApplyRenderingState();
		}

		/// <inheritdoc />
		public override void OnPresenterClosed()
		{
			_presenterOpen = false;
			ApplyRenderingState();
		}

		/// <inheritdoc />
		public void SetRenderActive(bool isActive)
		{
			_renderActivityEnabled = isActive;
			ApplyRenderingState();
		}

		private void ApplyRenderingState()
		{
			if (_panel == null)
			{
				return;
			}

			if (_presenterOpen && _renderActivityEnabled)
			{
				_panel.StartRendering();
				return;
			}

			_panel.StopRendering();
		}
	}
}
