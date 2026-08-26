using Rive.Components;
using UnityEngine;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Provides the common Rive component references used by world-space presenters.
	/// </summary>
	public abstract class RiveWorldPresenter : UiPresenter
	{
		[SerializeField]
		private RivePanel _panel;
		[SerializeField]
		private RiveTextureRenderer _textureRenderer;
		[SerializeField]
		private RiveWidget _widget;
		[SerializeField]
		private RiveViewModelBinding _binding;

		/// <summary>Gets the panel rendering this presenter.</summary>
		protected RivePanel Panel => _panel;

		/// <summary>Gets the mesh renderer displaying the panel texture.</summary>
		protected RiveTextureRenderer TextureRenderer => _textureRenderer;

		/// <summary>Gets the widget containing this presenter's artboard.</summary>
		protected RiveWidget Widget => _widget;

		/// <summary>Gets the view-model binding used by this presenter.</summary>
		protected RiveViewModelBinding Binding => _binding;
	}

	/// <summary>
	/// Provides Rive component references to a data-driven world-space presenter.
	/// </summary>
	/// <typeparam name="T">The presenter data supplied when the UI opens.</typeparam>
	public abstract class RiveWorldPresenter<T> : UiPresenter<T> where T : struct
	{
		[SerializeField]
		private RivePanel _panel;
		[SerializeField]
		private RiveTextureRenderer _textureRenderer;
		[SerializeField]
		private RiveWidget _widget;
		[SerializeField]
		private RiveViewModelBinding _binding;

		/// <summary>Gets the panel rendering this presenter.</summary>
		protected RivePanel Panel => _panel;

		/// <summary>Gets the mesh renderer displaying the panel texture.</summary>
		protected RiveTextureRenderer TextureRenderer => _textureRenderer;

		/// <summary>Gets the widget containing this presenter's artboard.</summary>
		protected RiveWidget Widget => _widget;

		/// <summary>Gets the view-model binding used by this presenter.</summary>
		protected RiveViewModelBinding Binding => _binding;
	}
}
