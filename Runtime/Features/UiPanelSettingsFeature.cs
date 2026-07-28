using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService
{
	/// <summary>
	/// Clone policy for <see cref="UiPanelSettingsFeature"/>. Controls whether a presenter gets its own
	/// <see cref="PanelSettings"/> instance or keeps sharing the asset assigned on its <see cref="UIDocument"/>.
	/// </summary>
	public enum UiPanelSettingsClonePolicy
	{
		/// <summary>Never clone; overrides and <see cref="UiPanelSettingsFeature.SetTargetTexture"/> are no-ops
		/// (logged) since <see cref="PanelSettings"/> is a shared project asset and must not be mutated in place.</summary>
		UseSharedAsset,

		/// <summary>Clone only if at least one override field is set, or <see cref="UiPanelSettingsFeature.SetTargetTexture"/> is called. Default.</summary>
		CloneWhenOverridden,

		/// <summary>Always clone on init, even with no overrides.</summary>
		CloneAlways
	}

	/// <summary>
	/// Per-presenter override values for a cloned <see cref="PanelSettings"/>. Each field is paired with an
	/// <c>Override*</c> flag because <see cref="PanelSettings"/> has no "unset" state to fall back to.
	/// </summary>
	[Serializable]
	public struct UiPanelSettingsOverrides
	{
		public bool OverrideClearColor;
		public bool ClearColor;

		public bool OverrideColorClearValue;
		public Color ColorClearValue;

		public bool OverrideClearDepthStencil;
		public bool ClearDepthStencil;

		public bool OverrideTargetDisplay;
		public int TargetDisplay;

		public bool OverrideRenderMode;
		public PanelRenderMode RenderMode;

		public bool OverrideScaleMode;
		public PanelScaleMode ScaleMode;

		public bool OverrideReferenceResolution;
		public Vector2Int ReferenceResolution;

		public bool OverrideForceGammaRendering;
		public bool ForceGammaRendering;

		/// <summary>True if at least one <c>Override*</c> flag is set.</summary>
		public readonly bool HasAnyOverride =>
			OverrideClearColor || OverrideColorClearValue || OverrideClearDepthStencil ||
			OverrideTargetDisplay || OverrideRenderMode || OverrideScaleMode ||
			OverrideReferenceResolution || OverrideForceGammaRendering;
	}

	/// <summary>
	/// Gives a <see cref="UiToolkitPresenterFeature"/> presenter URP/consumer-safe control over its
	/// <see cref="PanelSettings"/> -- render mode, clear behaviour, resolution, and (via
	/// <see cref="SetTargetTexture"/>) a render-texture target -- without ever mutating the shared project
	/// asset that other <see cref="UIDocument"/>s may reference.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="PanelSettings"/> is a plain <see cref="ScriptableObject"/> asset, commonly shared across several
	/// <see cref="UIDocument"/>s (e.g. every UI Toolkit presenter in a project). Writing directly to a shared
	/// asset's properties (its render mode, its target texture, ...) mutates it for every consumer, and in the
	/// Editor persists that mutation to disk. This feature clones the asset per presenter instead, so per-presenter
	/// configuration never leaks.
	/// </para>
	/// <para>
	/// Cloning has one consequence to be aware of: multiple <see cref="UIDocument"/>s sharing one
	/// <see cref="PanelSettings"/> share one UI Toolkit panel, and are ordered within it via
	/// <see cref="UIDocument.sortingOrder"/> -- which is exactly what <c>UiService</c> sets from a presenter's
	/// configured layer. Once a presenter gets its own cloned <see cref="PanelSettings"/>, it also gets its own
	/// panel, and cross-presenter ordering for that presenter is now driven by
	/// <see cref="PanelSettings.sortingOrder"/> instead. This feature mirrors
	/// <see cref="UIDocument.sortingOrder"/> onto the clone automatically so layering keeps working.
	/// </para>
	/// </remarks>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	public class UiPanelSettingsFeature : PresenterFeatureBase
	{
		[SerializeField] private UiPanelSettingsClonePolicy _clonePolicy = UiPanelSettingsClonePolicy.CloneWhenOverridden;
		[SerializeField] private UiPanelSettingsOverrides _overrides;

		private UiToolkitPresenterFeature _toolkitFeature;
		private PanelSettings _sharedAsset;
		private PanelSettings _clone;

		/// <summary>
		/// The <see cref="PanelSettings"/> actually driving this presenter's <see cref="UIDocument"/> right now:
		/// the clone if one was created, otherwise the shared asset.
		/// </summary>
		public PanelSettings ActivePanelSettings => _clone != null ? _clone : _sharedAsset;

		/// <summary>True if this presenter is using a cloned <see cref="PanelSettings"/> rather than the shared asset.</summary>
		public bool IsClone => _clone != null;

		/// <summary>Test-only configuration hook (package tests have InternalsVisibleTo access) -- avoids
		/// needing a Prefab/Inspector round trip just to exercise a non-default clone policy or overrides.</summary>
		internal void ConfigureForTest(UiPanelSettingsClonePolicy clonePolicy, UiPanelSettingsOverrides overrides = default)
		{
			_clonePolicy = clonePolicy;
			_overrides = overrides;
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);

			_toolkitFeature = GetComponent<UiToolkitPresenterFeature>();
			if (_toolkitFeature == null || _toolkitFeature.Document == null)
			{
				Debug.LogError($"{nameof(UiPanelSettingsFeature)} on '{name}' requires a {nameof(UiToolkitPresenterFeature)}" +
					" with an assigned UIDocument.", this);
				return;
			}

			_sharedAsset = _toolkitFeature.Document.panelSettings;
			if (_sharedAsset == null)
			{
				Debug.LogError($"{nameof(UiPanelSettingsFeature)} on '{name}': the UIDocument has no PanelSettings assigned.", this);
				return;
			}

			var shouldClone = _clonePolicy == UiPanelSettingsClonePolicy.CloneAlways ||
				(_clonePolicy == UiPanelSettingsClonePolicy.CloneWhenOverridden && _overrides.HasAnyOverride);

			if (shouldClone)
			{
				CreateClone();
			}

			ApplyOverrides();
			MirrorSortingOrder();
		}

		/// <summary>
		/// Routes this presenter's UI Toolkit output to <paramref name="texture"/> (or back to the screen if
		/// <see langword="null"/>). Forces a clone if the presenter is still on the shared asset -- targeting a
		/// render texture on a <see cref="PanelSettings"/> other presenters reference would redirect their output too.
		/// </summary>
		public void SetTargetTexture(RenderTexture texture)
		{
			if (_toolkitFeature == null || _toolkitFeature.Document == null)
			{
				return;
			}

			if (_clone == null)
			{
				if (_sharedAsset == null)
				{
					Debug.LogError($"{nameof(UiPanelSettingsFeature)} on '{name}': cannot set a target texture, no PanelSettings available.", this);
					return;
				}

				CreateClone();
				ApplyOverrides();
				MirrorSortingOrder();
			}

			_clone.targetTexture = texture;
			_clone.renderMode = texture != null ? PanelRenderMode.WorldSpace : PanelRenderMode.ScreenSpaceOverlay;
		}

		private void CreateClone()
		{
			_clone = UnityEngine.Object.Instantiate(_sharedAsset);
			_clone.name = _sharedAsset.name + " (Clone)";

			// Assign the theme before handing the panel to the document -- otherwise Unity logs the
			// "No Theme Style Sheet set" warning once here and again when the GameObject activates.
			// Object.Instantiate already copies themeStyleSheet, but this stays correct if that ever changes.
			_clone.themeStyleSheet = _sharedAsset.themeStyleSheet;

			_toolkitFeature.Document.panelSettings = _clone;
		}

		private void ApplyOverrides()
		{
			if (_clone == null)
			{
				if (_overrides.HasAnyOverride)
				{
					Debug.LogWarning($"{nameof(UiPanelSettingsFeature)} on '{name}': overrides are set but" +
						$" {nameof(UiPanelSettingsClonePolicy)} is {UiPanelSettingsClonePolicy.UseSharedAsset}," +
						" so they will not be applied (would mutate the shared PanelSettings asset).", this);
				}
				return;
			}

			if (_overrides.OverrideClearColor) _clone.clearColor = _overrides.ClearColor;
			if (_overrides.OverrideColorClearValue) _clone.colorClearValue = _overrides.ColorClearValue;
			if (_overrides.OverrideClearDepthStencil) _clone.clearDepthStencil = _overrides.ClearDepthStencil;
			if (_overrides.OverrideTargetDisplay) _clone.targetDisplay = _overrides.TargetDisplay;
			if (_overrides.OverrideRenderMode) _clone.renderMode = _overrides.RenderMode;
			if (_overrides.OverrideScaleMode) _clone.scaleMode = _overrides.ScaleMode;
			if (_overrides.OverrideReferenceResolution) _clone.referenceResolution = _overrides.ReferenceResolution;
			if (_overrides.OverrideForceGammaRendering) _clone.forceGammaRendering = _overrides.ForceGammaRendering;
		}

		private void MirrorSortingOrder()
		{
			if (_clone == null || _toolkitFeature == null || _toolkitFeature.Document == null)
			{
				return;
			}

			_clone.sortingOrder = _toolkitFeature.Document.sortingOrder;
		}

		private void OnDestroy()
		{
			if (_toolkitFeature != null && _toolkitFeature.Document != null && _sharedAsset != null && _clone != null)
			{
				_toolkitFeature.Document.panelSettings = _sharedAsset;
			}

			if (_clone != null)
			{
				UnityEngine.Object.Destroy(_clone);
				_clone = null;
			}
		}
	}
}
