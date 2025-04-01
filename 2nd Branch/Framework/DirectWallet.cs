extern alias SC;

using SC::SharedComponents.Py;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public enum CorpWalletDivisions
    {
        Division1 = 1,
        Division2 = 2,
        Division3 = 3,
        Division4 = 4,
        Division5 = 5,
        Division6 = 6,
        Division7 = 7,
        Division8 = 8,
    }

    public class DirectWallet : DirectObject
    {
        #region Constructors

        internal DirectWallet(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Methods
        /**
        public void SwitchTab(JournalTab t)
        {
            DirectEve.Interval(3000);
            if (SelectedTab != JournalTab.Unknown && SelectedTab != t)
                DirectEve.ThreadedCall(PyWindow.Attribute("ShowAgentTab"), (int) t);
        }
        **/
        #endregion Methods

        #region Properties

        public double Wealth => (double)DirectEve.GetLocalSvc("wallet").Attribute("wealth");

        //class WalletSvc(service.Service):
        //def TransferMoney(self, fromID, fromAccountKey, toID, toAccountKey):
        //def GetAccountName(self, acctID):
        //def SelectWalletDivision(self, *args):
        //def AskSetWalletDivision(self, *args):
        //def GetCorpWealth(self, accountKey):
        //def GetCorpWealthCached1Min(self, accountKey):

        //Roles
        //
        //def AmAccountant(self):
        //def AmAccountantOrJuniorAccountant(self):
        //def AmAccountantOrTrader(self):
        //def HaveAccessToCorpWallet(self):
        //def HaveAccessToCorpWalletDivision(self, division):
        //def HaveReadAccessToCorpWalletDivision(self, division):
        //def GetAccessibleWallets(self):
        //def GetWnd(self, new = 0):
        //def GetDivisionName(self, divisionID):

        //
        // class WalletContainer(uiprimitives.Container):
        // def TransferMoney(self, fromID, fromAccountKey, toID, toAccountKey):
        // def SelectWalletDivision(self, *args):
        // def CanChangeActiveDivision(self):
        // def AskSetWalletDivision(self):
        //

        #endregion Properties
    }
}