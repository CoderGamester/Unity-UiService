using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using RiveAsset = global::Rive.Asset;
using RiveFile = global::Rive.File;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Owns an Addressables Rive asset handle and the decoded file created from it.
	/// </summary>
	public sealed class RiveAddressableFileLease : IDisposable
	{
		private AsyncOperationHandle<RiveAsset> _assetHandle;
		private bool _disposed;

		/// <summary>Gets the decoded Rive file owned by this lease.</summary>
		public RiveFile File { get; }

		private RiveAddressableFileLease(
			AsyncOperationHandle<RiveAsset> assetHandle,
			RiveFile file)
		{
			_assetHandle = assetHandle;
			File = file;
		}

		/// <summary>
		/// Loads an imported Rive asset and decodes one cached file instance.
		/// </summary>
		/// <param name="address">The Addressables key for the imported Rive asset.</param>
		/// <param name="cancellationToken">Cancellation token for the load.</param>
		/// <returns>A lease that owns both resources.</returns>
		public static async UniTask<RiveAddressableFileLease> LoadAsync(
			string address,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				throw new ArgumentException("A Rive Addressables key is required.", nameof(address));
			}

			var handle = Addressables.LoadAssetAsync<RiveAsset>(address);
			try
			{
				while (!handle.IsDone)
				{
					cancellationToken.ThrowIfCancellationRequested();
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				}

				if (handle.Status != AsyncOperationStatus.Succeeded)
				{
					throw handle.OperationException ??
					      new InvalidOperationException($"Failed to load Rive asset '{address}'.");
				}

				RiveFile file = RiveFile.Load(handle.Result);
				if (file == null)
				{
					throw new InvalidOperationException($"Rive asset '{address}' could not be decoded.");
				}

				return new RiveAddressableFileLease(handle, file);
			}
			catch
			{
				if (handle.IsValid())
				{
					Addressables.Release(handle);
				}

				throw;
			}
		}

		/// <summary>
		/// Disposes the decoded file before releasing its backing Addressables asset.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			File?.Dispose();
			if (_assetHandle.IsValid())
			{
				Addressables.Release(_assetHandle);
				_assetHandle = default;
			}
		}
	}
}
