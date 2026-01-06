using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Custom feature that plays sounds on UI lifecycle events.
	/// Demonstrates a simple feature that doesn't require coroutines.
	/// 
	/// Usage:
	/// 1. Add this component to your presenter prefab
	/// 2. Add an AudioSource component (or let it auto-create)
	/// 3. Assign sound clips in the inspector
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class SoundFeature : PresenterFeatureBase
	{
		[Header("Sound Clips")]
		[SerializeField] private AudioClip _openSound;
		[SerializeField] private AudioClip _closeSound;
		
		[Header("Settings")]
		[SerializeField] [Range(0f, 1f)] private float _volume = 1f;
		[SerializeField] private AudioSource _audioSource;

		private void OnValidate()
		{
			_audioSource = _audioSource ?? GetComponent<AudioSource>();
		}

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			
			if (_audioSource == null)
			{
				_audioSource = GetComponent<AudioSource>();
			}
			
			// Configure audio source for UI sounds
			if (_audioSource != null)
			{
				_audioSource.playOnAwake = false;
				_audioSource.spatialBlend = 0f; // 2D sound
			}
			
			Debug.Log("[SoundFeature] Initialized");
		}

		public override void OnPresenterOpened()
		{
			PlaySound(_openSound);
			Debug.Log("[SoundFeature] Played open sound");
		}

		public override void OnPresenterClosing()
		{
			PlaySound(_closeSound);
			Debug.Log("[SoundFeature] Played close sound");
		}

		private void PlaySound(AudioClip clip)
		{
			if (clip == null || _audioSource == null) return;
			
			_audioSource.PlayOneShot(clip, _volume);
		}
	}
}

