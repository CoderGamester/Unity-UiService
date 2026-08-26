using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameLovers.UiService
{
	/// <summary>
	/// Prepares EventSystem, uGUI raycaster, and world-space physics-raycaster prerequisites.
	/// </summary>
	public sealed class EventSystemUiInputRouter : IUiInputRouter
	{
		private readonly Action<GameObject> _inputModuleInstaller;
		private readonly Func<Camera> _worldCameraResolver;

		private EventSystem _eventSystem;

		public EventSystemUiInputRouter(
			Action<GameObject> inputModuleInstaller = null,
			Func<Camera> worldCameraResolver = null)
		{
			_inputModuleInstaller = inputModuleInstaller;
			_worldCameraResolver = worldCameraResolver;
		}

		/// <inheritdoc />
		public void Prepare(UiPresenter presenter, IUiSurface surface)
		{
			EventSystem eventSystem = EnsureEventSystem();
			if (eventSystem.GetComponent<BaseInputModule>() == null)
			{
				Debug.LogError(
					$"EventSystem '{eventSystem.gameObject.name}' has no {nameof(BaseInputModule)}. " +
					"Provide an input-module installer matching the consumer's active input backend.",
					eventSystem);
			}

			Canvas canvas = presenter.GetComponentInChildren<Canvas>(true);
			if (canvas != null)
			{
				if (canvas.GetComponent<GraphicRaycaster>() == null)
				{
					canvas.gameObject.AddComponent<GraphicRaycaster>();
				}

				if (surface.SurfaceSpace == UiSurfaceSpace.World && canvas.worldCamera == null)
				{
					canvas.worldCamera = ResolveWorldCamera(presenter);
				}
			}

			if (surface.SurfaceSpace != UiSurfaceSpace.World)
			{
				return;
			}

			Camera camera = canvas != null && canvas.worldCamera != null
				? canvas.worldCamera
				: ResolveWorldCamera(presenter);
			if (camera.GetComponent<PhysicsRaycaster>() == null)
			{
				camera.gameObject.AddComponent<PhysicsRaycaster>();
			}

			if (canvas == null && presenter.GetComponentInChildren<MeshCollider>(true) == null)
			{
				Debug.LogError(
					$"World-space presenter '{presenter.gameObject.name}' has no {nameof(MeshCollider)} for pointer input.",
					presenter);
			}
		}

		private EventSystem EnsureEventSystem()
		{
			if (_eventSystem != null)
			{
				return _eventSystem;
			}

			_eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
			if (_eventSystem != null)
			{
				return _eventSystem;
			}

			var eventSystemObject = new GameObject("[UiService] EventSystem");
			_eventSystem = eventSystemObject.AddComponent<EventSystem>();
			_inputModuleInstaller?.Invoke(eventSystemObject);
			Object.DontDestroyOnLoad(eventSystemObject);
			return _eventSystem;
		}

		private Camera ResolveWorldCamera(UiPresenter presenter)
		{
			Camera camera = _worldCameraResolver?.Invoke();
			if (camera == null)
			{
				camera = Camera.main;
			}

			if (camera != null)
			{
				return camera;
			}

			throw new InvalidOperationException(
				$"World-space presenter '{presenter.gameObject.name}' requires a camera for pointer input.");
		}
	}
}
