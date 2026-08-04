using System;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Represents a unique identifier for a UI presenter instance.
	/// Combines the presenter type with an optional instance address to support multiple instances of the same type.
	/// </summary>
	public readonly struct UiInstanceId : IEquatable<UiInstanceId>
	{
		/// <summary>
		/// The type of the UI presenter
		/// </summary>
		public readonly Type PresenterType;
		
		/// <summary>
		/// Optional instance address. If null or empty, this represents the default/singleton instance.
		/// </summary>
		public readonly string InstanceAddress;

		public UiInstanceId(Type presenterType, string instanceAddress = null)
		{
			PresenterType = presenterType ?? throw new ArgumentNullException(nameof(presenterType));
			InstanceAddress = string.IsNullOrEmpty(instanceAddress) ? string.Empty : instanceAddress;
		}

		/// <summary>
		/// Creates a default instance ID for a presenter type (singleton)
		/// </summary>
		public static UiInstanceId Default(Type presenterType) => new UiInstanceId(presenterType);

		/// <summary>
		/// Creates a named instance ID for a presenter type
		/// </summary>
		public static UiInstanceId Named(Type presenterType, string instanceAddress) => new UiInstanceId(presenterType, instanceAddress);

		/// <summary>
		/// Checks if this is a default/singleton instance (no specific instance address)
		/// </summary>
		public bool IsDefault => string.IsNullOrEmpty(InstanceAddress);

		public bool Equals(UiInstanceId other)
		{
			return PresenterType == other.PresenterType && InstanceAddress == other.InstanceAddress;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is UiInstanceId other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((PresenterType != null ? PresenterType.GetHashCode() : 0) * 397) ^ (InstanceAddress != null ? InstanceAddress.GetHashCode() : 0);
			}
		}

		public static bool operator ==(UiInstanceId left, UiInstanceId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(UiInstanceId left, UiInstanceId right)
		{
			return !left.Equals(right);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return IsDefault ? $"{PresenterType.Name}" : $"{PresenterType.Name}:{InstanceAddress}";
		}
	}
}

