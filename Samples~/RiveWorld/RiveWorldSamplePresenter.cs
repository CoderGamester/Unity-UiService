using GameLovers.UiService.Rive;

namespace GameLovers.UiService.Samples.RiveWorld
{
	public sealed class RiveWorldSamplePresenter : RiveWorldPresenter
	{
		private const string LabelPath = "label";

		public void SetLabel(string label)
		{
			Binding.SetString(LabelPath, label);
		}
	}
}
