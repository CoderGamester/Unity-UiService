using Cysharp.Threading.Tasks;

namespace GameLovers.UiService
{
	/// <summary>
	/// Interface for presenter features that provide open/close transition delays.
	/// Features implementing this interface can be awaited by the presenter to coordinate transitions.
	/// </summary>
	public interface ITransitionFeature
	{
		/// <summary>
		/// Task that completes when the open transition finishes.
		/// Returns <see cref="UniTask.CompletedTask"/> if no open transition is active.
		/// </summary>
		UniTask OpenTransitionTask { get; }

		/// <summary>
		/// Task that completes when the close transition finishes.
		/// Returns <see cref="UniTask.CompletedTask"/> if no close transition is active.
		/// </summary>
		UniTask CloseTransitionTask { get; }
	}
}

