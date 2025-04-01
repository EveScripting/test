extern alias SC;

using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    public class DirectFleetAbyssActivationWindow : DirectWindow
    {
        #region Constructors

        internal DirectFleetAbyssActivationWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Properties

        //public bool AnyError => Controller.Attribute("errors").ToList().Any();
        public bool IsActivating => ActivationController.Attribute("isActivating").ToBool();
        public bool CoOpIsActivating => CoOpActivationController.Attribute("isActivating").ToBool();
        public bool IsFinished => ActivationController.Attribute("isFinished").ToBool();
        public bool CoOpIsFinished => CoOpActivationController.Attribute("isFinished").ToBool();
        public bool IsJumping => ActivationController.Attribute("isJumping").ToBool();
        public bool CoOpIsJumping => CoOpActivationController.Attribute("isJumping").ToBool();
        public bool IsReady => ActivationController.Attribute("isReady").ToBool();
        public bool CoOpIsReady => CoOpActivationController.Attribute("isReady").ToBool();
        public float Tier => Controller.Attribute("tier").ToFloat();
        public string TierDescription => Controller.Attribute("tierDescription").ToUnicodeString();
        public string TimerDescription => Controller.Attribute("timerDescription").ToUnicodeString();
        public int TypeId => Controller.Attribute("typeID").ToInt();
        public float Weather => Controller.Attribute("weather").ToInt();
        public string WeatherDescription => Controller.Attribute("weatherDescription").ToUnicodeString();
        public string WeatherName => Controller.Attribute("weatherName").ToUnicodeString();
        private PyObject ActivationController => Controller.Attribute("activationController");
        private PyObject CoOpActivationController => Controller.Attribute("coOpActivationController");
        private PyObject Controller => PyWindow.Attribute("controller");

        #endregion Properties

        #region Methods

        public bool Activate()
        {
            if (IsJumping || IsFinished || IsActivating || !IsReady)
                return false;

            return DirectEve.ThreadedCall(ActivationController.Attribute("Activate"));
        }

        public bool FleetActivate()
        {
            if (IsJumping || IsFinished || IsActivating || !IsReady || !DirectEve.Session.InFleet )
                return false;

            return false;
            //return DirectEve.ThreadedCall(CoOpActivationController.Attribute("Activate"));
        }

        /**
        private static DirectAgentResponse requestButton(DirectAgent myAgent)
        {
            //if (!IsAgentWindowReady()) return null;
            string textToLookForInAgentWindow = strRequestMission;
            DirectAgentResponse _requestButton = FindAgentResponse(textToLookForInAgentWindow, myAgent);
            if (_requestButton == null)
                ChangeLastButtonPushed(AgentInteractionButton.Request, AgentInteractionButton.None, myAgent);

            return _requestButton;
        }
        **/

        public override string ToString()
        {
            return $"{nameof(Controller)}: {Controller}, {nameof(TypeId)}: {TypeId}, {nameof(Weather)}: {Weather}, {nameof(WeatherDescription)}: {WeatherDescription}, {nameof(WeatherName)}: {WeatherName}, {nameof(Tier)}: {Tier}, {nameof(TierDescription)}: {TierDescription}, {nameof(TimerDescription)}: {TimerDescription}, {nameof(IsReady)}: {IsReady}, {nameof(IsJumping)}: {IsJumping}, {nameof(IsFinished)}: {IsFinished}, {nameof(IsActivating)}: {IsActivating}";
        }

        #endregion Methods
    }
}