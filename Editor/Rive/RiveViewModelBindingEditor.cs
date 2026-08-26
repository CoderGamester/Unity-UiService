using System.Collections.Generic;
using GameLovers.UiService.Rive;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using RiveArtboard = global::Rive.Artboard;
using RiveFile = global::Rive.File;
using RiveViewModelInstance = global::Rive.ViewModelInstance;

namespace GameLoversEditor.UiService.Rive
{
	/// <summary>Validates declared Rive view-model paths in the binding inspector.</summary>
	[CustomEditor(typeof(RiveViewModelBinding))]
	internal sealed class RiveViewModelBindingEditor : UnityEditor.Editor
	{
		/// <inheritdoc />
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			InspectorElement.FillDefaultInspector(root, serializedObject, this);

			var binding = (RiveViewModelBinding)target;
			List<string> errors = Validate(binding);
			if (errors.Count > 0)
			{
				root.Add(new HelpBox(string.Join("\n", errors), HelpBoxMessageType.Error));
			}

			return root;
		}

		private static List<string> Validate(RiveViewModelBinding binding)
		{
			var errors = new List<string>();
			if (binding.Widget == null || binding.Widget.Asset == null)
			{
				if (binding.DeclaredProperties.Count > 0)
				{
					errors.Add("Declared Rive properties require a widget with an imported Rive asset.");
				}

				return errors;
			}

			RiveFile file = null;
			RiveArtboard artboard = null;
			RiveViewModelInstance instance = null;
			try
			{
				file = RiveFile.Load(binding.Widget.Asset);
				artboard = string.IsNullOrEmpty(binding.Widget.ArtboardName)
					? file.Artboard(0)
					: file.Artboard(binding.Widget.ArtboardName);
				if (artboard == null || artboard.DefaultViewModel == null)
				{
					errors.Add(
						$"Artboard '{binding.Widget.ArtboardName}' has no default view model for validation.");
					return errors;
				}

				instance = artboard.DefaultViewModel.CreateDefaultInstance() ??
				           artboard.DefaultViewModel.CreateInstance();
				for (int i = 0; i < binding.DeclaredProperties.Count; i++)
				{
					RivePropertyReference reference = binding.DeclaredProperties[i];
					if (!PropertyExists(instance, reference))
					{
						errors.Add(
							$"Artboard '{artboard.Name}' is missing {reference.Type} property '{reference.Path}'.");
					}
				}
			}
			finally
			{
				instance?.Dispose();
				artboard?.Dispose();
				file?.Dispose();
			}

			return errors;
		}

		private static bool PropertyExists(
			RiveViewModelInstance instance,
			RivePropertyReference reference)
		{
			switch (reference.Type)
			{
				case RivePropertyType.Number:
					return instance.GetNumberProperty(reference.Path) != null;
				case RivePropertyType.Boolean:
					return instance.GetBooleanProperty(reference.Path) != null;
				case RivePropertyType.String:
					return instance.GetStringProperty(reference.Path) != null;
				case RivePropertyType.Color:
					return instance.GetColorProperty(reference.Path) != null;
				case RivePropertyType.Trigger:
					return instance.GetTriggerProperty(reference.Path) != null;
				default:
					return false;
			}
		}
	}
}
