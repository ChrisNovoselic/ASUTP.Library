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
        private static bool _created = false;

        private ConnectionSettings _connSett = null;

        private int _iIdListener = -1;

        private DbConnection _dbConn = null;

        private Writer () { _created = true; }

        public static ASUTP.ILoggingDbWriter Create () {
            if (_created == true)
                throw new Exception ("Повторное создание объекта для сохранения событий приложения в БД...");
            else
                ;

            return new Writer ();
        }

        /// <summary>
        /// Объект с параметрами для соедиения с БД при режиме журналирования "БД"
        /// </summary>
        public object ConnSett
        {
            set
            {
                if (value is ConnectionSettings) {
                    _connSett = new ConnectionSettings (value as ConnectionSettings);
                } else
                    throw new Exception ("Попытка установить параметры соединения с БД значениями объекта неразрешенного типа...");
            }
        }

        public bool IsConnect
        {
            get
            {
                return (!(_connSett == null))
                    && (_iIdListener > 0)
                    && (!(_dbConn == null))
                    && ((!(_dbConn.State == System.Data.ConnectionState.Closed))
                        || (!(_dbConn.State == System.Data.ConnectionState.Broken)));
            }
        }

        public int Connect ()
        {
            int err = IsConnect == true ? 0 : -1;

            if (err < 0) {
                _iIdListener = DbSources.Sources ().Register (_connSett, false, @"LOGGING_DB");
                //Console.WriteLine(@"Logging::connect (active=false) - s_iIdListener=" + s_iIdListener);
                if (!(_iIdListener < 0))
                    _dbConn = DbSources.Sources ().GetConnection (_iIdListener, out err);
                else
                    ;
            } else
                ;

            return err;
        }

        public void Disconnect ()
        {
            if (!(_iIdListener < 0))
                DbSources.Sources ().UnRegister (_iIdListener);
            else
                ;
            _iIdListener = -1;
            _dbConn = null;
        }

        public void Exec (string message, out int err)
        {
            DbTSQLInterface.ExecNonQuery (ref _dbConn, message, null, null, out err);
        }
    }
}
