/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 12.12.2016
 * Time: 16:30
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public enum AvailabilityOptions
    {
        PUBLIC = 0,
        PRIVATE = 1
    }

    public enum ContractType
    {
        TYPE_ITEMEXCHANGE = 1,
        TYPE_AUCTION = 2,
        TYPE_COURIER = 3
    }

    public enum ExpireTime
    {
        ONE_DAY = 24 * 60,
        THREE_DAYS = 24 * 60 * 3,
        ONE_WEEK = 24 * 60 * 7,
        TWO_WEEKS = 24 * 60 * 14
    }

    public class CourierDestination
    {
        #region Constructors

        public CourierDestination(int id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion Constructors

        #region Properties

        public int Id { get; set; }
        public string Name { get; set; }

        #endregion Properties
    }

    public class CourierProvider
    {
        #region Constructors

        public CourierProvider(int id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion Constructors

        #region Properties

        public int Id { get; set; }
        public string Name { get; set; }

        #endregion Properties
    }

    /// <summary>
    ///     Description of DirectContract.
    /// </summary>
    public class DirectContract : DirectObject
    {
        //		        def FinishStep2(self):
        //        if hasattr(self.data, 'price'):
        //            self.data.price = int(self.data.price)
        //        if hasattr(self.data, 'reward'):
        //            self.data.reward = int(self.data.reward)
        //        if hasattr(self.data, 'collateral'):
        //            self.data.collateral = int(self.data.collateral)
        //        if len(self.data.description) > MAX_TITLE_LENGTH:

        //    def FinishStep2_CourierContract(self):
        //        if not self.data.endStation and len(self.form.sr.endStationName.GetValue()) > 0:
        //            self.SearchStationFromEdit(self.form.sr.endStationName)
        //            if not self.data.endStation:
        //                return False
        //        if not self.data.endStation:
        //            errorLabel = GetByLabel('UI/Contracts/ContractsService/UserErrorMustSpecifyContractDestination')
        //            raise UserError('CustomInfo', {'info': errorLabel})
        //        if not self.data.assigneeID:
        //            if self.data.reward < MIN_CONTRACT_MONEY:
        //                errorLabel = GetByLabel('UI/Contracts/ContractsService/UserErrorMinimumRewardNotMet', minimum=MIN_CONTRACT_MONEY)
        //                raise UserError('CustomInfo', {'info': errorLabel})
        //            if self.data.collateral < MIN_CONTRACT_MONEY:
        //                errorLabel = GetByLabel('UI/Contracts/ContractsService/UserErrorMinimumCollateralNotMet', minimum=MIN_CONTRACT_MONEY)
        //                raise UserError('CustomInfo', {'info': errorLabel})
        //        return True

        // DEST CAN'T BE CURRENT STATION!!! xD
        //(22:33:37) duketwo: Hold on there! You can't deliver a courier package to the same place where it came from, that would be ridiculous!
        //(22:33:37) duketwo: Ahh, you almost had me there. This is a joke, right? Very funny! Now go on and select another destination, you big comedian.

        #region Constructors

        public DirectContract(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Fields

        //
        // Destinations
        //
        public CourierDestination AMARR = new CourierDestination(60008494, "Amarr VIII (Oris) - Emperor Family Academy");

        public CourierDestination DODIXIE = new CourierDestination(60011866, "Dodixie IX - Moon 20 - Federation Navy Assembly Plant");
        public CourierDestination HEK = new CourierDestination(60005686, "Hek VIII - Moon 12 - Boundless Creation Factory");

        public CourierDestination JITA = new CourierDestination(60003760, "Jita IV - Moon 4 - Caldari Navy Assembly Plant");

        //
        // Courier Providers
        //
        // https://api.eveonline.com/eve/CharacterID.xml.aspx?names=Push%20Industries
        public CourierProvider PUSH_X = new CourierProvider(98079862, "Push Industries");

        public CourierDestination RENS = new CourierDestination(60004588, "Rens VI - Moon 8 - Brutor tribe Treasury");

        // https://api.eveonline.com/eve/CharacterID.xml.aspx?names=Red%20Frog%20Freight
        public CourierProvider RF_Freight = new CourierProvider(1495741119, "Red Frog Freight");

        private static readonly Random RANDOM = new Random();

        private DateTime _nextLoadPageInfo = DateTime.MinValue;

        #endregion Fields

        #region Properties

        public bool CanFinishCourierContract
        {
            get
            {
                if (IsCreateContractWindowOpen)
                {
                    DirectWindow wnd = GetCreateContractWindow;
                    PyObject data = wnd.PyWindow.Attribute("data");
                    if (data.IsValid)
                    {
                        bool reward = data.HasAttrString("reward");
                        bool expTime = data.HasAttrString("expiretime");
                        bool name = data.HasAttrString("name");
                        bool assigneeID = data.HasAttrString("assigneeID");
                        bool type = data.HasAttrString("type");
                        bool endStationName = data.HasAttrString("endStationName");
                        bool endStation = data.HasAttrString("endStation");
                        bool duration = data.HasAttrString("duration");
                        bool avail = data.HasAttrString("avail");
                        return reward && expTime && name && assigneeID && type && endStationName &&
                               endStation && duration && avail;
                    }
                }

                return false;
            }
        }

        public DirectWindow GetCreateContractWindow
        {
            get { return DirectEve.Windows.Find(w => w.Guid.Equals("form.CreateContract")); }
        }

        public bool IsCreateContractWindowOpen => GetCreateContractWindow != null;

        #endregion Properties

        #region Methods

        public void CreateContract(IEnumerable<DirectItem> items)
        {
            if (!items.Any())
                return;

            PyObject contractsSvc = DirectEve.GetLocalSvc("contracts");
            if (contractsSvc.IsValid)
            {
                Dictionary<string, object> keywords = new Dictionary<string, object>
                {
                    { "items", items.Select(i => i.PyItem) }
                };

                DirectEve.ThreadedCallWithKeywords(contractsSvc.Attribute("OpenCreateContract"), keywords);
            }
        }

        public bool CreateContract()
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                DirectWindow wnd = GetCreateContractWindow;

                return wnd.PyWindow.Call("CreateContract").ToBool();
            }
            return false;
        }

        public bool FinishStep1()
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                DirectWindow wnd = GetCreateContractWindow;

                return wnd.PyWindow.Call("FinishStep1").ToBool();
            }

            return false;
        }

        public bool FinishStep2()
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                DirectWindow wnd = GetCreateContractWindow;

                return wnd.PyWindow.Call("FinishStep2").ToBool();
            }

            return false;
        }

        public int GetNumContractsLeft()
        {
            if (DirectEve.IsServiceRunning("contracts"))
                return DirectEve.GetLocalSvc("contracts").Attribute("myPageInfo").Attribute("numContractsLeft").ToInt();

            DirectEve.StartService("contracts");
            return -1;
        }

        public void GotoPage(int n)
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                DirectWindow wnd = GetCreateContractWindow;
                DirectEve.ThreadedCall(wnd.PyWindow.Attribute("GotoPage"), n);
            }
        }

        public bool IsPageInfoLoaded()
        {
            return DirectEve.GetLocalSvc("contracts").Attribute("myPageInfo").IsValid;
        }

        public bool LoadPageInfo()
        {
            if (_nextLoadPageInfo < DateTime.UtcNow)
            {
                _nextLoadPageInfo = DateTime.UtcNow.AddMilliseconds(RANDOM.Next(4000, 7000));
                DirectEve.ThreadedLocalSvcCall("contracts", "CollectMyPageInfo");
                return true;
            }
            return false;
        }

        public void SetAssignee(CourierProvider provider)
        {
            int id = provider.Id;
            string name = provider.Name;
            SetAssigneeId(id);
            SetAssigneeName(name);
        }

        public void SetAvailabilityOptions(AvailabilityOptions option)
        {
            int val = (int) option;
            SetDataValue("avail", val);
        }

        public void SetCollateral(int collateral)
        {
            SetDataValue("collateral", collateral);
        }

        public void SetContractType(ContractType type)
        {
            int typeInt = (int) type;
            SetDataValue("type", typeInt);
        }

        public void SetCourierContract(int reward, int collateral, int durationDays, ExpireTime expireTime, CourierDestination destination,
            CourierProvider provider, bool forCorp)
        {
            const ContractType type = ContractType.TYPE_COURIER;
            SetContractType(type);
            SetReward(reward);
            SetCollateral(collateral);
            SetDuration(durationDays);
            SetExpireTime(expireTime);
            SetAssignee(provider);
            SetCourierDestination(destination);
            SetDescription(string.Empty);
            SetAvailabilityOptions(AvailabilityOptions.PRIVATE);
            SetForCorp(forCorp);
        }

        public void SetCourierDestination(CourierDestination dest)
        {
            int id = dest.Id;
            string name = dest.Name;
            SetEndStation(id);
            SetEndStationName(name);
        }

        public void SetDescription(string description)
        {
            SetDataValue("description", description);
        }

        public void SetDuration(int days)
        {
            if (days < 1 || days > 30)
                return;
            SetDataValue("duration", days);
        }

        public void SetExpireTime(ExpireTime expireTime)
        {
            int expTime = (int) expireTime;
            SetDataValue("expiretime", expTime);
        }

        public void SetPrice(int price)
        {
            // not implemented yet... only courier contracts are supported for now
        }

        public void SetReward(int reward)
        {
            SetDataValue("reward", reward);
        }

        private void SetAssigneeId(int id)
        {
            SetDataValue("assigneeID", id);
        }

        private void SetAssigneeName(string name)
        {
            SetDataValue("name", name);
        }

        private void SetDataValue(string key, object value)
        {
            if (IsCreateContractWindowOpen)
            {
                DirectWindow wnd = GetCreateContractWindow;
                PyObject data = wnd.PyWindow.Attribute("data");
                if (data.IsValid) DirectEve.ThreadedCall(data.Attribute("Set"), key, value);
            }
        }

        private void SetEndStation(int stationId)
        {
            SetDataValue("endStation", stationId);
        }

        private void SetEndStationName(string name)
        {
            SetDataValue("endStationName", name);
        }

        // forCorp
        private void SetForCorp(bool forCorp)
        {
            SetDataValue("forCorp", forCorp);
        }

        #endregion Methods
    }
}