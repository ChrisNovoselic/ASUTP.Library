using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
//using System.Windows.Forms;

using MySql.Data.MySqlClient; //Для 'IsConnect'
using System.Data.OracleClient;
using System.Data;

//namespace HClassLibrary
namespace HClassLibrary
{
    public class ConnectionSettings
    {
        public static int UN_ENUMERABLE_ID = -666666;
        public static int ID_LISTENER_LOGGING = UN_ENUMERABLE_ID + 1;

        public volatile int id;
        public volatile string name;

        public volatile string server;
        public volatile string dbName;
        public volatile string userName;
        public volatile string password;
        public volatile int port;
        public volatile bool ignore;

        override public bool Equals(object obj) {
            if ((ConnectionSettings) obj == this)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ConnectionSettings csLeft, ConnectionSettings csRight)
        {
            bool bRes = false;

            if ((object.ReferenceEquals(csRight, null) == false) &&
                object.ReferenceEquals(csLeft, null) == false)
                if ((csLeft.server == csRight.server) &&
                    (csLeft.dbName == csRight.dbName) &&
                    (csLeft.userName == csRight.userName) &&
                    (csLeft.password == csRight.password) &&
                    (csLeft.port == csRight.port))
                    bRes = true;
                else
                    ;
            else
                if (object.ReferenceEquals(csLeft, null) == true)
                    bRes = true;
                else
                    ;

            return bRes;
        }

        public static bool operator !=(ConnectionSettings csLeft, ConnectionSettings csRight)
        {
            bool bRes = false;

            if (!(csLeft == csRight))
                bRes = true;
            else
                ;

            return bRes;
        }

        public enum ConnectionSettingsError
        { 
            NoError,
            WrongIp,
            WrongPort,
            WrongDbName,
            IllegalSymbolDbName,
            IllegalSymbolUserName,
            IllegalSymbolPassword,
            NotConnect
        }

        public ConnectionSettings()
        {
            SetDefault();
        }

        public ConnectionSettings(string nameConn, string srv, int port, string dbName, string uid, string pswd, bool bIgnore = false) : this ()
        {
            id = UN_ENUMERABLE_ID - 1;

            this.name = nameConn;
            this.server = srv;
            this.port = port;
            this.dbName = dbName;
            this.userName = uid;
            this.password = pswd;

            this.ignore = bIgnore;
        }

        /// <summary>
        /// Конструктор для параметров соединения с БД
        /// </summary>
        /// <param name="r">строка таблицы с параметрами соединения</param>
        /// <param name="bLogConnSett">признак предназначения параметров соединения (БД логирования/обычная)</param>
        public ConnectionSettings(DataRow r, int iLogConnSett) : this ()
        {
            if (! (iLogConnSett < 0))
                id = ID_LISTENER_LOGGING + iLogConnSett;
            else
                id = Int32.Parse (r[@"ID"].ToString ());
            name = r[@"NAME_SHR"].ToString ();
            server = r[@"IP"].ToString ();
            port = Int32.Parse(r[@"PORT"].ToString());
            dbName = r[@"DB_NAME"].ToString();
            userName = r[@"UID"].ToString();
            password = r[@"PASSWORD"].ToString();

            int iVal = -1;
            bool bVal = false
                , bRes = int.TryParse(r["IGNORE"].ToString(), out iVal);
            if (bRes == true)
            {
                ignore = iVal == 1; //== "1";
            }
            else
            {
                bRes = bool.TryParse(r["IGNORE"].ToString(), out bVal);
                if (bRes == true)
                {
                    ignore = bVal;
                }
                else
                    ignore = false;
            }
        }

        public void SetDefault()
        {
            id = -1;
            name =
            server = dbName = userName = password = string.Empty;
            port = 1433;
            //ignore = true;
            ignore = false;
        }

        public ConnectionSettingsError Validate()
        {
            try {
                IPAddress ip;
                ip = IPAddress.Parse(server);
                if (IPAddress.TryParse(server, out ip) == false)
                {
                    //MessageBox.Show("Неправильный ip-адрес.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ConnectionSettingsError.WrongIp;
                }
            }
            catch (Exception e) {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"ConnectionSettings::Validate() - ...");
            }

            if (port > 65535)
            {
                //MessageBox.Show("Порт должен лежать в пределах [0:65535].", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ConnectionSettingsError.WrongPort;
            }

            if (dbName == "")
            {
                //MessageBox.Show("Не задано имя базы данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ConnectionSettingsError.WrongDbName;
            }

            if (dbName.IndexOf('\'') >= 0 ||
                dbName.IndexOf('\"') >= 0 ||
                dbName.IndexOf('\\') >= 0 ||
                dbName.IndexOf('/') >= 0 ||
                dbName.IndexOf('?') >= 0 ||
                dbName.IndexOf('<') >= 0 ||
                dbName.IndexOf('>') >= 0 ||
                dbName.IndexOf('*') >= 0 ||
                dbName.IndexOf('|') >= 0 ||
                dbName.IndexOf(':') >= 0 ||
                dbName.IndexOf(';') >= 0)
            {
                //MessageBox.Show("Недопустимый символ в имени базы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ConnectionSettingsError.IllegalSymbolDbName;
            }

            if (userName.IndexOf('\'') >= 0 ||
                userName.IndexOf('\"') >= 0 ||
                userName.IndexOf('\\') >= 0 ||
                userName.IndexOf('/') >= 0 ||
                userName.IndexOf('?') >= 0 ||
                userName.IndexOf('<') >= 0 ||
                userName.IndexOf('>') >= 0 ||
                userName.IndexOf('*') >= 0 ||
                userName.IndexOf('|') >= 0 ||
                userName.IndexOf(':') >= 0 ||
                userName.IndexOf(';') >= 0)
            {
                //MessageBox.Show("Недопустимый символ в имени пользователя.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ConnectionSettingsError.IllegalSymbolUserName;
            }

            if (password.IndexOf('\'') >= 0 ||
                password.IndexOf('\"') >= 0 ||
                password.IndexOf('\\') >= 0 ||
                password.IndexOf('/') >= 0 ||
                password.IndexOf('?') >= 0 ||
                password.IndexOf('<') >= 0 ||
                password.IndexOf('>') >= 0 ||
                password.IndexOf('*') >= 0 ||
                password.IndexOf('|') >= 0 ||
                password.IndexOf(':') >= 0 ||
                password.IndexOf(';') >= 0)
            {
                //MessageBox.Show("Недопустимый символ в пароле пользователя.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ConnectionSettingsError.IllegalSymbolPassword;
            }

            //if (DbTSQLInterface.Select(this, "SELECT * FROM TEC_LIST").Rows.Count > 0)
            //    return ConnectionSettingsError.NotConnect;
            //else
            //    ;

            return ConnectionSettingsError.NoError;
        }

        public string GetConnectionStringMSSQL()
        {
            return @"Data Source=" + server +
                   @"," + port.ToString() +
                   @";Network Library=DBMSSOCN;Initial Catalog=" + dbName +
                   @";User Id=" + userName +
                   @";Password=" + password + @";";
        }

        public string GetConnectionStringMySQL()
        {
            return @"Server=" + server +
                   @";Port=" + port.ToString() +
                   @";Database=" + dbName +
                   @";User Id=" + userName +
                   @";Password=" + password + @";";
        }

        public static string GetConnectionStringExcel(string path)
        {
            string var1 = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                        //@"Persist Security Info=false;" +
                        //@"Extended Properties=Excel 12.0 Xml;HDR=YES;" +
                        @"Extended Properties=Excel 12.0 Xml" + @";" +
                        @"Data Source=" + path + ";",
                    
                    var2 = @"Provider=Microsoft.Jet.OLEDB.4.0;"  +
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

        public static string GetConnectionStringCSV(string path)
        {
            string var1 = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source='" + path + @"';Extended Properties='text;HDR=Yes;FMT=Delimited'",
                        //@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source='" + path + @"';Extended Properties='text;HDR=Yes;FMT=CSVDelimited'",
                    var2 = //@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source= " + path + ";Extended Properties='Excel 12.0;HDR=YES;IMEX=1;'";
                            @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source= " + path + ";Extended Properties='text;HDR=YES;IMEX=1;FMT=Delimited'";

            return var1;
        }

        public static string GetConnectionStringDBF(string path)
        {
            string var1 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" +
                         path +
                        ";Extended Properties=dBase III;";
            ;

            return var1;
        }

        public string GetConnectionStringOracle()
        {
            //return @"Provider=OraOLEDB.Oracle"
            //    + @"; host=" + server + @":" + port
            //    + @"; Data Source="+ dbName
            //    + @"; User Id=" + userName
            //    + @"; Password=" + password
            //    + @"; OLEDB.NET=True;";

            OracleConnectionStringBuilder csb = new OracleConnectionStringBuilder();
            csb.DataSource = dbName;
            csb.UserID = userName;
            csb.Password = password;
            return csb.ConnectionString;
        }
    }
}
