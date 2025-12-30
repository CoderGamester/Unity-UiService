using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// This interface allows to wrap the asset loading scheme into the UI memory
	/// </summary>
	public interface IUiAssetLoader
	{
		/// <summary>
		/// Instantiates the prefab asynchronously with the given <paramref name="config"/> and <paramref name="parent"/>.
		/// </summary>
		/// <param name="config">The UI configuration to instantiate.</param>
		/// <param name="parent">The parent transform to instantiate the prefab under.</param>
		/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
		/// <returns>A task that completes with the instantiated prefab game object.</returns>
		UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default);

		/// <summary>
		/// Unloads the given <paramref name="asset"/> from the game memory
		/// </summary>
		void UnloadAsset(GameObject asset);
	}
}

