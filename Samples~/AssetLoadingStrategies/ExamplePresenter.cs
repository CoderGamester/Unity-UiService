using UnityEngine;
using GameLovers.UiService;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Simple presenter for demonstrating different loading strategies.
	/// </summary>
	public class ExamplePresenter : UiPresenter
	{
		protected override void OnInitialized()
		{
			Debug.Log($"[{nameof(ExamplePresenter)}] Initialized");
		}

		protected override void OnOpened()
		{
			Debug.Log($"[{nameof(ExamplePresenter)}] Opened");
		}

		protected override void OnClosed()
		{
			Debug.Log($"[{nameof(ExamplePresenter)}] Closed");
		}
	}
}

