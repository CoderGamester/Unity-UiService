using GameLovers.UiService.Rive;

namespace GameLovers.UiService.Samples.RiveScreen
{
	public sealed class RiveLoadingSamplePresenter : RiveScreenPresenter
	{
		private const string ProgressPath = "progress";

		public void SetProgress(float progress)
		{
			Binding.SetNumber(ProgressPath, progress);
		}
	}
}
