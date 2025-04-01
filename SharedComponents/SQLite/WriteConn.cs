/*
 * ---------------------------------------
 * User: duketwo
 * Date: 03.07.2018
 * Time: 12:30
 * ---------------------------------------
 */


using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using SharedComponents.Utility;

namespace SharedComponents.SQLite
{
    public class WriteConn : IDisposable
    {
        public IDbConnection DB { get; }
        private ProcessLock _pLock;
        private const string _mutexRef = "_writeConnMutexRef";

        private WriteConn()
        {
            _pLock = new ProcessLock(-1, _mutexRef); // wait until other processes finished writing   
            DB = ConnFactory.Instance.Factory.Open(); // open the db connection
        }

        public static WriteConn Open()
        {
            return new WriteConn();
        }

        #region IDisposable

        public void Dispose()
        {
            _pLock.Dispose(); // dispose the mutex after writing
            DB.Close(); // close the db
        }

        #endregion
    }
}
