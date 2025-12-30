using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <inheritdoc />
	public class AddressablesUiAssetLoader : IUiAssetLoader
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