using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Keeps a presenter at a world-space offset from an authored or runtime-assigned target.
	/// </summary>
	public class UiFollowTargetFeature : PresenterFeatureBase
	{
		[SerializeField] private Transform _target;
		[SerializeField] private Vector3 _offset;

		/// <summary>The transform followed by this presenter.</summary>
		public Transform Target
		{
			get => _target;
			set
			{
				_target = value;
				UpdatePosition();
			}
		}

		/// <summary>The world-space offset preserved from the followed target.</summary>
		public Vector3 Offset
		{
			get => _offset;
			set
			{
				_offset = value;
				UpdatePosition();
			}
		}

		private void LateUpdate()
		{
			UpdatePosition();
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			UpdatePosition();
		}

		private void UpdatePosition()
		{
			if (_target == null)
			{
				return;
			}

			transform.position = _target.position + _offset;
		}
	}
}
