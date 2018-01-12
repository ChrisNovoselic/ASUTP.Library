using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASUTP.Database {
    /// <summary>
    /// Класс для бращения к источнику данных посредством запросов на T-SQL
    /// </summary>
    public class DbTSQLInterface : DbInterface {
        /// <summary>
        /// Перечисление - перечень возможных ошибок при работе с источником данных
        /// </summary>
        public enum Error {
            NO_ERROR = 0
            , DBCONN_NOT_OPEN = -1, DBCONN_NULL = -2, DBADAPTER_NULL = -3
            , PARAMQUERY_NULL = -4, PARAMQUERY_LENGTH = -5
            , CATCH_DBCONN = -11
            , CATCH_CSV_READ = -21, CATCH_CSV_ROWREAD = -22
            , TABLE_NULL = -31, TABLE_ROWS_0 = -32
        };
        /// <summary>
        /// Перечисление - типы запросов
        /// </summary>
        public enum QUERY_TYPE {
            /// <summary>
            /// Обновление данных
            /// </summary>
            UPDATE
            /// <summary>Вставка новых записей</summary>
            , INSERT
            /// <summary>Удаление данных</summary>
            , DELETE
                /// <summary>Количество типов запросов</summary>
                , COUNT_QUERY_TYPE
        };
        /// <summary>
        /// Строка - сообщение об успешно установленном соединении
        /// </summary>
        public static string MessageDbOpen = "Соединение с базой установлено";
        /// <summary>
        /// Строка - сообщение о разрыве соедиения с источником данных
        /// </summary>
        public static string MessageDbClose = "Соединение с базой разорвано";
        /// <summary>
        /// Строка - сообщение при возникновении исключительной ситуации при работе с БД
        /// </summary>
        public static string MessageDbException = "!Исключение! Работа с БД";
        /// <summary>
        /// Непосредственный объект соединения с источником данных
        /// </summary>
        private DbConnection m_dbConnection;
        private DbCommand m_dbCommand;
        private DbDataAdapter m_dbAdapter;
        /// <summary>
        /// Объект синхронизации при доступе к параметрам соединения с источником данных
        /// </summary>
        private static object lockConn = new object ();
        /// <summary>
        /// Тип источника данных
        /// </summary>
        private DB_TSQL_INTERFACE_TYPE m_connectionType;

        /// <summary>
        /// Конструктор - основной (с аргументами)
        /// </summary>
        /// <param name="type">Тип источника данных</param>
        /// <param name="name">Наименование объекта для доступа к источнику данных</param>
        public DbTSQLInterface (DB_TSQL_INTERFACE_TYPE type, string name)
            : base (name)
        {
            m_connectionType = type;

            m_connectionSettings = new ConnectionSettings ();

            m_dbConnection = createDbConnection ();

            createDbAccessories ();
        }

        protected override int Timeout
        {
            get
            {
                return m_dbCommand.CommandTimeout;
            }

            set
            {
                m_dbCommand.CommandTimeout = value;
            }
        }

        /// <summary>
        /// Создать объект соединения с БД
        /// </summary>
        /// <returns>Объект соединения с БД</returns>
        private DbConnection createDbConnection ()
        {
            return createDbConnection (m_connectionType);
        }

        /// <summary>
        /// Создать объект соединения с БД
        /// </summary>
        /// <param name="type">Тип источника данных</param>
        /// <returns>Объект соединения с БД</returns>
        private static DbConnection createDbConnection (DB_TSQL_INTERFACE_TYPE type)
        {
            DbConnection connRes;

            switch (type) {
                case DB_TSQL_INTERFACE_TYPE.MySQL:
                    connRes = new MySqlConnection ();
                    break;
                case DB_TSQL_INTERFACE_TYPE.MSSQL:
                default:
                    connRes = new SqlConnection ();
                    break;
                case DB_TSQL_INTERFACE_TYPE.Oracle:
                    connRes = new OracleConnection ();
                    break;
                case DB_TSQL_INTERFACE_TYPE.MSExcel:
                    connRes = new OleDbConnection ();
                    break;
            }

            return connRes;
        }

        private bool createDbAccessories ()
        {
            bool bRes = false;

            bRes = !(m_connectionType == DB_TSQL_INTERFACE_TYPE.UNKNOWN)
                && (Equals (m_dbConnection, null) == false);

            if (bRes == true) {
                switch (m_connectionType) {
                    case DB_TSQL_INTERFACE_TYPE.MySQL:
                        m_dbCommand = new MySqlCommand ();
                        m_dbAdapter = new MySqlDataAdapter ();
                        break;
                    case DB_TSQL_INTERFACE_TYPE.MSSQL:
                        m_dbCommand = new SqlCommand ();
                        m_dbAdapter = new SqlDataAdapter ();
                        break;
                    case DB_TSQL_INTERFACE_TYPE.Oracle:
                        m_dbCommand = new OracleCommand ();
                        m_dbAdapter = new OracleDataAdapter ();
                        break;
                    case DB_TSQL_INTERFACE_TYPE.MSExcel:
                        m_dbCommand = new OleDbCommand ();
                        m_dbAdapter = new OleDbDataAdapter ();
                        break;
                    default:
                        break;
                }

                bRes = (Equals (m_dbCommand, null) == false)
                    && (Equals (m_dbAdapter, null) == false);

                if (bRes == true) {
                    m_dbCommand.Connection = m_dbConnection;
                    m_dbCommand.CommandType = CommandType.Text;
                    m_dbAdapter.SelectCommand = m_dbCommand;
                    m_dbAdapter.FillError += new FillErrorEventHandler (getData_OnFillError);
                } else
                    ;
            } else
                ;

            return bRes;
        }

        #region Mode - Статическое обращение к БД

        /// <summary>
        /// Перечисление - возможные режимы разрыва соединения при вызове статических методов обращения к БД
        /// </summary>
        public enum ModeStaticConnectionLeaving {
            /// <summary>
            /// Разрывать соединение
            /// </summary>
            No
            /// <summary>
            /// Оставлять соединение, ожидая что выполняется группа запросов
            ///  , разрыв соединения при изменении режима на 'No'
            /// </summary>
            , Yes
        }

        private static int _counterModeStaticConnectionLeaveChanged = -1;

        private static int _iListenerIdLeaving;

        private static DateTime _datetimeDbConnectionLeaving;

        private static ModeStaticConnectionLeaving _modeStaticConnectionLeave;
        /// <summary>
        /// Режим разрыва соединения при вызове статических методов обращения к БД
        /// </summary>
        public static ModeStaticConnectionLeaving ModeStaticConnectionLeave
        {
            get
            {
                return _modeStaticConnectionLeave;
            }

            set
            {
                Error err = Error.NO_ERROR;

                if (value == ModeStaticConnectionLeaving.Yes) {
                    _datetimeDbConnectionLeaving = DateTime.UtcNow;

                    _counterModeStaticConnectionLeaveChanged += _counterModeStaticConnectionLeaveChanged < 0 ? 2 : 1;
                } else {
                    _counterModeStaticConnectionLeaveChanged--;

                    unregister (out err);
                }

                _modeStaticConnectionLeave = value;
            }
        }

        #endregion

        ///// <summary>
        ///// Установить соединение
        ///// </summary>
        ///// <param name="connSett">Параметры соединения с БД</param>
        ///// <param name="err">Признак ошибкт при установке соединения</param>
        ///// <returns>Признак установки соединения</returns>
        //private static void connect (ConnectionSettings connSett, out Error err)
        //{
        //    err = Error.NO_ERROR;

        //    bool needCreate = true
        //        , needConnect = true;

        //    needCreate = Equals (_dbConnectionLeaving, null);

        //    if (ModeStaticConnectionLeave == ModeStaticConnectionLeaving.No) {
        //        if (needCreate == false) {
        //            disconnect (ref _dbConnectionLeaving, false, false, out err);
        //        } else
        //            ;
        //    } else {
        //        if (needCreate == false)
        //            needConnect = !(_dbConnectionLeaving.State == ConnectionState.Open);
        //        else
        //            ;
        //    }

        //    try {
        //        if (needCreate == true) {
        //            _dbConnectionLeaving = createDbConnection (getTypeDB (connSett));
        //            _dbConnectionLeaving.ConnectionString = connSett.GetConnectionString (getTypeDB (connSett));
        //        } else
        //            ;

        //        if (needConnect == true) {
        //            _dbConnectionLeaving.Open ();

        //            logging_open_db (_dbConnectionLeaving);
        //        } else
        //            ;

        //        err = _dbConnectionLeaving.State == ConnectionState.Open ? Error.NO_ERROR : Error.DBCONN_NOT_OPEN;
        //    } catch (Exception e) {
        //        logging_catch_db (_dbConnectionLeaving, e);
        //    }
        //}

        /// <summary>
        /// Разорвать соединение с БД
        /// </summary>
        /// <param name="conn">Объект соединения с БД</param>
        /// <param name="bIsActive">Признак активного соединения (проверять счетчик или нет)</param>
        /// <param name="bConnIsLogging">Признак определяющий, является ли объект соединения с БД соединением для логгирования</param>
        /// <param name="err">Признак ошибки при выполнении операции</param>
        private static void disconnect (ref DbConnection conn, bool bIsActive, bool bConnIsLogging, out Error err)
        {
            err = (int)Error.NO_ERROR;

            if ((bIsActive == false)
                && (_counterModeStaticConnectionLeaveChanged > 0))
                return;
            else
                ;

            try {
                if (Equals (conn, null) == false) {
                    if (!(conn.State == ConnectionState.Closed)) {
                        conn.Close ();

                        if (bConnIsLogging == false)
                            logging_close_db (conn);
                        else
                            ;
                    } else
                        ;
                } else
                    ;
            } catch (Exception e) {
                if (bConnIsLogging == false)
                    logging_catch_db (conn, e);
                else
                    ;

                err = Error.CATCH_DBCONN;
            } finally {
                if (bIsActive == false)
                    conn = null;
                else
                    ;
            }
        }

        /// <summary>
        /// Установить соединение с источником данных
        /// </summary>
        /// <returns>Признак установки соединения</returns>
        protected override bool Connect ()
        {
            if (((ConnectionSettings)m_connectionSettings).Validate () != ConnectionSettings.ConnectionSettingsError.NoError)
                return false;
            else
                ;

            string conn_str = string.Empty;

            bool bRes = false;

            bRes = m_dbConnection.State == ConnectionState.Open;

            //??? зачем такая сложная конструкция
            try {
                if (bRes == true)
                    return bRes;
                else
                    bRes = true;
            } catch (Exception e) {
                logging_catch_db (m_dbConnection, e);
            }

            bRes = m_dbConnection.State == ConnectionState.Closed;

            if (bRes == false)
                return bRes;
            else
                ;

            lock (lockConnectionSettings) {
                if (IsNeedReconnectNew == true) // если перед приходом в данную точку повторно были изменены настройки, то подключения со старыми настройками не делаем
                    return false;
                else
                    ;

                conn_str = ((ConnectionSettings)m_connectionSettings).GetConnectionString (m_connectionType);

                bRes = !(string.IsNullOrEmpty (conn_str));

                if (bRes == true)
                    m_dbConnection.ConnectionString = conn_str;
                else
                    return bRes;
            }

            try {
                if (bRes == true) {
                    if (IsNeedReconnectHard == true) {
                        m_dbAdapter.FillError -= new FillErrorEventHandler(getData_OnFillError); m_dbAdapter.SelectCommand = null; m_dbAdapter.Dispose(); m_dbAdapter = null;
                        m_dbCommand.Connection = null; m_dbCommand.Dispose(); m_dbCommand = null;

                        createDbAccessories();
                    } else
                        ;

                    m_dbConnection.Open();
                    bRes = m_dbConnection.State == ConnectionState.Open;

                    if ((bRes == true)
                        && (!(((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING))) {
                        logging_open_db(m_dbConnection);
                    } else
                        ;
                } else
                    ;
            } catch (Exception e) {
                if (!(((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING)) {
                    logging_catch_db (m_dbConnection, e);
                } else
                    ;
            }

            return bRes;
        }

        /// <summary>
        /// Сравнить объекты с параметрами соединения с БД
        /// </summary>
        /// <param name="cs">Объект с параметрами соединения для </param>
        /// <returns>Признак идентичности текущегоо объекта и объекта, полученного в аргументе для сравнения</returns>
        public override bool EqualeConnectionSettings(object cs)
        {
            return (m_connectionSettings is ConnectionSettings)
                && ((ConnectionSettings)m_connectionSettings) == ((ConnectionSettings)cs);
        }

        /// <summary>
        /// Признак наличия значений основных параметров для установления соединения
        /// </summary>
        public override bool IsEmptyConnectionSettings
        {
            get
            {
                return ((ConnectionSettings)m_connectionSettings).IsEmpty;
            }
        }

        /// <summary>
        /// Установить параметры соединения с источником данных
        /// </summary>
        /// <param name="cs">Объект с параметрами соединения</param>
        /// <param name="bStarted">Признак немедленной активации объекта доступа к источнику данных</param>
        public override void SetConnectionSettings (object cs, bool bStarted)
        {
            lock (lockConnectionSettings) {
                setConnectionSettings(cs);

                ((ConnectionSettings)m_connectionSettings).id = ((ConnectionSettings)cs).id;
                ((ConnectionSettings)m_connectionSettings).server = ((ConnectionSettings)cs).server;
                ((ConnectionSettings)m_connectionSettings).instance = ((ConnectionSettings)cs).instance;
                ((ConnectionSettings)m_connectionSettings).port = ((ConnectionSettings)cs).port;
                ((ConnectionSettings)m_connectionSettings).dbName = ((ConnectionSettings)cs).dbName;
                ((ConnectionSettings)m_connectionSettings).userName = ((ConnectionSettings)cs).userName;
                ((ConnectionSettings)m_connectionSettings).password = ((ConnectionSettings)cs).password;
                //((ConnectionSettings)m_connectionSettings).ignore = ((ConnectionSettings)cs).ignore;
            }

            if (bStarted == true)
                //base.SetConnectionSettings (cs); //базовой function 'cs' не нужен
                setConnectionSettings ();
            else
                ;
        }

        /// <summary>
        /// Отменить установку соединения с источником данных
        /// </summary>
        /// <returns>Признак выполнения операции разъединения</returns>
        protected override bool Disconnect ()
        {
            Error error = Error.NO_ERROR;

            disconnect (ref m_dbConnection, true, ((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING, out error);

            return error == Error.NO_ERROR;
        }

        /// <summary>
        /// Разорвать установленное соединение с источником данных
        /// </summary>
        /// <param name="er">Признак наличия ошибки при выполнении операции</param>
        public override void Disconnect (out int er)
        {
            er = -1;
            Error error = Error.CATCH_DBCONN;

            disconnect (ref m_dbConnection, true, ((ConnectionSettings)m_connectionSettings).id == ConnectionSettings.ID_LISTENER_LOGGING, out error);

            er = (int)Error.NO_ERROR;
        }

        /// <summary>
        /// Инициировать отмену выполняющегося запроса
        /// </summary>
        protected override void GetDataCancel ()
        {
            m_dbCommand?.Cancel ();
        }

        /// <summary>
        /// Получить результат запроса (KhryapinAN DD.09.2017 выполняется в отдельном потоке)
        /// </summary>
        /// <param name="table">Таблица - результат запроса</param>
        /// <param name="query">Запрос к источнику данных на T-SQL</param>
        /// <returns>Признак получения результата</returns>
        protected override bool GetData (DataTable table, object query)
        {
            //Thread.Sleep(Timeout * 1000);

            if (m_dbConnection.State != ConnectionState.Open)
                return false;
            else
                ;

            bool result = false;

            try {
                m_dbCommand.CommandText = query.ToString ();
            } catch (Exception e) {
                logging_catch_db(m_dbConnection, e);
            }

            table.Reset ();
            table.Locale = System.Globalization.CultureInfo.InvariantCulture;

            try {
                m_dbAdapter.Fill (table);
                result = true;
            } catch (DbException e) {
                logging_catch_db (m_dbConnection, e);
            } catch (Exception e) {
                logging_catch_db (m_dbConnection, e);
            } finally {
            }

            if (result == false)
                Logging.Logg ().Error (@"DbTSQLInterface::GetData () - query=" + query, Logging.INDEX_MESSAGE.NOT_SET);
            else
                ;

            return result;
        }

        /// <summary>
        /// Строка соединения для логгирования с усечением значения пароля
        /// </summary>
        /// <param name="strConnSett">Строка соединения с БД</param>
        /// <returns>Строка для размещения в журнале приложения</returns>
        private static string ConnectionStringToLog (string strConnSett)
        {
            string strRes = string.Empty;
            int pos = -1;

            pos = strConnSett.IndexOf ("Password", StringComparison.CurrentCultureIgnoreCase);
            if (pos < 0)
                strRes = strConnSett;
            else
                strRes = strConnSett.Substring (0, pos);

            return strRes;
        }

        /// <summary>
        /// Зафиксировать/разместить в журнале сообщение о возникновении исключительной ситуации
        /// </summary>
        /// <param name="conn">Объект соединения с БД(источником данных)</param>
        /// <param name="e">Исключение, требуещще журналирования</param>
        private static void logging_catch_db (DbConnection conn, Exception e)
        {
            string s = string.Empty, log = string.Empty;
            if (!(conn == null))
                s = ConnectionStringToLog (conn.ConnectionString);
            else
                s = @"Объект 'DbConnection' = null";

            log = MessageDbException;
            log += Environment.NewLine + "Строка соединения: " + s;
            if (!(e == null)) {
                log += Environment.NewLine + "Ошибка: " + e.Message;
                log += Environment.NewLine + e.ToString ();
            } else
                ;
            Logging.Logg ().ExceptionDB (log);
        }

        /// <summary>
        /// Зафиксировать/разместить в журнале сообщение о разрыве соединения с БД (источником данных)
        /// </summary>
        /// <param name="conn">Объект соединения с БД(источником данных)</param>
        private static void logging_close_db (DbConnection conn)
        {
            string s = ConnectionStringToLog (conn.ConnectionString);

            Logging.Logg ().Debug (MessageDbClose + " (" + s + ")", Logging.INDEX_MESSAGE.NOT_SET);
        }

        /// <summary>
        /// Зафиксировать/разместить в журнале сообщение об установке соединения с БД (источником данных)
        /// </summary>
        /// <param name="conn">Объект соединения с БД(источником данных)</param>
        private static void logging_open_db (DbConnection conn)
        {
            string s = ConnectionStringToLog (conn.ConnectionString);

            Logging.Logg ().Debug (MessageDbOpen + " (" + s + ")", Logging.INDEX_MESSAGE.NOT_SET, true);
        }

        /// <summary>
        /// Возвратить тип источника данных по объекту с параметрами для соединения
        /// </summary>
        /// <param name="connSett">Объект с параметрами для установления соединения</param>
        /// <returns>Тип источника данных</returns>
        public static DbTSQLInterface.DB_TSQL_INTERFACE_TYPE getTypeDB (ConnectionSettings connSett)
        {
            return getTypeDB (connSett.port);
        }

        /// <summary>
        /// Возвратить тип источника данных по строке соединения
        ///  (извлекается номер порта, по нему и определяется искомый тип)
        ///  , не применим, если в строке соединения не указан номер порта
        ///  , по умолчанию возвращается 'MS SQL'
        /// </summary>
        /// <param name="strConn">Строка соединения с источником данных</param>
        /// <returns>Тип источника данных</returns>
        public static DbTSQLInterface.DB_TSQL_INTERFACE_TYPE getTypeDB (string strConn)
        {
            DB_TSQL_INTERFACE_TYPE res = DB_TSQL_INTERFACE_TYPE.MSSQL;
            int port = -1;
            string strMarkPort = @"port="
                , strPort = string.Empty;

            if (!(strConn.IndexOf (strMarkPort) < 0)) {
                int iPosPort = strConn.IndexOf (strMarkPort) + strMarkPort.Length;
                strPort = strConn.Substring (iPosPort, strConn.IndexOf (';', iPosPort) - iPosPort);

                if (Int32.TryParse (strPort, out port) == true)
                    res = getTypeDB (port);
                else
                    ;
            } else
                ;

            return res;
        }

        /// <summary>
        /// Возвратить тип источника данных по номеру порта
        ///  , если не удается идентифицировать источник, возвращается 'неизвестный тип'
        /// </summary>
        /// <param name="port">Номер порта для связи с источником данных</param>
        /// <returns>Тип источника данных</returns>
        public static DbTSQLInterface.DB_TSQL_INTERFACE_TYPE getTypeDB (int port)
        {
            DbTSQLInterface.DB_TSQL_INTERFACE_TYPE typeDBRes = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.UNKNOWN;

            switch (port) {
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

        /// <summary>
        /// Возвратить объект соединения с БД (для синхронных операций)
        /// </summary>
        /// <param name="err">Признак ошибки при выполнении метода, актуальности возвращаемого значения</param>
        /// <returns></returns>
        public DbConnection GetConnection (out int err)
        {
            err = (int)Error.NO_ERROR;

            // проверить значение _needReconnect: для для синхронных операций ВСЕГДА = 'SOFT' (из конструктора)
            bool bRes = Connect ();

            if (bRes == true)
                return m_dbConnection;
            else {
                err = (int)Error.DBCONN_NOT_OPEN;

                return null;
            }
        }

        /// <summary>
        /// Подтвердить наличие поля в таблице-результате запроса
        /// </summary>
        /// <param name="data">Таблица</param>
        /// <param name="nameField">Наименование поля</param>
        /// <returns>Признак наличия поля</returns>
        public static bool IsNameField (DataTable data, string nameField)
        {
            return data.Columns.IndexOf (nameField) > -1 ? true : false;
        }

        /// <summary>
        /// Подтвердить наличие поля в строке таблицы-результата запроса
        /// </summary>
        /// <param name="data">Строка таблицы</param>
        /// <param name="nameField">Наименование поля</param>
        /// <returns>Признак наличия поля</returns>
        public static bool IsNameField (DataRow data, string nameField)
        {
            return IsNameField (data.Table, nameField);
        }

        /// <summary>
        /// Преобразование/подготовка значение для использования его в строке запроса
        ///  - добавление в начале и в конце значения одинарные кавычки
        ///  , если тип значения "простой"
        /// </summary>
        /// <param name="table">Таблица со значениями</param>
        /// <param name="row">Номер записи в таблице со значениями</param>
        /// <param name="col">Номер столбца в записи таблицы со значениями</param>
        /// <returns>Строка - значение из таблицы с одинарными кавычками или без них</returns>
        public static string ValueToQuery (DataTable table, int row, int col)
        {
            return ValueToQuery (table.Rows [row] [col], table.Columns [col].DataType);
        }

        /// <summary>
        /// Преобразование/подготовка значение для использования его в строке запроса
        /// </summary>
        /// <param name="val">Значение для преобразования</param>
        /// <param name="type">Тип столбца в таблице, для размещения в него значения</param>
        /// <returns>Строка - значение из таблицы с одинарными кавычками или без них</returns>
        public static string ValueToQuery (object val, Type type)
        {
            string strRes = string.Empty
                , strVal = string.Empty;
            bool bQuote =
                //table.Columns[col].DataType.IsByRef;
                !type.IsPrimitive;

            switch (type.Name) {
                case "DateTime":
                    strVal = Convert.ToDateTime (val).ToString (@"yyyyMMdd HH:mm:ss.fff");//(@"yyyyMMdd HH:mm:ss.fff"); //System.Globalization.CultureInfo.InvariantCulture
                    break;
                case @"Double":
                case @"double":
                    strVal = ((double)val).ToString (System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case @"Single":
                case @"single":
                case @"Float":
                case @"float":
                    strVal = ((float)val).ToString (System.Globalization.CultureInfo.InvariantCulture);
                    break;
                default:
                    //??? не учитывается может поле принимать значение NULL
                    strVal = val.ToString ();
                    if (strVal.Length == 0)
                        strVal = "NULL";
                    else
                        strVal = strVal.Replace (@"'", @"''");
                    break;
            }

            strRes = (bQuote ? "'" : string.Empty) + strVal + (bQuote ? "'" : string.Empty);

            return strRes;
        }

        /// <summary>
        /// Проверка и преобразование запроса к БД в зависимости от типа БД (MSSQL, MySql)
        /// </summary>
        /// <param name="strConn">строка соединения объекта DbConnection</param>
        /// <param name="query">преобразуемый запрос</param>
        private static void queryValidateOfTypeDB (string strConn, ref string query)
        {
            switch (getTypeDB (strConn)) {
                case DB_TSQL_INTERFACE_TYPE.MySQL:
                    query = query.Replace (@"[dbo].", string.Empty);
                    query = query.Replace ('[', '`');
                    query = query.Replace (']', '`');
                    break;
                case DB_TSQL_INTERFACE_TYPE.MSSQL:
                case DB_TSQL_INTERFACE_TYPE.Oracle:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Импорт содержания текстового файла с разделителем в таблицу
        /// </summary>
        /// <param name="path">путь к файлу</param>
        /// <param name="fields">наименования полей таблицы (не используется)</param>
        /// <param name="er">Признак ошибки при выполнении операции</param>
        /// <returns></returns>
        public static DataTable CSVImport (string path, string fields, out int er)
        {
            er = (int)Error.NO_ERROR;

            DataTable dataTableRes = new DataTable ();

            string [] data;
            //Открыть поток чтения файла...
            try {
                StreamReader sr = new StreamReader (path);
                //Из 1-ой строки сформировать столбцы...
                data = sr.ReadLine ().Split (';');
                foreach (string field in data)
                    dataTableRes.Columns.Add (field, typeof (string));
                //Из остальных строк сформировать записи...
                while (sr.EndOfStream == false) {
                    try {
                        data = sr.ReadLine ().Split (';');
                        dataTableRes.Rows.Add (data);
                    } catch (Exception e) {
                        er = (int)Error.CATCH_CSV_ROWREAD;
                        break;
                    }
                }

                //Закрыть поток чтения файла
                sr.Close ();
            } catch (Exception e) {
                er = (int)Error.CATCH_CSV_READ;
            }

            return dataTableRes;
        }

        /// <summary>
        /// Тип делегата для функции обратного вызова при аснхронном запросе к источнику данных синхронного типа
        /// </summary>
        /// <param name="table">Таблица - результат запроса</param>
        /// <param name="err">Признак ошибки при выполнении запроса</param>
        public delegate void DelegateSelectAsync (out DataTable table, out Error err);

        /// <summary>
        /// Отправить/выполнить запрос к источнику данных
        /// </summary>
        /// <param name="path">Путь к источнику данных (MS Excel, MS Access)</param>
        /// <param name="query">Строка - запрос</param>
        /// <param name="er">Признак ошибки при отправлении запроса</param>
        /// <returns>Результат выполнения запроса</returns>
        public static DataTable Select (string path, string query, out int er)
        {
            er = (int)Error.NO_ERROR;

            DataTable dataTableRes = new DataTable ();

            OleDbConnection connectionOleDB = null;
            System.Data.OleDb.OleDbCommand commandOleDB;
            System.Data.OleDb.OleDbDataAdapter adapterOleDB;

            if (path.IndexOf (".xls") > -1)
                connectionOleDB = new OleDbConnection (ConnectionSettings.GetConnectionStringExcel (path));
            else {
                if (path.IndexOf ("CSV_DATASOURCE=") > -1)
                    connectionOleDB = new OleDbConnection (ConnectionSettings.GetConnectionStringCSV (path.Remove (0, "CSV_DATASOURCE=".Length)));
                else {
                    connectionOleDB = new OleDbConnection (ConnectionSettings.GetConnectionStringDBF (path));
                }
            }

            if (!(connectionOleDB == null)) {
                commandOleDB = new OleDbCommand ();
                commandOleDB.Connection = connectionOleDB;
                commandOleDB.CommandType = CommandType.Text;

                adapterOleDB = new OleDbDataAdapter ();
                adapterOleDB.SelectCommand = commandOleDB;

                commandOleDB.CommandText = query;

                dataTableRes.Reset ();
                dataTableRes.Locale = System.Globalization.CultureInfo.InvariantCulture;

                try {
                    connectionOleDB.Open ();

                    if (connectionOleDB.State == ConnectionState.Open) {
                        adapterOleDB.Fill (dataTableRes);
                    } else
                        ; //
                } catch (OleDbException e) {
                    logging_catch_db (connectionOleDB, e);

                    er = (int)Error.CATCH_DBCONN;
                }

                connectionOleDB.Close ();
            } else
                ;

            return dataTableRes;
        }

        private static void register (ConnectionSettings connSett, out Error err)
        {
            err = Error.NO_ERROR;

            _iListenerIdLeaving = DbSources.Sources ().Register (connSett, false, connSett.name, false);
        }

        private static void unregister (out Error err)
        {
            err = Error.NO_ERROR;

            DbSources.Sources ().UnRegister (_iListenerIdLeaving);
            _iListenerIdLeaving = -1;
        }

        /// <summary>
        /// Отправить/выполнить запрос не требующий возвращения результата
        /// </summary>
        /// <param name="connSett">Параметры соединения с БД</param>
        /// <param name="query">Запрос для выполнения</param>
        /// <param name="er">Признак ошибки при выполнеии запроса</param>
        public static DataTable Select (ConnectionSettings connSett, string query, out int er)
        {
        //!!! Внимание. Полная копия метода 'ExecNonQuery'. устранить дублирование
            DataTable tableRes;
            er = 0;

            Error err = Error.NO_ERROR;
            DbConnection dbConn = null;

            if ((ModeStaticConnectionLeave == ModeStaticConnectionLeaving.No)
                && (!(_iListenerIdLeaving < 0))) {
            // нештатная ситуация: соединение удерживать не требуется, но идентификатор имеет признак регистрации
                // отменить регистрацию
                unregister (out err);

                Logging.Logg ().Error (@"DbTSQLInterface::Select() - несоответствие значения подписчика и режима удержания соединения...", Logging.INDEX_MESSAGE.NOT_SET);
            } else if ((ModeStaticConnectionLeave == ModeStaticConnectionLeaving.Yes)
                && (_iListenerIdLeaving < 0)) {
            // нештатная ситуация: требуется удерживать соединение, но идендификатор имеет признак отсутствия регистрации
            } else
            // штатная ситуация
                ;

            if (_iListenerIdLeaving < 0)
                register (connSett, out err);
            else
                ;

            if (err == Error.NO_ERROR) {
                dbConn = DbSources.Sources ().GetConnection (_iListenerIdLeaving, out er);

                if (er == 0) {
                    tableRes = Select (ref dbConn, query, null, null, out er);
                    err = (Error)er;

                    if (ModeStaticConnectionLeave == ModeStaticConnectionLeaving.No) {
                        unregister(out err);
                    } else
                        ;
                } else
                    tableRes = new DataTable ();
            } else
                tableRes = new DataTable ();

            return tableRes;
        }

        /// <summary>
        /// Отправить/выполнить запрос асинхронно к источнику данных синхронного типа
        /// </summary>
        /// <param name="path">Путь к источнику данных (MS Excel, MS Access)</param>
        /// <param name="query">Строка - запрос</param>
        /// <param name="fCallback">Делегат(метод/функция) обратного вызова</param>
        public static void Select (string path, string query, DelegateSelectAsync fCallback)
        {
            DataTable tableRes = new DataTable ();
            Error err = Error.NO_ERROR;

            int er = -1;

            new Thread (new ParameterizedThreadStart (delegate (object obj) {
                tableRes = Select (path, query, out er);

                err = (Error)er;
                fCallback (out tableRes, out err);
            })).Start (null);
        }

        /// <summary>
        /// Отправить/выполнить запрос(параметризованный) к источнику данных
        /// </summary>
        /// <param name="conn">Объект соединения с источником данных</param>
        /// <param name="query">Строка - запрос к источнику данных</param>
        /// <param name="types">Список типов параметров в запросе(НЕ РЕАЛИЗОВАНО)</param>
        /// <param name="parametrs">Список параметров в запросе(НЕ РЕАЛИЗОВАНО)</param>
        /// <param name="er">Признак ошибки при выпонении запроса</param>
        /// <returns>Результат - таблица выполнения запроса</returns>
        public static DataTable Select (ref DbConnection conn, string query, DbType [] types, object [] parametrs, out int er)
        {
            er = (int)Error.NO_ERROR;
            DataTable dataTableRes = null;

            if (conn == null)
                er = (int)Error.DBCONN_NULL;
            else {
                lock (lockConn) {
                    dataTableRes = new DataTable ();

                    queryValidateOfTypeDB (conn.ConnectionString, ref query);

                    ParametrsValidate (types, parametrs, out er);

                    if (er == 0) {
                        DbCommand cmd = null;
                        DbDataAdapter adapter = null;

                        if (conn is MySqlConnection) {
                            cmd = new MySqlCommand ();
                            adapter = new MySqlDataAdapter ();
                        } else if (conn is SqlConnection) {
                            cmd = new SqlCommand ();
                            adapter = new SqlDataAdapter ();
                        } else if (conn is OracleConnection) {
                            cmd = new OracleCommand ();
                            adapter = new OracleDataAdapter ();
                        } else
                            ;

                        if ((!(cmd == null)) && (!(adapter == null))) {
                            cmd.Connection = conn;
                            cmd.CommandType = CommandType.Text;

                            adapter.SelectCommand = cmd;

                            cmd.CommandText = query;
                            ParametrsAdd (cmd, types, parametrs);

                            dataTableRes.Reset ();
                            dataTableRes.Locale = System.Globalization.CultureInfo.InvariantCulture;

                            try {
                                if (conn.State == ConnectionState.Open) {
                                    adapter.Fill (dataTableRes);
                                } else
                                    er = (int)Error.DBCONN_NOT_OPEN;
                            } catch (Exception e) {
                                logging_catch_db (conn, e);

                                er = (int)Error.CATCH_DBCONN;
                            }
                        } else
                            er = (int)Error.DBADAPTER_NULL;
                    } else {
                        // Логгирование в 'ParametrsValidate'
                    }
                }
            }

            return dataTableRes;
        }

        /// <summary>
        /// Отправить/выполнить запрос(параметризованный) асинхронно к источнику данных синхронного типа
        /// </summary>
        /// <param name="conn">Объект соединения с источником данных</param>
        /// <param name="query">Строка - запрос</param>
        /// <param name="types">Список типов параметров в запросе(НЕ РЕАЛИЗОВАНО)</param>
        /// <param name="parametrs">Список значений параметров в запросе(НЕ РЕАЛИЗОВАНО)</param>
        /// <param name="fCallback">Делегат(метод/функция) обратного вызова</param>
        public static void Select (ref DbConnection conn, string query, DbType [] types, object [] parametrs, DelegateSelectAsync fCallback)
        {
            DataTable tableRes = new DataTable ();
            Error err = Error.NO_ERROR;

            DbConnection conn_ = conn;
            int er = (int)Error.DBCONN_NOT_OPEN;

            new Thread (new ParameterizedThreadStart (delegate (object obj) {
                tableRes = Select (ref conn_, query, types, parametrs, out er);

                err = (Error)er;
                fCallback (out tableRes, out err);
            })).Start (null);
        }

        /// <summary>
        /// Добавить параметр к запросу
        /// </summary>
        /// <param name="cmd">Объект 'команда' в составе запроса к источнику данных</param>
        /// <param name="types">Тип парметра</param>
        /// <param name="parametrs">Значение параметра</param>
        private static void ParametrsAdd (DbCommand cmd, DbType [] types, object [] parametrs)
        {
            if ((!(types == null)) && (!(parametrs == null)))
                foreach (DbType type in types) {
                    //cmd.Parameters.AddWithValue(string.Empty, parametrs[commandMySQL.Parameters.Count - 1]);
                    //cmd.Parameters.Add(new SqlParameter(cmd.Parameters.Count.ToString (), parametrs[cmd.Parameters.Count]));
                    cmd.Parameters.Add (new SqlParameter (string.Empty, parametrs [cmd.Parameters.Count]));
                }
            else
                ;
        }

        /// <summary>
        /// Проверить параметры для возможности использования в запросе
        /// </summary>
        /// <param name="types">Список типов параметров в запросе</param>
        /// <param name="parametrs">Список значений параметров в запросе</param>
        /// <param name="err">Признак ошибки при выполнеии метода</param>
        private static void ParametrsValidate (DbType [] types, object [] parametrs, out int err)
        {
            err = (int)Error.NO_ERROR;

            //if ((!(types == null)) || (!(parametrs == null)))
            if ((types == null) || (parametrs == null))
                ;
            else
                if ((!(types == null)) && (!(parametrs == null))) {
                if (!(types.Length == parametrs.Length)) {
                    err = (int)Error.PARAMQUERY_LENGTH;
                } else
                    ;
            } else
                err = (int)Error.PARAMQUERY_NULL;

            if (!(err == 0)) {
                Logging.Logg ().Error ("!Ошибка! static DbTSQLInterface::ParametrsValidate () - types OR parametrs не корректны", Logging.INDEX_MESSAGE.NOT_SET);
            } else
                ;
        }

        /// <summary>
        /// Выполнить запрос, не требущий возвращения результатат, синхронно 
        /// </summary>
        /// <param name="conn">Объект соединения с источником данных</param>
        /// <param name="query">Строка - запрос</param>
        /// <param name="types">Список типов параметров в запросе(НЕ РЕАЛИЗОВАНО)</param>
        /// <param name="parametrs">Список значений параметров в запросе(НЕ РЕАЛИЗОВАНО)</param>
        /// <param name="er">Признак ошибки при выполнеини запроса</param>
        public static void ExecNonQuery (ref DbConnection conn, string query, DbType [] types, object [] parametrs, out int er)
        {
            er = (int)Error.NO_ERROR;

            DbCommand cmd = null;

            queryValidateOfTypeDB (conn.ConnectionString, ref query);

            ParametrsValidate (types, parametrs, out er);

            if (er == 0) {
                lock (lockConn) {
                    if (conn is MySqlConnection) {
                        cmd = new MySqlCommand ();
                    } else if (conn is SqlConnection) {
                        cmd = new SqlCommand ();
                    } else
                        ;

                    if (!(cmd == null)) {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;

                        cmd.CommandText = query;
                        ParametrsAdd (cmd, types, parametrs);

                        try {
                            if (conn.State == ConnectionState.Open) {
                                cmd.ExecuteNonQuery ();
                            } else
                                er = (int)Error.DBCONN_NOT_OPEN;
                        } catch (Exception e) {
                            logging_catch_db (conn, e);

                            er = (int)Error.CATCH_DBCONN;
                        }
                    } else
                        er = (int)Error.DBADAPTER_NULL;
                }
            } else
                ;
        }

        /// <summary>
        /// Выполнить запрос не требующий возвращения результата
        /// </summary>
        /// <param name="connSett">Объект с параметрами соединения</param>
        /// <param name="query">Запрос для выполнения</param>
        /// <param name="er">Признак ошибки при выполнении операции</param>
        public static void ExecNonQuery (ConnectionSettings connSett, string query, out int er)
        {
        //!!! Внимание. Полная копия метода 'Select'. устранить дублирование
            er = 0;

            Error err = Error.NO_ERROR;
            DbConnection dbConn = null;

            if ((ModeStaticConnectionLeave == ModeStaticConnectionLeaving.No)
                && (!(_iListenerIdLeaving < 0))) {
                // нештатная ситуация: соединение удерживать не требуется, но идентификатор имеет признак регистрации
                // отменить регистрацию
                unregister (out err);

                Logging.Logg ().Error (@"DbTSQLInterface::Select() - несоответствие значения подписчика и режима удержания соединения...", Logging.INDEX_MESSAGE.NOT_SET);
            } else if ((ModeStaticConnectionLeave == ModeStaticConnectionLeaving.Yes)
                && (_iListenerIdLeaving < 0)) {
                // нештатная ситуация: требуется удерживать соединение, но идендификатор имеет признак отсутствия регистрации
            } else
                // штатная ситуация
                ;

            if (_iListenerIdLeaving < 0)
                register (connSett, out err);
            else
                ;

            if (err == Error.NO_ERROR) {
                dbConn = DbSources.Sources ().GetConnection (_iListenerIdLeaving, out er);

                if (er == 0) {
                    ExecNonQuery (ref dbConn, query, null, null, out er);
                    err = (Error)er;

                    if (ModeStaticConnectionLeave == ModeStaticConnectionLeaving.No) {
                        unregister (out err);
                    } else
                        ;
                } else
                    ;
            } else
                ;
        }

        /// <summary>
        /// Выполнить запрос не требующий возвращения результата
        /// </summary>
        /// <param name="path">Путь к источнику данных (файловому: MS Excel, MS Access)</param>
        /// <param name="query">Запрос для выполнения</param>
        /// <param name="er">Признак ошибки при выполнении операции</param>
        public static void ExecNonQuery (string path, string query, out int er)
        {
            er = 0;

            OleDbConnection connectionOleDB = null;
            System.Data.OleDb.OleDbCommand commandOleDB;

            if (path.IndexOf ("xls") > -1)
                connectionOleDB = new OleDbConnection (ConnectionSettings.GetConnectionStringExcel (path));
            else
                //if (path.IndexOf ("dbf") > -1)
                connectionOleDB = new OleDbConnection (ConnectionSettings.GetConnectionStringDBF (path));
            //else
            //    ;

            if (!(connectionOleDB == null)) {
                commandOleDB = new OleDbCommand ();
                commandOleDB.Connection = connectionOleDB;
                commandOleDB.CommandType = CommandType.Text;

                commandOleDB.CommandText = query;

                try {
                    connectionOleDB.Open ();

                    if (connectionOleDB.State == ConnectionState.Open) {
                        commandOleDB.ExecuteNonQuery ();
                    } else
                        ; //
                } catch (Exception e) {
                    logging_catch_db (connectionOleDB, e);

                    er = (int)Error.CATCH_DBCONN;
                }

                connectionOleDB.Close ();
            } else
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
        /// <returns>Очередной(по сквозной нумерации) идентификатор</returns>
        public static Int32 GetIdNext (ref DbConnection conn, string nameTable, out int err, string nameFieldID = @"ID", Int32 min = 0, Int32 max = Int32.MaxValue)
        {
            Int32 idRes = -1;
            err = (int)Error.NO_ERROR;

            lock (lockConn) {
                idRes = Convert.ToInt32 (Select (ref conn, "SELECT MAX(" + nameFieldID + @") FROM " + nameTable + @" WHERE "
                        + nameFieldID + @">" + min + @" AND " + nameFieldID + @"<" + max
                    , null, null, out err).Rows [0] [0]);
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
        public static Int32 GetIdNext (DataTable table, out int err, string nameFieldID = @"ID", Int32 min = 0, Int32 max = Int32.MaxValue)
        {
            Int32 idRes = -1;
            err = (int)Error.NO_ERROR;
            DataRow [] rangeRows = null;

            if (table.Rows.Count > 0) {
                rangeRows = table.Select (nameFieldID + @">=" + min + @" AND " + nameFieldID + @"<" + max, nameFieldID + @" DESC");

                if (rangeRows.Length > 0)
                    idRes = Convert.ToInt32 (rangeRows [0] [nameFieldID]);
                else
                    ;
            } else
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
        /// <param name="unchangeableColumn">Наименования полей таблицы не подлежащие изменению</param>
        /// <param name="origin">Таблица со значениями - исходная</param>
        /// <param name="data">Таблица со значениями - с изменениями</param>
        /// <param name="err">Признак ошибки выполнения функции</param>
        public static void RecUpdateInsertDelete (ref DbConnection conn, string nameTable, string keyFields, string unchangeableColumn, DataTable origin, DataTable data, out int err)
        {
            if (!(data.Rows.Count < origin.Rows.Count)) {
                //UPDATE, INSERT
                RecUpdateInsert (ref conn, nameTable, keyFields, unchangeableColumn, origin, data, out err);
            } else {
                //DELETE
                RecDelete (ref conn, nameTable, keyFields, origin, data, out err);
            }
        }

        /// <summary>
        /// Возвратить предложение 'WHERE' для запроса
        /// </summary>
        /// <param name="fields">Список полей в предложении WHERE</param>
        /// <param name="r">Строка со значенями для включения в результирующую строку</param>
        /// <returns>Строка - предложение 'WHERE (часть запроса)'</returns>
        private static string getWhereSelect (string fields, DataRow r)
        {
            string strRes = string.Empty;

            string [] arFields = fields.Split (',');

            for (int i = 0; i < arFields.Length; i++) {
                arFields [i] = arFields [i].Trim ();
                if (arFields [i] == "DATE_TIME")
                    strRes += String.Format (r.Table.Locale, @"DATE_TIME = '{0:o}'", r [arFields [i]]) + @" AND ";
                else
                    strRes += arFields [i] + @"=" + DbTSQLInterface.ValueToQuery (r [arFields [i]], r [arFields [i]].GetType ()) + @" AND ";
            }

            if (strRes.Equals (string.Empty) == false)
                strRes = strRes.Substring (0, strRes.Length - @" AND ".Length);
            else
                ;

            return strRes;
        }

        /// <summary>
        /// Изменение (вставка) в оригинальную таблицу записей измененных (добавленных) в измененную таблицу (обязательно наличие поля: ID)
        /// </summary>
        /// <param name="conn">Объект с параметрами соединения с БД</param>
        /// <param name="nameTable">Наименование таблицы в БД</param>
        /// <param name="keyFields">Набор ключевых полей по которым определяется изменение/добавление/удаление записи</param>
        /// <param name="unchangeableColumn">Наименование поля, которое не проверяется на изменение/добавление/удаление</param>
        /// <param name="origin">Оригинальная таблица со значениями</param>
        /// <param name="data">Таблица с внесенными изменениями</param>
        /// <param name="err">Признак ошибки выполнения функции</param>
        public static void RecUpdateInsert (ref DbConnection conn, string nameTable, string keyFields, string unchangeableColumn
            , DataTable origin, DataTable data, out int err)
        {
            err = (int)Error.NO_ERROR;

            int j = -1, k = -1;
            bool bUpdate = false;
            DataRow [] originRows;
            string [] strQuery = new string [(int)DbTSQLInterface.QUERY_TYPE.COUNT_QUERY_TYPE];
            string valuesForInsert = string.Empty
                , strWhere = string.Empty;

            for (j = 0; j < data.Rows.Count; j++) {
                strWhere = getWhereSelect (keyFields, data.Rows [j]);
                originRows = origin.Select (strWhere);

                if (originRows.Length == 0) {
                    //INSERT
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT] = string.Empty;
                    valuesForInsert = string.Empty;
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT] = "INSERT INTO " + nameTable + " (";
                    for (k = 0; k < data.Columns.Count; k++) {
                        if (data.Columns [k].ColumnName != unchangeableColumn) {
                            strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT] += data.Columns [k].ColumnName + ",";
                            valuesForInsert += DbTSQLInterface.ValueToQuery (data, j, k) + ",";
                        }
                    }
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT] = strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT].Substring (0, strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT].Length - 1);
                    valuesForInsert = valuesForInsert.Substring (0, valuesForInsert.Length - 1);
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT] += ") VALUES (";
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT] += valuesForInsert + ")";
                    DbTSQLInterface.ExecNonQuery (ref conn, strQuery [(int)DbTSQLInterface.QUERY_TYPE.INSERT], null, null, out err);
                } else {
                    if (originRows.Length == 1) {//UPDATE
                        bUpdate = false;
                        strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE] = string.Empty;
                        for (k = 0; k < data.Columns.Count; k++) {
                            if (data.Columns [k].ColumnName != unchangeableColumn) {
                                if (!(data.Rows [j] [k].Equals (originRows [0] [k]) == true)) {

                                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE] += data.Columns [k].ColumnName + "="; // + data.Rows[j][k] + ",";

                                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE] += DbTSQLInterface.ValueToQuery (data, j, k) + ",";

                                    if (bUpdate == false)
                                        bUpdate = true;
                                    else
                                        ;
                                } else
                                    ;
                            }
                        }

                        if (bUpdate == true) {//UPDATE
                            strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE] = strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE].Substring (0, strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE].Length - 1);
                            strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE] = "UPDATE " + nameTable + " SET " + strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE] + " WHERE " + getWhereSelect (keyFields, data.Rows [j]);

                            DbTSQLInterface.ExecNonQuery (ref conn, strQuery [(int)DbTSQLInterface.QUERY_TYPE.UPDATE], null, null, out err);
                        } else
                            ;
                    } else
                        throw new Exception ("Невозможно определить тип изменения таблицы " + nameTable);
                }
            }
        }

        /// <summary>
        /// Удаление из оригинальной таблицы записей не существующих в измененной таблице (обязательно наличие поля: ID)
        /// </summary>
        /// <param name="conn">Объект с параметрами соединения с БД</param>
        /// <param name="nameTable">Наименование таблицы в БД</param>
        /// <param name="keyFields">Набор ключевых полей по которым определяется изменение/добавление/удаление записи</param>
        /// <param name="origin">Оригинальная(исходная) таблица со значениями</param>
        /// <param name="data">Таблица с внесенными изменениями</param>
        /// <param name="err">Признак ошибки выполнения функции</param>
        public static void RecDelete (ref DbConnection conn, string nameTable, string keyFields, DataTable origin, DataTable data, out int err)
        {
            err = (int)Error.NO_ERROR;

            int j = -1;
            DataRow [] dataRows;
            string [] strQuery = new string [(int)DbTSQLInterface.QUERY_TYPE.COUNT_QUERY_TYPE];
            string strWhere = string.Empty;

            for (j = 0; j < origin.Rows.Count; j++) {
                strWhere = getWhereSelect (keyFields, origin.Rows [j]);
                dataRows = data.Select (strWhere);
                if (dataRows.Length == 0) {
                    //DELETE
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.DELETE] = string.Empty;
                    strQuery [(int)DbTSQLInterface.QUERY_TYPE.DELETE] = "DELETE FROM " + nameTable + " WHERE " + getWhereSelect (keyFields, origin.Rows [j]);
                    DbTSQLInterface.ExecNonQuery (ref conn, strQuery [(int)DbTSQLInterface.QUERY_TYPE.DELETE], null, null, out err);
                } else {  //Ничего удалять не надо
                    if (dataRows.Length == 1) {
                    } else {
                    }
                }
            }
        }

        /// <summary>
        /// Признак наличия соединения с БД
        /// </summary>
        /// <param name="obj">Объект соединения с БД, подвергающийся проверке</param>
        /// <returns>Признак наличия соединения с БД</returns>
        public static bool IsConnected (ref DbConnection obj)
        {
            return (!(obj == null)) && (!(obj.State == ConnectionState.Closed)) && (!(obj.State == ConnectionState.Broken));
        }
    }

    /// <summary>
    /// Класс расширение для 'class ConnectionSettings'
    /// </summary>
    public static class ConnectionSettingsExtension
    {
        /// <summary>
        /// Возвратить строку соединения СУБД в ~ от аргумента
        /// </summary>
        /// <param name="connSett">Объект с параметрами соединения</param>
        /// <param name="type">Тип СУБД</param>
        /// <returns>Строка соединения СУБД</returns>
        public static string GetConnectionString (this ConnectionSettings connSett, DbTSQLInterface.DB_TSQL_INTERFACE_TYPE type)
        {
            string strRes = string.Empty;

            switch (type) {
                case DbInterface.DB_TSQL_INTERFACE_TYPE.MSSQL:
                    strRes = connSett.GetConnectionStringMSSQL ();
                    break;
                case DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MySQL:
                    strRes = connSett.GetConnectionStringMySQL ();
                    break;
                case DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.Oracle:
                    strRes = connSett.GetConnectionStringOracle ();
                    break;
                case DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MSExcel:
                    //conn_str = GetConnectionStringExcel ();
                    break;
                default:
                    break;
            }

            return strRes;
        }
    }
}
