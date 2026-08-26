using System.Collections.Generic;
using Rive.Components;
using UnityEngine;
using RiveBooleanProperty = global::Rive.ViewModelInstanceBooleanProperty;
using RiveColorProperty = global::Rive.ViewModelInstanceColorProperty;
using RiveNumberProperty = global::Rive.ViewModelInstanceNumberProperty;
using RiveStringProperty = global::Rive.ViewModelInstanceStringProperty;
using RiveTriggerProperty = global::Rive.ViewModelInstanceTriggerProperty;
using RiveViewModelInstance = global::Rive.ViewModelInstance;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Caches typed Rive view-model properties and scopes widget subscriptions to presenter visibility.
	/// </summary>
	public sealed class RiveViewModelBinding : PresenterFeatureBase
	{
		[SerializeField]
		private RiveWidget _widget;
		[SerializeField]
		private List<RivePropertyReference> _declaredProperties = new List<RivePropertyReference>();

		private readonly Dictionary<string, RiveNumberProperty> _numberProperties =
			new Dictionary<string, RiveNumberProperty>();
		private readonly Dictionary<string, RiveBooleanProperty> _booleanProperties =
			new Dictionary<string, RiveBooleanProperty>();
		private readonly Dictionary<string, RiveStringProperty> _stringProperties =
			new Dictionary<string, RiveStringProperty>();
		private readonly Dictionary<string, RiveColorProperty> _colorProperties =
			new Dictionary<string, RiveColorProperty>();
		private readonly Dictionary<string, RiveTriggerProperty> _triggerProperties =
			new Dictionary<string, RiveTriggerProperty>();

		private RiveViewModelInstance _instance;
		private bool _subscribed;

		/// <summary>Gets whether a bound view-model instance is available.</summary>
		public bool IsReady => ResolveInstance();

		/// <summary>Gets the widget that owns the bound view model.</summary>
		public RiveWidget Widget => _widget;

		/// <summary>Gets property paths declared for edit-time validation.</summary>
		public IReadOnlyList<RivePropertyReference> DeclaredProperties => _declaredProperties;

		private void OnDestroy()
		{
			Unsubscribe();
			ClearCache();
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			if (_widget == null)
			{
				_widget = GetComponentInChildren<RiveWidget>(true);
			}
		}

		/// <inheritdoc />
		public override void OnPresenterOpened()
		{
			Subscribe();
			ResolveInstance();
		}

		/// <inheritdoc />
		public override void OnPresenterClosed()
		{
			Unsubscribe();
			ClearCache();
		}

		/// <summary>
		/// Writes a number property by its Rive view-model path.
		/// </summary>
		/// <param name="path">The property path authored in Rive.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>True when the property was found and updated.</returns>
		public bool SetNumber(string path, float value)
		{
			if (!TryGetProperty(path, _numberProperties, instance => instance.GetNumberProperty(path), out var property))
			{
				return false;
			}

			property.Value = value;
			return true;
		}

		/// <summary>
		/// Writes a boolean property by its Rive view-model path.
		/// </summary>
		/// <param name="path">The property path authored in Rive.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>True when the property was found and updated.</returns>
		public bool SetBoolean(string path, bool value)
		{
			if (!TryGetProperty(path, _booleanProperties, instance => instance.GetBooleanProperty(path), out var property))
			{
				return false;
			}

			property.Value = value;
			return true;
		}

		/// <summary>
		/// Writes a string property by its Rive view-model path.
		/// </summary>
		/// <param name="path">The property path authored in Rive.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>True when the property was found and updated.</returns>
		public bool SetString(string path, string value)
		{
			if (!TryGetProperty(path, _stringProperties, instance => instance.GetStringProperty(path), out var property))
			{
				return false;
			}

			property.Value = value;
			return true;
		}

		/// <summary>
		/// Writes a color property by its Rive view-model path.
		/// </summary>
		/// <param name="path">The property path authored in Rive.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>True when the property was found and updated.</returns>
		public bool SetColor(string path, Color value)
		{
			if (!TryGetProperty(path, _colorProperties, instance => instance.GetColorProperty(path), out var property))
			{
				return false;
			}

			property.Value = value;
			return true;
		}

		/// <summary>
		/// Fires a trigger property by its Rive view-model path.
		/// </summary>
		/// <param name="path">The property path authored in Rive.</param>
		/// <returns>True when the property was found and fired.</returns>
		public bool FireTrigger(string path)
		{
			if (!TryGetProperty(path, _triggerProperties, instance => instance.GetTriggerProperty(path), out var property))
			{
				return false;
			}

			property.Trigger();
			return true;
		}

		private bool ResolveInstance()
		{
			if (_instance != null)
			{
				return true;
			}

			if (_widget == null ||
				_widget.Status != WidgetStatus.Loaded ||
				_widget.StateMachine == null)
			{
				return false;
			}

			_instance = _widget.StateMachine.ViewModelInstance;
			return _instance != null;
		}

		private bool TryGetProperty<T>(
			string path,
			IDictionary<string, T> cache,
			System.Func<RiveViewModelInstance, T> resolver,
			out T property) where T : class
		{
			if (cache.TryGetValue(path, out property))
			{
				return true;
			}

			if (!ResolveInstance())
			{
				property = null;
				return false;
			}

			property = resolver(_instance);
			if (property == null)
			{
				Debug.LogWarning(
					$"Rive view-model property '{path}' was not found on widget '{_widget.name}'.",
					this);
				return false;
			}

			cache.Add(path, property);
			return true;
		}

		private void Subscribe()
		{
			if (_subscribed || _widget == null)
			{
				return;
			}

			_widget.OnWidgetStatusChanged += OnWidgetStatusChanged;
			_subscribed = true;
		}

		private void Unsubscribe()
		{
			if (!_subscribed || _widget == null)
			{
				return;
			}

			_widget.OnWidgetStatusChanged -= OnWidgetStatusChanged;
			_subscribed = false;
		}

		private void OnWidgetStatusChanged()
		{
			ClearCache();
			ResolveInstance();
		}

		private void ClearCache()
		{
			_instance = null;
			_numberProperties.Clear();
			_booleanProperties.Clear();
			_stringProperties.Clear();
			_colorProperties.Clear();
			_triggerProperties.Clear();
		}
	}
}
