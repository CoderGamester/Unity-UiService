using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Default <see cref="IUiBaseCameraProvider"/>: <see cref="Camera.main"/>, cached and
	/// invalidated on scene load. The "Ui" root <see cref="UiService"/> creates is
	/// DontDestroyOnLoad, but scene cameras are not -- a stacked presenter surviving a scene
	/// load must re-resolve rather than hold a stale/destroyed reference.
	/// </summary>
	public class UiBaseCameraProvider : IUiBaseCameraProvider
	{
		private static UiBaseCameraProvider _default;

		/// <summary>Shared instance used by <see cref="UiCameraStackFeature"/> unless overridden.</summary>
		public static UiBaseCameraProvider Default => _default ??= new UiBaseCameraProvider();

		private Camera _cached;
		private bool _subscribed;

		/// <inheritdoc />
		public Camera GetBaseCamera()
		{
			EnsureSubscribed();

			if (_cached == null)
			{
				_cached = Camera.main;
			}

			return _cached;
		}

		private void EnsureSubscribed()
		{
			if (_subscribed)
			{
				return;
			}

			SceneManager.sceneLoaded += OnSceneLoaded;
			_subscribed = true;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			_cached = null;
		}
	}
}
