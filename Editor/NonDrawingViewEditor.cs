using GameLovers.UiService.Views;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// <see cref="NonDrawingView"/> custom inspector
	/// </summary>
	[CanEditMultipleObjects, CustomEditor(typeof(NonDrawingView), false)]
	public class NonDrawingViewEditor : GraphicEditor
	{
		/// <inheritdoc />
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();

			// Add script field
			var scriptField = new PropertyField(serializedObject.FindProperty("m_Script"));
			scriptField.SetEnabled(false);
			root.Add(scriptField);

			// Add raycast controls using IMGUI container since it's from base class
			var raycastContainer = new IMGUIContainer(() =>
			{
				serializedObject.Update();
				RaycastControlsGUI();
				serializedObject.ApplyModifiedProperties();
			});
			root.Add(raycastContainer);

			return root;
		}
	}
}