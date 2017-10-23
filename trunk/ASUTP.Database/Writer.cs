using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace ASUTP.Database
{
    public class Writer : ASUTP.ILoggingDbWriter
    {
        private static ConnectionSettings s_connSett = null;

        private static int s_iIdListener = -1;

        private static DbConnection s_dbConn = null;

        /// <summary>
        /// Объект с параметрами для соедиения с БД при режиме журналирования "БД"
        /// </summary>
        public static ConnectionSettings ConnSett
        {
            set
            {
                s_connSett = new ConnectionSettings ();

                s_connSett.id = value.id;
                s_connSett.name = value.name;
                s_connSett.server = value.server;
                s_connSett.instance = value.instance;
                s_connSett.port = value.port;
                s_connSett.dbName = value.dbName;
                s_connSett.userName = value.userName;
                s_connSett.password = value.password;

                //s_connSett.ignore = value.ignore;
            }
        }

        public bool IsConnect
        {
            get
            {
                return (!(s_connSett == null))
                    && (s_iIdListener > 0)
                    && (!(s_dbConn == null))
                    && ((!(s_dbConn.State == System.Data.ConnectionState.Closed))
                        || (!(s_dbConn.State == System.Data.ConnectionState.Broken)));
            }
        }

        public int Connect ()
        {
            int err = IsConnect == true ? 0 : -1;

            if (err < 0) {
                s_iIdListener = DbSources.Sources ().Register (s_connSett, false, @"LOGGING_DB");
                //Console.WriteLine(@"Logging::connect (active=false) - s_iIdListener=" + s_iIdListener);
                if (!(s_iIdListener < 0))
                    s_dbConn = DbSources.Sources ().GetConnection (s_iIdListener, out err);
                else
                    ;
            } else
                ;

            return err;
        }

        public void Disconnect ()
        {
            if (!(s_iIdListener < 0))
                DbSources.Sources ().UnRegister (s_iIdListener);
            else
                ;
            s_iIdListener = -1;
            s_dbConn = null;
        }

        public void Exec (string message, out int err)
        {
            DbTSQLInterface.ExecNonQuery (ref s_dbConn, message, null, null, out err);
        }
    }
}
