using System;
using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>Resolves placement against service-owned roots or an explicit runtime target.</summary>
	internal sealed class DefaultUiPlacement : IUiPlacement
	{
		private readonly Transform _screenRoot;
		private Transform _worldRoot;

		internal DefaultUiPlacement(Transform screenRoot)
		{
			_screenRoot = screenRoot;
		}

		/// <inheritdoc />
		public Transform Resolve(UiPlacementSpace placement, Transform followTarget)
		{
			switch (placement)
			{
				case UiPlacementSpace.ScreenRoot:
					return _screenRoot;
				case UiPlacementSpace.WorldRoot:
					return ResolveWorldRoot();
				case UiPlacementSpace.FollowTarget:
					return followTarget != null
						? followTarget
						: throw new ArgumentNullException(nameof(followTarget),
							"FollowTarget placement requires a runtime follow target.");
				default:
					throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unsupported UI placement.");
			}
		}

		private Transform ResolveWorldRoot()
		{
			if (_worldRoot != null)
			{
				return _worldRoot;
			}

			_worldRoot = new GameObject("WorldUi").transform;
			_worldRoot.SetParent(_screenRoot, false);
			return _worldRoot;
		}
	}
}
