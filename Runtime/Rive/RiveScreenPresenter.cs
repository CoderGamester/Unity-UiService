using Rive.Components;
using UnityEngine;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Provides the common Rive component references used by screen-space presenters.
	/// </summary>
	public abstract class RiveScreenPresenter : UiPresenter
	{
		[SerializeField]
		private RivePanel _panel;
		[SerializeField]
		private RiveWidget _widget;
		[SerializeField]
		private RiveViewModelBinding _binding;

		/// <summary>Gets the panel rendering this presenter.</summary>
		protected RivePanel Panel => _panel;

		/// <summary>Gets the widget containing this presenter's artboard.</summary>
		protected RiveWidget Widget => _widget;

		/// <summary>Gets the view-model binding used by this presenter.</summary>
		protected RiveViewModelBinding Binding => _binding;
	}

	/// <summary>
	/// Provides Rive component references to a data-driven screen-space presenter.
	/// </summary>
	/// <typeparam name="T">The presenter data supplied when the UI opens.</typeparam>
	public abstract class RiveScreenPresenter<T> : UiPresenter<T> where T : struct
	{
		[SerializeField]
		private RivePanel _panel;
		[SerializeField]
		private RiveWidget _widget;
		[SerializeField]
		private RiveViewModelBinding _binding;

		/// <summary>Gets the panel rendering this presenter.</summary>
		protected RivePanel Panel => _panel;

		/// <summary>Gets the widget containing this presenter's artboard.</summary>
		protected RiveWidget Widget => _widget;

		/// <summary>Gets the view-model binding used by this presenter.</summary>
		protected RiveViewModelBinding Binding => _binding;
	}
}
