using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data.OracleClient;

using System.IO; //StreamReader

//namespace HClassLibrary
namespace HClassLibrary
{
    public class DbTSQLInterface : DbInterface
    {
        public enum Error
        {
            NO_ERROR = 0, DBCONN_NOT_OPEN = -1, DBCONN_NULL = -2, DBADAPTER_NULL = -3, PARAMQUERY_NULL = -4, PARAMQUERY_LENGTH = -5
                            , CATCH_DBCONN = -11
                            , CATCH_CSV_READ = -21, CATCH_CSV_ROWREAD = -22
                            , TABLE_NULL = -31, TABLE_ROWS_0 = -32
        };
        public enum QUERY_TYPE { UPDATE, INSERT, DELETE, COUNT_QUERY_TYPE };

        public static string MessageDbOpen = "Соединение с базой установлено";
        public static string MessageDbClose = "Соединение с базой разорвано";
        public static string MessageDbException = "!Исключение! Работа с БД";

        private DbConnection m_dbConnection;
        private DbCommand m_dbCommand;
        private DbDataAdapter m_dbAdapter;
        private static object lockConn = new object ();

        private DB_TSQL_INTERFACE_TYPE m_connectionType;

        public DbTSQLInterface(DB_TSQL_INTERFACE_TYPE type, string name)
            : base(name)
        {
            m_connectionType = type;

            m_connectionSettings = new ConnectionSettings();

            switch (m_connectionType)
            {
                case DB_TSQL_INTERFACE_TYPE.MySQL:
                    m_dbConnection = new MySqlConnection();

                    m_dbCommand = new MySqlCommand();
                    m_dbCommand.Connection = m_dbConnection;
                    m_dbCommand.CommandType = CommandType.Text;

                    m_dbAdapter = new MySqlDataAdapter();
                    break;
                case DB_TSQL_INTERFACE_TYPE.MSSQL:
                    m_dbConnection = new SqlConnection();

                    m_dbCommand = new SqlCommand();
                    m_dbCommand.Connection = m_dbConnection;
                    m_dbCommand.CommandType = CommandType.Text;

                    m_dbAdapter = new SqlDataAdapter();
                    break;
                case DB_TSQL_INTERFACE_TYPE.Oracle:
                    m_dbConnection = new OracleConnection();

                    m_dbCommand = new OracleCommand();
                    m_dbCommand.Connection = m_dbConnection;
                    m_dbCommand.CommandType = CommandType.Text;

                    m_dbAdapter = new OracleDataAdapter();
                    break;
                case DB_TSQL_INTERFACE_TYPE.MSExcel:
                    m_dbConnection = new OleDbConnection();

                    m_dbCommand = new OleDbCommand();
                    m_dbCommand.Connection = m_dbConnection;
                    m_dbCommand.CommandType = CommandType.Text;

                    m_dbAdapter = new OleDbDataAdapter();
                    break;
                default:
                    break;
            }

            m_dbAdapter.SelectCommand = m_dbCommand;
        }

        protected override bool Connect()
        {
            if (((ConnectionSettings)m_connectionSettings).Validate() != ConnectionSettings.ConnectionSettingsError.NoError)
                return false;
            else
                ;

            bool result = false, bRes = false;

            if (m_dbConnection.State == ConnectionState.Open)
                bRes = true;
            else
                ;

            try
            {
                if (bRes == true)
                    return bRes;
                else
                    bRes = true;
            }
            catch (Exception e)
            {
                logging_catch_db(m_dbConnection, e);
            }

            if (!(m_dbConnection.State == ConnectionState.Closed))
                bRes = false;
            else
                ;

            if (bRes == false)
                return bRes;
            else
                ;

            lock (lockConnectionSettings)
            {
                if (needReconnect) // если перед приходом в данную точку повторно были изменены настройки, то подключения со старыми настройками не делаем
                    return false;
                else
                    ;

                if (((ConnectionSettings)m_connectionSettings).ignore == true)
                    return false;
                else
                    ;

                //string connStr = string.Empty;
                switch (m_connectionType)
                {
                    case DB_TSQL_INTERFACE_TYPE.MSSQL:
                        //connStr = connectionSettings.GetConnectionStringMSSQL();
                        ((SqlConnection)m_dbConnection).ConnectionString = ((ConnectionSettings)m_connectionSettings).GetConnectionStringMSSQL();
                        break;
                    case DB_TSQL_INTERFACE_TYPE.MySQL:
                        //connStr = connectionSettings.GetConnectionStringMySQL();
                        ((MySqlConnection)m_dbConnection).ConnectionString = ((ConnectionSettings)m_connectionSettings).GetConnectionStringMySQL();
                        break;
                    case DB_TSQL_INTERFACE_TYPE.Oracle:
                        ((OracleConnection)m_dbConnection).ConnectionString = ((ConnectionSettings)m_connectionSettings).GetConnectionStringOracle();
                        break;
                    case DB_TSQL_INTERFACE_TYPE.MSExcel:
                        //((OleDbConnection)m_dbConnection).ConnectionString = ConnectionSettings.GetConnectionStringExcel ();
                        break;
                    default:
                        break;
                }
                //m_dbConnection.ConnectionString = connStr;
            }

            try
            {
                m_dbConnection.Open();
                result = true;
                if (!(((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING)) { logging_open_db(m_dbConnection); } else ;                
            }
            catch (Exception e)
            {
                if (!(((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING)) { logging_catch_db(m_dbConnection, e); } else ;
            }

            return result;
        }

        public override void SetConnectionSettings(object cs, bool bStarted)
        {
            lock (lockConnectionSettings)
            {
                ((ConnectionSettings)m_connectionSettings).id = ((ConnectionSettings)cs).id;
                ((ConnectionSettings)m_connectionSettings).server = ((ConnectionSettings)cs).server;
                ((ConnectionSettings)m_connectionSettings).port = ((ConnectionSettings)cs).port;
                ((ConnectionSettings)m_connectionSettings).dbName = ((ConnectionSettings)cs).dbName;
                ((ConnectionSettings)m_connectionSettings).userName = ((ConnectionSettings)cs).userName;
                ((ConnectionSettings)m_connectionSettings).password = ((ConnectionSettings)cs).password;
                ((ConnectionSettings)m_connectionSettings).ignore = ((ConnectionSettings)cs).ignore;

                needReconnect = true;
            }

            if (bStarted == true)
                //base.SetConnectionSettings (cs); //базовой function 'cs' не нужен
                SetConnectionSettings();
            else
                ;
        }

        protected override bool Disconnect()
        {
            if (m_dbConnection.State == ConnectionState.Closed)
                return true;
            else
                ;

            bool result = false;

            try
            {
                //Из-за этого кода не происходит возобновления обмена данными при разарыве/восстановлении соединения
                //lock (lockListeners)
                //{
                //    m_listListeners.Clear();
                //}

                m_dbConnection.Close();
                result = true;

                if (!(((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING)) { logging_close_db(m_dbConnection); } else ;

            }
            catch (Exception e)
            {
                logging_catch_db(m_dbConnection, e);
            }

            return result;
        }

        public override void Disconnect(out int er)
        {
            er = (int)Error.NO_ERROR;

            try
            {
                if (!(m_dbConnection.State == ConnectionState.Closed))
                {
                    m_dbConnection.Close();

                    logging_close_db(m_dbConnection);

                    m_dbConnection = null;
                }
                else
                    ;
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, @"DbTSQLInterface::CloseConnection () - ...", Logging.INDEX_MESSAGE.NOT_SET);

                er = (int)Error.CATCH_DBCONN;
            }
        }

        protected override bool GetData(DataTable table, object query)
        {
            if (m_dbConnection.State != ConnectionState.Open)
                return false;
            else
                ;

            bool result = false;

            try { m_dbCommand.CommandText = query.ToString(); }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

            table.Reset();
            table.Locale = System.Globalization.CultureInfo.InvariantCulture;

            try
            {
                m_dbAdapter.Fill(table);
                result = true;
            }
            catch (DbException e)
            {
                needReconnect = true;
                logging_catch_db (m_dbConnection, e);
            }
            catch (Exception e)
            {
                needReconnect = true;
                logging_catch_db(m_dbConnection, e);
            }

            if (needReconnect == true)
                Logging.Logg().Error(@"DbTSQLInterface::GetData () - query=" + query, Logging.INDEX_MESSAGE.NOT_SET);
            else
                ;

            return result;
        }

        private static string ConnectionStringToLog (string strConnSett)
        {
            string strRes = string.Empty;
            int pos = -1;

            pos = strConnSett.IndexOf("Password", StringComparison.CurrentCultureIgnoreCase);
            if (pos < 0)
                strRes = strConnSett;
            else
                strRes = strConnSett.Substring(0, pos);

            return strRes;
        }

        private static void logging_catch_db(DbConnection conn, Exception e)
        {
            string s = string.Empty, log = string.Empty;
            if (!(conn == null))
                s = ConnectionStringToLog (conn.ConnectionString);
            else
                s = @"Объект 'DbConnection' = null";

            log = MessageDbException;
            log += Environment.NewLine + "Строка соединения: " + s;
            if (!(e == null))
            {
                log += Environment.NewLine + "Ошибка: " + e.Message;
                log += Environment.NewLine + e.ToString();
            }
            else
                ;
            Logging.Logg().ExceptionDB(log);
        }

        private static void logging_close_db (DbConnection conn)
        {
            string s = ConnectionStringToLog(conn.ConnectionString);

            Logging.Logg().Debug(MessageDbClose + " (" + s + ")", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private static void logging_open_db (DbConnection conn)
        {            
            string s = ConnectionStringToLog(conn.ConnectionString);

            Logging.Logg().Debug(MessageDbOpen + " (" + s + ")", Logging.INDEX_MESSAGE.NOT_SET, true);
        }

        public static DbTSQLInterface.DB_TSQL_INTERFACE_TYPE getTypeDB(string strConn)
        {
            DB_TSQL_INTERFACE_TYPE res = DB_TSQL_INTERFACE_TYPE.MSSQL;
            int port = -1;
            string strMarkPort = @"port="
                , strPort =  string.Empty;

            if (! (strConn.IndexOf (strMarkPort) < 0)) {
                int iPosPort = strConn.IndexOf (strMarkPort) + strMarkPort.Length;
                strPort = strConn.Substring(iPosPort, strConn.IndexOf(';', iPosPort) - iPosPort);

                if (Int32.TryParse(strPort, out port) == true)
                    res = getTypeDB (port);
                else
                    ;
            } else
                ;

            return res;
        }

        public static DbTSQLInterface.DB_TSQL_INTERFACE_TYPE getTypeDB(int port)
        {
            DbTSQLInterface.DB_TSQL_INTERFACE_TYPE typeDBRes = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.UNKNOWN;

            switch (port)
            {
                case 3306:
                    typeDBRes = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MySQL;
                    break;
                case 1433:
                    typeDBRes = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MSSQL;
                    break;
                case 1521:
                    typeDBRes = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.Oracle;
                    break;
                default:
                    break;
            }

            return typeDBRes;
        }

        public DbConnection GetConnection(out int err)
        {
            err = (int)Error.NO_ERROR;

            needReconnect = false;
            bool bRes = Connect();

            if (bRes == true)
                return m_dbConnection;
            else
            {
                err = (int)Error.DBCONN_NOT_OPEN;

                return null;
            }
        }

        ////public static DbConnection getConnection (ConnectionSettings connSett, out int er)
        //public DbConnection getConnection(ConnectionSettings connSett, out int er)
        //{
        //    er = 0;

        //    string s = string.Empty;
        //    DbConnection connRes = null;

        //    DbTSQLInterface.DB_TSQL_INTERFACE_TYPE typeDB = getTypeDB (connSett.port);

        //    if (!(typeDB == DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.UNKNOWN))
        //    {
        //        switch (typeDB)
        //        {
        //            case DB_TSQL_INTERFACE_TYPE.MySQL:
        //                s = connSett.GetConnectionStringMySQL();
        //                connRes = new MySqlConnection(s);
        //                break;
        //            case DB_TSQL_INTERFACE_TYPE.MSSQL:
        //                s = connSett.GetConnectionStringMSSQL ();
        //                connRes = new SqlConnection(s);
        //                break;
        //            default:
        //                break;
        //        }

        //        try
        //        {
        //            connRes.Open();

        //            if (!(connSett.id == ConnectionSettings.ID_LISTENER_LOGGING)) { logging_open_db(connRes); } else ;
        //        }
        //        catch (Exception e)
        //        {
        //            if (!(connSett.id == ConnectionSettings.ID_LISTENER_LOGGING)) { logging_catch_db(connRes, e); } else ;

        //            connRes = null;

        //            er = -1;
        //        }
        //    }
        //    else
        //        ;

        //    return connRes;
        //}

        //public static void closeConnection(ref DbConnection conn, out int er)
        //{
        //    er = 0;

        //    try
        //    {
        //        if (!(conn.State == ConnectionState.Closed))
        //        {
        //            conn.Close();

        //            //if (!(((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING)) { logging_close_db(conn); } else ;
        //            logging_close_db (conn);
        //        }
        //        else
        //            ;
        //    }
        //    catch (Exception e)
        //    {
        //        logging_catch_db(conn, e);
                
        //        conn = null;

        //        er = -1;
        //    }
        //}

        /// <summary>
        /// Добавление в начале и в конце значения одинарные кавычки,
        ///  если тип значения "простой"
        /// </summary>
        /// <param name="table">таблица со значениями</param>
        /// <param name="row">номер записи в таблице со значениями</param>
        /// <param name="col">номер столбца в записи таблицы со значениями</param>
        /// <returns>строка - значение из таблицы с одинарными кавычками или без них</returns>
        public static string ValueToQuery(DataTable table, int row, int col)
        {
            return ValueToQuery(table.Rows[row][col], table.Columns[col].DataType);
        }

        public static string ValueToQuery(object val, Type type)
        {
            string strRes = string.Empty
                , strVal = string.Empty;
            bool bQuote =
                //table.Columns[col].DataType.IsByRef;
                !type.IsPrimitive;

            switch (type.Name)
            {
                case "DateTime":
                    strVal = ((DateTime)val).ToString(@"yyyyMMdd HH:mm:ss.fff"); //System.Globalization.CultureInfo.InvariantCulture
                    break;
                case @"Double":
                case @"double":
                    strVal = ((double)val).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case @"Float":
                case @"float":
                    strVal = ((float)val).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                default:
                    //??? не учитывается может поле принимать значение NULL
                    strVal = val.ToString();
                    if (strVal.Length == 0)
                        strVal = "NULL";
                    else
                        strVal = strVal.Replace(@"'", @"''");
                    break;
            }

            strRes = (bQuote ? "'" : string.Empty) + strVal + (bQuote ? "'" : string.Empty);

            return strRes;
        }

        /// <summary>
        /// Импорт содержания текстового файла с разделителем в таблицу
        /// </summary>
        /// <param name="path">путь к файлу</param>
        /// <param name="fields">наименования полей таблицы (не используется)</param>
        /// <param name="er"></param>
        /// <returns></returns>
        public static DataTable CSVImport(string path, string fields, out int er)
        {
            er = (int)Error.NO_ERROR;

            DataTable dataTableRes = new DataTable();

            string [] data;
            //Открыть поток чтения файла...
            try
            {
                StreamReader sr = new StreamReader(path);
                //Из 1-ой строки сформировать столбцы...
                data = sr.ReadLine().Split(';');
                foreach (string field in data)
                    dataTableRes.Columns.Add(field, typeof(string));
                //Из остальных строк сформировать записи...
                while (sr.EndOfStream == false)
                {
                    try
                    {
                        data = sr.ReadLine().Split(';');
                        dataTableRes.Rows.Add(data);
                    }
                    catch (Exception e)
                    {
                        er = (int)Error.CATCH_CSV_ROWREAD;
                        break;
                    }
                }

                //Закрыть поток чтения файла
                sr.Close();
            }
            catch (Exception e)
            {
                er = (int)Error.CATCH_CSV_READ;
            }

            return dataTableRes;
        }

        public static DataTable Select(string path, string query, out int er)
        {
            er = (int)Error.NO_ERROR;

            DataTable dataTableRes = new DataTable();

            OleDbConnection connectionOleDB = null;
            System.Data.OleDb.OleDbCommand commandOleDB;
            System.Data.OleDb.OleDbDataAdapter adapterOleDB;

            if (path.IndexOf(".xls") > -1)
                connectionOleDB = new OleDbConnection(ConnectionSettings.GetConnectionStringExcel(path));
            else
            {
                if (path.IndexOf("CSV_DATASOURCE=") > -1)
                    connectionOleDB = new OleDbConnection(ConnectionSettings.GetConnectionStringCSV(path.Remove(0, "CSV_DATASOURCE=".Length)));
                else
                {
                    connectionOleDB = new OleDbConnection(ConnectionSettings.GetConnectionStringDBF(path));
                }
            }

            if (!(connectionOleDB == null))
            {
                commandOleDB = new OleDbCommand();
                commandOleDB.Connection = connectionOleDB;
                commandOleDB.CommandType = CommandType.Text;

                adapterOleDB = new OleDbDataAdapter();
                adapterOleDB.SelectCommand = commandOleDB;

                commandOleDB.CommandText = query;

                dataTableRes.Reset();
                dataTableRes.Locale = System.Globalization.CultureInfo.InvariantCulture;

                try
                {
                    connectionOleDB.Open();

                    if (connectionOleDB.State == ConnectionState.Open)
                    {
                        adapterOleDB.Fill(dataTableRes);
                    }
                    else
                        ; //
                }
                catch (OleDbException e)
                {
                    logging_catch_db(connectionOleDB, e);

                    er = (int)Error.CATCH_DBCONN;
                }

                connectionOleDB.Close();
            }
            else
                ;

            return dataTableRes;
        }

        /// <summary>
        /// Проверка и преобразование запроса к БД в зависимости от типа БД (MSSQL, MySql)
        /// </summary>
        /// <param name="strConn">строка соединения объекта DbConnection</param>
        /// <param name="query">преобразуемый запрос</param>
        private static void queryValidateOfTypeDB(string strConn, ref string query)
        {
            switch (getTypeDB(strConn))
            {
                case DB_TSQL_INTERFACE_TYPE.MySQL:
                    query = query.Replace(@"[dbo].", string.Empty);
                    query = query.Replace('[', '`');
                    query = query.Replace(']', '`');
                    break;
                case DB_TSQL_INTERFACE_TYPE.MSSQL:
                case DB_TSQL_INTERFACE_TYPE.Oracle:
                    break;
                default:
                    break;
            }
        }

        public static DataTable Select(ref DbConnection conn, string query, DbType[] types, object[] parametrs, out int er)
        {
            er = (int)Error.NO_ERROR;
            DataTable dataTableRes = null;

            if (conn == null)
                er = (int)Error.DBCONN_NULL;
            else {
                lock (lockConn)
                {
                    dataTableRes = new DataTable();

                    queryValidateOfTypeDB (conn.ConnectionString, ref query);

                    ParametrsValidate(types, parametrs, out er);

                    if (er == 0)
                    {
                        DbCommand cmd = null;
                        DbDataAdapter adapter = null;

                        if (conn is MySqlConnection)
                        {
                            cmd = new MySqlCommand();
                            adapter = new MySqlDataAdapter();
                        }
                        else if (conn is SqlConnection) {
                                cmd = new SqlCommand();
                                adapter = new SqlDataAdapter();
                            }
                            else if (conn is OracleConnection) {
                                    cmd = new OracleCommand();
                                    adapter = new OracleDataAdapter();
                                }
                                else
                                    ;

                        if ((!(cmd == null)) && (!(adapter == null))) {
                            cmd.Connection = conn;
                            cmd.CommandType = CommandType.Text;

                            adapter.SelectCommand = cmd;

                            cmd.CommandText = query;
                            ParametrsAdd(cmd, types, parametrs);

                            dataTableRes.Reset();
                            dataTableRes.Locale = System.Globalization.CultureInfo.InvariantCulture;

                            try
                            {
                                if (conn.State == ConnectionState.Open)
                                {
                                    adapter.Fill(dataTableRes);
                                }
                                else
                                    er = (int)Error.DBCONN_NOT_OPEN;
                            }
                            catch (Exception e)
                            {
                                logging_catch_db(conn, e);

                                er = (int)Error.CATCH_DBCONN;
                            }
                        }
                        else
                            er = (int)Error.DBADAPTER_NULL;
                    }
                    else
                    {
                        // Логгирование в 'ParametrsValidate'
                    }
                }
            }

            return dataTableRes;
        }

        /*public static DataTable Select(ConnectionSettings connSett, string query, out int er)
        {
            er = 0;

            DataTable dataTableRes = null;
            DbConnection conn;
            conn = getConnection (connSett, out er);

            if (er == 0)
            {
                dataTableRes = Select(conn, query, null, null, out er);

                closeConnection (conn, out er);
            }
            else
                dataTableRes = new DataTable();

            return dataTableRes;
        }*/

        private static void ParametrsAdd(DbCommand cmd, DbType[] types, object[] parametrs)
        {
            if ((!(types == null)) && (!(parametrs == null)))
                foreach (DbType type in types)
                {
                    //cmd.Parameters.AddWithValue(string.Empty, parametrs[commandMySQL.Parameters.Count - 1]);
                    //cmd.Parameters.Add(new SqlParameter(cmd.Parameters.Count.ToString (), parametrs[cmd.Parameters.Count]));
                    cmd.Parameters.Add(new SqlParameter(string.Empty, parametrs[cmd.Parameters.Count]));
                }
            else
                ;
        }

        private static void ParametrsValidate(DbType[] types, object[] parametrs, out int err)
        {
            err = (int)Error.NO_ERROR;

            //if ((!(types == null)) || (!(parametrs == null)))
            if ((types == null) || (parametrs == null))
                ;
            else
                if ((!(types == null)) && (!(parametrs == null)))
                {
                    if (!(types.Length == parametrs.Length))
                    {
                        err = (int)Error.PARAMQUERY_LENGTH;
                    }
                    else
                        ;
                }
                else
                    err = (int)Error.PARAMQUERY_NULL;

            if (!(err == 0))
            {
                Logging.Logg().Error("!Ошибка! static DbTSQLInterface::ParametrsValidate () - types OR parametrs не корректны", Logging.INDEX_MESSAGE.NOT_SET);
            }
            else
                ;
        }

        public static void ExecNonQuery(ref DbConnection conn, string query, DbType[] types, object[] parametrs, out int er)
        {
            er = (int)Error.NO_ERROR;

            DbCommand cmd = null;

            queryValidateOfTypeDB(conn.ConnectionString, ref query);

            ParametrsValidate(types, parametrs, out er);

            if (er == 0)
            {
                lock (lockConn)
                {
                    if (conn is MySqlConnection) {
                        cmd = new MySqlCommand();
                    }
                    else if (conn is SqlConnection) {
                        cmd = new SqlCommand();
                        }
                        else
                            ;
                
                    if (! (cmd == null)) {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;

                        cmd.CommandText = query;
                        ParametrsAdd(cmd, types, parametrs);

                        try
                        {
                            if (conn.State == ConnectionState.Open)
                            {
                                cmd.ExecuteNonQuery();
                            }
                            else
                                er = (int)Error.DBCONN_NOT_OPEN;
                        }
                        catch (Exception e)
                        {
                            logging_catch_db(conn, e);

                            er = (int)Error.CATCH_DBCONN;
                        }
                    }
                    else
                        er = (int)Error.DBADAPTER_NULL;
                }
            }
            else
                ;
        }

        /*public static void ExecNonQuery(ConnectionSettings connSett, string query, out int er)
        {
            er = 0;

            DbConnection conn;

            conn = getConnection(connSett, out er);

            if (er == 0)
            {
                ExecNonQuery(conn, query, null, null, out er);

                closeConnection (conn, out er);
            }
            else
                ;
        }*/

        public static void ExecNonQuery(string path, string query, out int er)
        {
            er = 0;

            OleDbConnection connectionOleDB = null;
            System.Data.OleDb.OleDbCommand commandOleDB;

            if (path.IndexOf("xls") > -1)
                connectionOleDB = new OleDbConnection(ConnectionSettings.GetConnectionStringExcel(path));
            else
                //if (path.IndexOf ("dbf") > -1)
                connectionOleDB = new OleDbConnection(ConnectionSettings.GetConnectionStringDBF(path));
            //else
            //    ;

            if (!(connectionOleDB == null))
            {
                commandOleDB = new OleDbCommand();
                commandOleDB.Connection = connectionOleDB;
                commandOleDB.CommandType = CommandType.Text;

                commandOleDB.CommandText = query;

                try
                {
                    connectionOleDB.Open();

                    if (connectionOleDB.State == ConnectionState.Open)
                    {
                        commandOleDB.ExecuteNonQuery();
                    }
                    else
                        ; //
                }
                catch (Exception e)
                {
                    logging_catch_db(connectionOleDB, e);

                    er = (int)Error.CATCH_DBCONN;
                }

                connectionOleDB.Close();
            }
            else
                ;
        }
        /// <summary>
        /// Возвратить идентификатор для очередной записи в диапазоне [min...max]
        /// </summary>
        /// <param name="conn">Объект с установленным соединениес с БД</param>
        /// <param name="nameTable">Наименование таблицы для вставки записи</param>
        /// <param name="nameFieldID">Наименование целочисленного поля с идентификатором</param>
        /// <param name="min">Минимальное значение диапазона для поиска идентификатора</param>
        /// <param name="max">Максимальное значение диапазона для поиска идентификатора</param>
        /// <returns></returns>
        public static Int32 GetIdNext(ref DbConnection conn, string nameTable, out int err, string nameFieldID = @"ID", Int32 min = 0, Int32 max = Int32.MaxValue)
        {
            Int32 idRes = -1;
            err = (int)Error.NO_ERROR;

            lock (lockConn)
            {
                idRes = Convert.ToInt32(Select(ref conn, "SELECT MAX(" + nameFieldID + @") FROM " + nameTable + @" WHERE "
                        + nameFieldID + @">" + min + @" AND " + nameFieldID + @"<" + max
                    , null, null, out err).Rows[0][0]);
            }

            return ++idRes;
        }
        /// <summary>
        /// Возвратить идентификатор для очередной записи в диапазоне [min...max]
        /// </summary>
        /// <param name="table">Таблица для вставки записи</param>
        /// <param name="nameFieldID">Наименование целочисленного поля</param>
        /// <param name="min">Минимальное значение диапазона для поиска идентификатора</param>
        /// <param name="max">Максимальное значение диапазона для поиска идентификатора</param>
        /// <returns>Целочисленный идентификатор записи в таблице</returns>
        public static Int32 GetIdNext(DataTable table, out int err, string nameFieldID = @"ID", Int32 min = 0, Int32 max = Int32.MaxValue)
        {
            Int32 idRes = -1;
            err = (int)Error.NO_ERROR;
            DataRow[] rangeRows = null;

            if (table.Rows.Count > 0)
            {
                rangeRows = table.Select(nameFieldID + @">" + min + @" AND " + nameFieldID + @"<" + max, nameFieldID + @" DESC");

                if (rangeRows.Length > 0)
                    idRes = Convert.ToInt32(rangeRows[0][nameFieldID]);
                else
                    ;
            }
            else
                //err = (int)Error.TABLE_ROWS_0
                ;

            return ++idRes;
        }
        /// <summary>
        /// Изменение (вставка), удаление
        /// </summary>
        /// <param name="conn">Объект соединения с БД</param>
        /// <param name="nameTable">Наименование таблицы</param>
        /// <param name="keyFields">Наименования полей таблицы в составе ключа по поиску записей</param>
        /// <param name="origin">Таблица со значениями - исходная</param>
        /// <param name="data">Таблица со значениями - с изменениями</param>
        /// <param name="err">Признак выполнения функции</param>
        public static void RecUpdateInsertDelete(ref DbConnection conn, string nameTable, string keyFields, DataTable origin, DataTable data, out int err)
        {
            if (!(data.Rows.Count < origin.Rows.Count))
            {
                //UPDATE, INSERT
                RecUpdateInsert(ref conn, nameTable, keyFields, origin, data, out err);
            }
            else
            {
                //DELETE
                RecDelete(ref conn, nameTable, keyFields, origin, data, out err);
            }
        }

        private static string getWhereSelect(string fields, DataRow r)
        {
            string strRes = string.Empty;
            
            string [] arFields = fields.Split(',');

            for (int i = 0; i < arFields.Length; i++)
            {
                arFields[i] = arFields[i].Trim();
                strRes += arFields[i] + @"=" + r[arFields[i]] + @" AND ";
            }

            if (strRes.Equals(string.Empty) == false)
                strRes = strRes.Substring(0, strRes.Length - @" AND ".Length);
            else
                ;

            return strRes;
        }

        //Изменение (вставка) в оригинальную таблицу записей измененных (добавленных) в измененную таблицу (обязательно наличие поля: ID)
        public static void RecUpdateInsert(ref DbConnection conn, string nameTable, string keyFields, DataTable origin, DataTable data, out int err)
        {
            err = (int)Error.NO_ERROR;

            int j = -1, k = -1;
            bool bUpdate = false;
            DataRow[] dataRows;
            string[] strQuery = new string[(int)DbTSQLInterface.QUERY_TYPE.COUNT_QUERY_TYPE];
            string valuesForInsert = string.Empty
                , strWhere = string.Empty;

            for (j = 0; j < data.Rows.Count; j++)
            {
                strWhere = getWhereSelect(keyFields, data.Rows[j]);
                dataRows = origin.Select(strWhere);
                if (dataRows.Length == 0)
                {
                    //INSERT
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT] = string.Empty;
                    valuesForInsert = string.Empty;
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT] = "INSERT INTO " + nameTable + " (";
                    for (k = 0; k < data.Columns.Count; k++)
                    {
                        strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT] += data.Columns[k].ColumnName + ",";
                        valuesForInsert += DbTSQLInterface.ValueToQuery(data, j, k) + ",";
                    }
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT] = strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT].Substring(0, strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT].Length - 1);
                    valuesForInsert = valuesForInsert.Substring(0, valuesForInsert.Length - 1);
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT] += ") VALUES (";
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT] += valuesForInsert + ")";
                    DbTSQLInterface.ExecNonQuery(ref conn, strQuery[(int)DbTSQLInterface.QUERY_TYPE.INSERT], null, null, out err);
                }
                else
                {
                    if (dataRows.Length == 1)
                    {//UPDATE
                        bUpdate = false;
                        strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE] = string.Empty;
                        for (k = 0; k < data.Columns.Count; k++)
                        {
                            if (!(data.Rows[j][k].Equals(origin.Rows[j][k]) == true))
                                if (bUpdate == false) bUpdate = true; else ;
                            else
                                ;

                            strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE] += data.Columns[k].ColumnName + "="; // + data.Rows[j][k] + ",";

                            strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE] += DbTSQLInterface.ValueToQuery(data, j, k) + ",";
                        }

                        if (bUpdate == true)
                        {//UPDATE
                            strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE] = strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE].Substring(0, strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE].Length - 1);
                            strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE] = "UPDATE " + nameTable + " SET " + strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE] + " WHERE " + getWhereSelect (keyFields, data.Rows[j]);

                            DbTSQLInterface.ExecNonQuery(ref conn, strQuery[(int)DbTSQLInterface.QUERY_TYPE.UPDATE], null, null, out err);
                        }
                        else
                            ;
                    }
                    else
                        throw new Exception("Невозможно определить тип изменения таблицы " + nameTable);
                }
            }
        }

        //Удаление из оригинальной таблицы записей не существующих в измененной таблице (обязательно наличие поля: ID)
        public static void RecDelete(ref DbConnection conn, string nameTable, string keyFields, DataTable origin, DataTable data, out int err)
        {
            err = (int)Error.NO_ERROR;

            int j = -1;
            DataRow[] dataRows;
            string[] strQuery = new string[(int)DbTSQLInterface.QUERY_TYPE.COUNT_QUERY_TYPE];
            string strWhere = string.Empty;

            for (j = 0; j < origin.Rows.Count; j++)
            {
                strWhere = getWhereSelect(keyFields, origin.Rows[j]);
                dataRows = data.Select(strWhere);
                if (dataRows.Length == 0)
                {
                    //DELETE
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.DELETE] = string.Empty;
                    strQuery[(int)DbTSQLInterface.QUERY_TYPE.DELETE] = "DELETE FROM " + nameTable + " WHERE " + getWhereSelect(keyFields, origin.Rows[j]);
                    DbTSQLInterface.ExecNonQuery(ref conn, strQuery[(int)DbTSQLInterface.QUERY_TYPE.DELETE], null, null, out err);
                }
                else
                {  //Ничего удалять не надо
                    if (dataRows.Length == 1)
                    {
                    }
                    else
                    {
                    }
                }
            }
        }

        public static bool IsConnected(ref DbConnection obj)
        {
            return (!(obj == null)) && (!(obj.State == ConnectionState.Closed)) && (!(obj.State == ConnectionState.Broken));
        }
    }
}
