using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

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

	/// <inheritdoc />
	public class UiAssetLoader : IUiAssetLoader
	{
	/// <inheritdoc />
	public async UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default)
	{
		var operation = Addressables.InstantiateAsync(config.AddressableAddress, new InstantiationParameters(parent, false));

		if(config.LoadSynchronously)
		{
			operation.WaitForCompletion();
		}
		else
		{
			await operation.ToUniTask(cancellationToken: cancellationToken);
		}

		if (operation.Status != AsyncOperationStatus.Succeeded)
		{
			throw operation.OperationException;
		}

		return operation.Result;
	}

		/// <inheritdoc />
		public void UnloadAsset(GameObject asset)
		{
			Addressables.ReleaseInstance(asset);
		}
	}
}