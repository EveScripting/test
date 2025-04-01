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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace SharedComponents.SQLite
{
    public class ReadConn : IDisposable
    {

        public IDbConnection DB { get; }

        private ReadConn()
        {
            DB = ConnFactory.Instance.Factory.Open();
        }

        public static ReadConn Open()
        {
            var readConn = new ReadConn();
            return readConn;
        }

        #region IDisposable

        public void Dispose()
        {
            DB.Close();
        }

        #endregion
    }
}
