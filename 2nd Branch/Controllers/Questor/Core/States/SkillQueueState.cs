namespace EVESharpCore.Questor.States
{
    public enum SkillQueueState
    {
        Idle,
        Begin,
        Done,
        LoadPlan,
        ReadCharacterSheetSkills,
        AreThereSkillsReadyToInject,
        CheckTrainingQueue,
        Error,
        CloseQuestor,
        GenerateInnerspaceProfile,
        BuyingSkill
    }

    public static class _State
    {
        #region Properties

        public static SkillQueueState CurrentSkillQueueState { get; set; }

        #endregion Properties
    }
}