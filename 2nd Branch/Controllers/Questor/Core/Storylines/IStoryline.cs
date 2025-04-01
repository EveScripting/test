using EVESharpCore.Questor.States;

namespace EVESharpCore.Questor.Storylines
{
    public interface IStoryline
    {
        #region Methods

        StorylineState Arm(Storyline storyline);

        StorylineState BeforeGotoAgent(Storyline storyline);

        StorylineState ExecuteMission(Storyline storyline);

        StorylineState PreAcceptMission(Storyline storyline);

        #endregion Methods
    }
}