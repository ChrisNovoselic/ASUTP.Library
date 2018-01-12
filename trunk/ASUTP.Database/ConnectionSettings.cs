using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ASUTP.Database {
    /// <summary>
    /// Класс для описания параметров соединения с источником данных (БД)
    /// </summary>
    public class ConnectionSettings : IEquatable<ConnectionSettings> {
        public static int UN_ENUMERABLE_ID = -666666;
        public static int ID_LISTENER_LOGGING = UN_ENUMERABLE_ID + 1;
        //Параметры (не обязательные) соединения
        /// <summary>
        /// Идентификатор источника данных
        /// </summary>
        public volatile int id;
        /// <summary>
        /// Наименование источника данных
        /// </summary>
        public volatile string name;
        //Параметры (обязательные) соединения
        /// <summary>
        /// Сервер-источник данных (может быть указан как доменное имя, IP-адрес)
        /// </summary>
        public volatile string server;

        public volatile string instance;
        /// <summary>
        /// Нименование БД
        /// </summary>
        public volatile string dbName;
        /// <summary>
        /// Имя пользователя при подключении к источнику данных
        /// </summary>
        public volatile string userName;
        /// <summary>
        /// Пароль пользователя при подключении к источнику данных
        /// </summary>
        public volatile string password;
        /// <summary>
        /// Номер порта при подключении к источнику данных
        /// </summary>
        public volatile int port;
        ///// <summary>
        ///// Признак игнорирования (не использования) источника данных
        ///// </summary>
        //public volatile bool ignore;

        public bool Equals (ConnectionSettings other)
        {
            return Equals (other as object);
        }

        override public bool Equals (object obj)
        {
            if ((ConnectionSettings)obj == this)
                return true;
            else
                return false;
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        /// <summary>
        /// Оператор сравнения 2-х объектов с параметрами соединения
        /// </summary>
        /// <param name="csLeft">Объект для сравнения(слева)</param>
        /// <param name="csRight">Объект для сравнения(справа)</param>
        /// <returns>Признак результата сравнения</returns>
        public static bool operator == (ConnectionSettings csLeft, ConnectionSettings csRight)
        {
            bool bRes = false;

            if ((object.ReferenceEquals (csRight, null) == false) &&
                object.ReferenceEquals (csLeft, null) == false)
                if ((csLeft.server == csRight.server) &&
                    (csLeft.instance == csRight.instance) &&
                    (csLeft.dbName == csRight.dbName) &&
                    (csLeft.userName == csRight.userName) &&
                    (csLeft.password == csRight.password) &&
                    (csLeft.port == csRight.port))
                    bRes = true;
                else
                    ;
            else
                if (object.ReferenceEquals (csLeft, null) == true)
                bRes = true;
            else
                ;

            return bRes;
        }

        /// <summary>
        /// Оператор сравнения 2-х объектов с параметрами соединения
        /// </summary>
        /// <param name="csLeft">Объект для сравнения(слева)</param>
        /// <param name="csRight">Объект для сравнения(справа)</param>
        /// <returns>Признак результата сравнения</returns>
        public static bool operator != (ConnectionSettings csLeft, ConnectionSettings csRight)
        {
            bool bRes = false;

            if (!(csLeft == csRight))
                bRes = true;
            else
                ;

            return bRes;
        }

        /// <summary>
        /// Перечисление - возможные ошибки при попытке установки значений для параметров соединения
        /// </summary>
        [Flags]
        public enum ConnectionSettingsError {
            NoError = 0x0,
            WrongIp = 0x1,
            WrongPort = 0x2,
            WrongDbName = 0x4,
            IllegalSymbolDbName = 0x8,
            IllegalSymbolUserName = 0x10,
            IllegalSymbolPassword = 0x20,
            NotConnect = 0x40
        }

        /// <summary>
        /// Возвраить наименование сервера (IP-адрес)
        /// </summary>
        /// <param name="server">Полная строка наименования сервера</param>
        /// <returns>Наименование сервера</returns>
        public static string IP (string server)
        {
            return server.Split ('\\') [0];
        }

        /// <summary>
        /// Возвратить наименование экземпляра сервера
        /// </summary>
        /// <param name="server">Полная строка наименования сервера</param>
        /// <returns>Наименование экземпляра сервера</returns>
        public static string Instance (string server)
        {
            string strRes = string.Empty;

            if (server.Split ('\\').Length > 1)
                strRes = server.Split ('\\') [1];
            else
                ;

            return strRes;
        }

        /// <summary>
        /// Возвратить полную строку наименования сервера - источника информации (хост + наимнование экземпляра)
        /// </summary>
        /// <param name="ip">IP-адрес</param>
        /// <param name="instance">Наимнование экземпляра</param>
        /// <returns>Полная строка наименования сервера</returns>
        public static string IpInstance (string ip, string instance)
        {
            string strRes = ip;

            if (instance.Equals (string.Empty) == false)
                strRes += @"\" + instance;
            else
                ;

            return strRes;
        }

        /// <summary>
        /// Конструктор - основной (без аргументов)
        /// </summary>
        public ConnectionSettings ()
        {
            SetDefault ();
        }

        /// <summary>
        /// Конструктор - дополнительный (с аргументами)
        /// </summary>
        public ConnectionSettings (ConnectionSettings connSett)
        {
            if (connSett == null)
                SetDefault ();
            else {
                id = connSett.id;

                this.name = connSett.name;
                this.server = connSett.server;
                this.instance = connSett.instance;
                this.port = connSett.port;
                this.dbName = connSett.dbName;
                this.userName = connSett.userName;
                this.password = connSett.password;

                //this.ignore = connSett.ignore;
            }
        }

        /// <summary>
        /// Конструктор - основной (с аргументами - значениями параметров соединения)
        /// </summary>
        /// <param name="nameConn">Наименование соединения</param>
        /// <param name="srv">IP-адрес или доменное наименование сервера</param>
        /// <param name="instance">Наименование экземпляра сервера</param>
        /// <param name="port">Номер порта обмена данными с сервером</param>
        /// <param name="dbName">Наименование БД</param>
        /// <param name="uid">Имя входа/подключения пользователя</param>
        /// <param name="pswd">Пароль для имени входа/подключения</param>
        public ConnectionSettings (string nameConn
            , string srv
            , string instance
            , int port
            , string dbName
            , string uid
            , string pswd
            )
                : this ()
        {
            id = UN_ENUMERABLE_ID - 1;

            this.name = nameConn;
            this.server = srv;
            this.instance = instance;
            this.port = port;
            this.dbName = dbName;
            this.userName = uid;
            this.password = pswd;
        }

        /// <summary>
        /// Конструктор - основной (с аргументами - значениями параметров соединения)
        /// </summary>
        /// <param name="id">Идентификатор источника данных</param>
        /// <param name="nameConn">Наименование соединения</param>
        /// <param name="srv">IP-адрес или доменное наименование сервера</param>
        /// <param name="instance">Наименование экземпляра сервера</param>
        /// <param name="port">Номер порта обмена данными с сервером</param>
        /// <param name="dbName">Наименование БД</param>
        /// <param name="uid">Имя входа/подключения пользователя</param>
        /// <param name="pswd">Пароль для имени входа/подключения</param>
        public ConnectionSettings (int id
            , string nameConn
            , string srv
            , string instance
            , int port
            , string dbName
            , string uid
            , string pswd
            //, bool bIgnore = false
            )
                : this (nameConn, srv, instance, port, dbName, uid, pswd/*, bIgnore*/)
        {
            this.id = id;
        }

        /// <summary>
        /// Конструктор для параметров соединения с БД
        /// </summary>
        /// <param name="r">Строка таблицы с параметрами соединения</param>
        /// <param name="iLogConnSett">Признак предназначения параметров соединения (БД логирования/обычная)</param>
        public ConnectionSettings (DataRow r, int iLogConnSett) : this ()
        {
            if (!(iLogConnSett < 0))
                id = ID_LISTENER_LOGGING + iLogConnSett;
            else
                id = Int32.Parse (r [@"ID"].ToString ());
            name = r [@"NAME_SHR"].ToString ();
            server = r [@"IP"].ToString ();
            instance = (!(r [@"INSTANCE"] is DBNull)) ? r [@"INSTANCE"].ToString ().Trim () : string.Empty;
            port = Int32.Parse (r [@"PORT"].ToString ());
            dbName = r [@"DB_NAME"].ToString ();
            userName = r [@"UID"].ToString ();
            password = r [@"PASSWORD"].ToString ();
        }

        /// <summary>
        /// Установить значения по умолчанию
        /// </summary>
        public void SetDefault ()
        {
            id = -1;
            name =
            server =
            instance =
            dbName =
            userName =
            password =
                string.Empty;
            port = 1433;
        }

        /// <summary>
        /// Проверить корректность значений параметров соединения
        /// </summary>
        /// <returns>Набор признаков(флагов) ошибок</returns>
        public ConnectionSettingsError Validate ()
        {
            ConnectionSettingsError errRes = ConnectionSettingsError.NoError;

            Regex reg = new Regex ("[0-9a-zA-Z_-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            try {
                IPAddress ip = IPAddress.Parse (server);
                if (IPAddress.TryParse (server, out ip) == false) {
                    //MessageBox.Show("Неправильный ip-адрес.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    errRes |= ConnectionSettingsError.WrongIp;
                }
            } catch (Exception e) {
                Logging.Logg ().Exception (e, @"ConnectionSettings::Validate() - ...", Logging.INDEX_MESSAGE.NOT_SET);
            }

            //??? проверка INSTANCE (не должен начинаться на цифры, не содержит спец./символы)

            if ((port < 1)
                && (port > 65535)) {
                //MessageBox.Show("Порт должен лежать в пределах [0:65535].", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errRes |= ConnectionSettingsError.WrongPort;
            }

            if (string.IsNullOrEmpty(dbName) == true) {
                //MessageBox.Show("Не задано имя базы данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errRes |= ConnectionSettingsError.WrongDbName;
            }

            // IndexOf: '\'', '\"', '\\', '/', '?', '<', '>', '*', '|', ':', ';'
            if (reg.IsMatch(dbName) == false) {
                //MessageBox.Show("Недопустимый символ в имени базы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errRes |= ConnectionSettingsError.IllegalSymbolDbName;
            }

            // IndexOf: '\'', '\"', '\\', '/', '?', '<', '>', '*', '|', ':', ';'
            if (reg.IsMatch(userName) == false) {
                //MessageBox.Show("Недопустимый символ в имени пользователя.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errRes |= ConnectionSettingsError.IllegalSymbolUserName;
            }

            // IndexOf: '\'', '\"', '\\', '/', '?', '<', '>', '*', '|', ':', ';'
            if (reg.IsMatch (password) == false) {
                //MessageBox.Show("Недопустимый символ в пароле пользователя.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errRes |= ConnectionSettingsError.IllegalSymbolPassword;
            }

            return errRes;
        }

        /// <summary>
        /// Возвратить строку соединения СУБД MSSQLServer
        /// </summary>
        /// <returns>Строка соединения СУБД MSSQLServer</returns>
        public string GetConnectionStringMSSQL ()
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder ();

            csb.DataSource = $"{IpInstance (server, instance)},{port}";
            csb.InitialCatalog = dbName;
            csb.NetworkLibrary = "DBMSSOCN";
            csb.UserID = userName;
            csb.Password = password;

            csb.Pooling = true;

            return
                //@"Data Source=" + IpInstance (server, instance) +
                //@"," + port.ToString () +
                //@";Network Library=DBMSSOCN;Initial Catalog=" + dbName +
                //@";User Id=" + userName +
                //@";Password=" + password + @";"
                csb.ConnectionString
                ;
        }

        /// <summary>
        /// Возвратить строку соединения СУБД MySql
        /// </summary>
        /// <returns>Строка соединения СУБД MySql</returns>
        public string GetConnectionStringMySQL ()
        {
            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();

            csb.Server = server;
            csb.Port = (uint)port;
            csb.Database = dbName;
            csb.UserID = userName;
            csb.Password = password;

            return
                //@"Server=" + server +
                //@";Port=" + port.ToString () +
                //@";Database=" + dbName +
                //@";User Id=" + userName +
                //@";Password=" + password + @";"
                csb.ConnectionString
                ;
        }

        /// <summary>
        /// Возвратить строку соединения СУБД MS Excel
        /// </summary>
        /// <returns>Строка соединения СУБД MS Excel</returns>
        public static string GetConnectionStringExcel (string path)
        {
            string var1 = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                        //@"Persist Security Info=false;" +
                        //@"Extended Properties=Excel 12.0 Xml;HDR=YES;" +
                        @"Extended Properties=Excel 12.0 Xml" + @";" +
                        @"Data Source=" + path + ";"

                , var2 = @"Provider=Microsoft.Jet.OLEDB.4.0;" +
                    //@"Extended Properties=Excel 8.0;HDR=YES;Mode=Read;ReadOnly=true;" +
                    //@"Extended Properties=Excel 8.0;HDR=YES;IMEX=1;Mode=Read;ReadOnly=true;" +
                    //@"Extended Properties=Excel 8.0;HDR=YES;IMEX=1;" +
                    //@"Extended Properties=Excel 8.0;HDR=YES;" +
                    @"Extended Properties=""Excel 8.0;ReadOnly=false;"";" +
                    //@"Persist Security Info=false;" +
                    @"Data Source=" + path + @";";
                    //@"Data Source=" + path + ";" + @"Jet OLEDB:Database Password=" + @"nss;";

            return var1;
        }

        /// <summary>
        /// Возвратить строку соединения ядра OLE.DB для обработки CSV-файла
        /// </summary>
        /// <returns>Строка соединения ядра OLE.DB для обработки CSV-файла</returns>
        public static string GetConnectionStringCSV (string path)
        {
            string var1 = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source='{path}';Extended Properties='text;HDR=Yes;FMT=Delimited'"
                //$"Provider=Microsoft.Jet.OLEDB.4.0;Data Source='{path}';Extended Properties='text;HDR=Yes;FMT=CSVDelimited'",
                , var2 = //$"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Extended Properties='Excel 12.0;HDR=YES;IMEX=1;'";
                        $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Extended Properties='text;HDR=YES;IMEX=1;FMT=Delimited'";

            return var1;
        }

        /// <summary>
        /// Возвратить строку соединения ядра OLE.DB для обработки DBF-файла
        /// </summary>
        /// <returns>Строка соединения ядра OLE.DB для обработки DBF-файла</returns>
        public static string GetConnectionStringDBF (string path)
        {
            string var1 =
                $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={path};Extended Properties=dBase III;"
                ;

            return var1;
        }

        /// <summary>
        /// Возвратить строку соединения СУБД Oracle
        /// </summary>
        /// <returns>Строка соединения СУБД Oracle</returns>
        public string GetConnectionStringOracle ()
        {
            //return
                //@"Provider=OraOLEDB.Oracle"
                //+ @"; host=" + server + @":" + port
                //+ @"; Data Source=" + dbName
                //+ @"; User Id=" + userName
                //+ @"; Password=" + password
                //+ @"; OLEDB.NET=True;"
                //??? не работает (нет 'server', 'port')
                ;

            OracleConnectionStringBuilder csb = new OracleConnectionStringBuilder ();
            csb.DataSource = dbName;
            csb.UserID = userName;
            csb.Password = password;
            return csb.ConnectionString;
        }

        /// <summary>
        /// Признак наличия значений основных параметров для установления соединения
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty (server) == true
                    || string.IsNullOrEmpty (userName) == true;
            }
        }
    }
}
