using System;
using System.Collections.Generic;
using GameLovers.UiService.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Examples.Editor
{
	/// <summary>
	/// Adds <see cref="UiBackdropBlurRendererFeature"/> to every Universal Renderer asset the active URP
	/// pipeline uses, so the sample's blur works on first import instead of silently doing nothing.
	/// </summary>
	/// <remarks>
	/// Replicates URP's own <c>ScriptableRendererDataEditor.AddComponent</c>, which is internal: the
	/// feature has to be added as a sub-asset AND registered in the renderer's <c>m_RendererFeatureMap</c>
	/// by local file id, or the list deserializes with a null entry.
	/// </remarks>
	public static class UrpRenderingSampleSetup
	{
		private const string MenuPath = "Tools/GameLovers/Samples/Urp Rendering/Add Backdrop Blur Renderer Feature";

		[MenuItem(MenuPath)]
		public static void MenuAddRendererFeature() => Run(silent: false);

		internal static void RunSilent() => Run(silent: true);

		[InitializeOnLoadMethod]
		private static void OnDomainReload()
		{
			// The post-processor below compiles AFTER the import batch that delivers it, so it misses its
			// own first invocation. Asset edits are also unsafe during the InitializeOnLoad phase.
			EditorApplication.delayCall += RunSilent;
		}

		private static void Run(bool silent)
		{
			if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline)
			{
				if (!silent)
				{
					Debug.LogWarning("Urp Rendering sample: the active render pipeline is not URP, so there is" +
						" no Renderer asset to add the backdrop blur feature to.");
				}

				return;
			}

			var rendererDataList = FindRendererData(pipeline);
			if (rendererDataList.Count == 0)
			{
				if (!silent)
				{
					Debug.LogWarning("Urp Rendering sample: could not resolve any Renderer asset from the active" +
						$" URP pipeline asset '{pipeline.name}'.");
				}

				return;
			}

			var added = 0;
			foreach (var rendererData in rendererDataList)
			{
				if (TryAddFeature(rendererData))
				{
					added++;
				}
			}

			if (added > 0)
			{
				AssetDatabase.SaveAssets();
				Debug.Log($"Urp Rendering sample: added {nameof(UiBackdropBlurRendererFeature)} to {added}" +
					" Renderer asset(s). The sample's blurred modal will now render.");
			}
			else if (!silent)
			{
				Debug.Log($"Urp Rendering sample: {nameof(UiBackdropBlurRendererFeature)} is already present on" +
					" every Renderer asset -- nothing to do.");
			}
		}

		private static List<ScriptableRendererData> FindRendererData(UniversalRenderPipelineAsset pipeline)
		{
			// m_RendererDataList is not public API; reading it through SerializedObject avoids depending on
			// URP internals that move between versions.
			var result = new List<ScriptableRendererData>();
			var serializedPipeline = new SerializedObject(pipeline);
			var listProperty = serializedPipeline.FindProperty("m_RendererDataList");

			if (listProperty == null || !listProperty.isArray)
			{
				return result;
			}

			for (var i = 0; i < listProperty.arraySize; i++)
			{
				if (listProperty.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableRendererData data)
				{
					result.Add(data);
				}
			}

			return result;
		}

		private static bool TryAddFeature(ScriptableRendererData rendererData)
		{
			var serializedData = new SerializedObject(rendererData);
			var featuresProperty = serializedData.FindProperty("m_RendererFeatures");
			var mapProperty = serializedData.FindProperty("m_RendererFeatureMap");

			if (featuresProperty == null || mapProperty == null)
			{
				return false;
			}

			for (var i = 0; i < featuresProperty.arraySize; i++)
			{
				if (featuresProperty.GetArrayElementAtIndex(i).objectReferenceValue is UiBackdropBlurRendererFeature)
				{
					return false;
				}
			}

			var feature = ScriptableObject.CreateInstance<UiBackdropBlurRendererFeature>();
			feature.name = nameof(UiBackdropBlurRendererFeature);
			feature.hideFlags |= HideFlags.HideInHierarchy;

			if (EditorUtility.IsPersistent(rendererData))
			{
				AssetDatabase.AddObjectToAsset(feature, rendererData);
			}

			AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

			featuresProperty.arraySize++;
			featuresProperty.GetArrayElementAtIndex(featuresProperty.arraySize - 1).objectReferenceValue = feature;

			mapProperty.arraySize++;
			mapProperty.GetArrayElementAtIndex(mapProperty.arraySize - 1).longValue = localId;

			serializedData.ApplyModifiedProperties();
			EditorUtility.SetDirty(rendererData);
			return true;
		}
	}

	internal sealed class UrpRenderingSampleAssetPostprocessor : AssetPostprocessor
	{
		private const string MarkerSegment = "/UrpRendering/";

		private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
		{
			foreach (var path in imported)
			{
				if (path.IndexOf(MarkerSegment, StringComparison.Ordinal) >= 0)
				{
					EditorApplication.delayCall += UrpRenderingSampleSetup.RunSilent;
					return;
				}
			}
		}
	}
}
