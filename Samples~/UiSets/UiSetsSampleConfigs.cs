using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Sample-specific UiConfigs that uses <see cref="UiSetId"/> for UI set management.
	/// This class exists to allow a custom editor (<see cref="GameLoversEditor.UiService.Examples.UiSetsConfigsEditor"/>)
	/// to display set IDs using the sample's enum names instead of raw integers.
	/// 
	/// In your own project, you can either:
	/// 1. Create a similar subclass for your own enum (recommended for clarity)
	/// 2. Or create a CustomEditor for the base PrefabRegistryUiConfigs with your enum
	/// </summary>
	[CreateAssetMenu(fileName = "UiSetsSampleConfigs", menuName = "GameLovers/UiService/Samples/UiSets Sample Configs")]
	public class UiSetsSampleConfigs : PrefabRegistryUiConfigs
	{
		// No additional implementation needed - this class exists solely for the custom editor targeting
	}
}

