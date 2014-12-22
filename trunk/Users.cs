using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
//using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Windows.Forms; //Application.ProductVersion

namespace HClassLibrary
{    
    public class HUsers : object
    {
        private class DbTable_dictionary
        {
            enum INDEX_QUERY { SELECT, INSERT, UPDATE, DELETE }

            ConnectionSettings m_connSett;
            DbConnection m_dbConn;
            string[] m_query;

            public DbTable_dictionary(ConnectionSettings connSett, string nameTable, string where)
            {
            }

            public void ReadString(string[] opt, string[] valDef)
            {
            }

            public void WriteString(string[] opt, string[] val)
            {
            }
        }

        private class HProfiles
        {
            static DataTable m_tblValues;

            public HProfiles(int iListenerId, int id_role, int id_user)
            {
                int err = -1;
                string query = @"SELECT * FROM profiles WHERE (ID_EXT=" + id_role + @" AND IS_ROLE=1)" + @" OR (ID_EXT=" + id_user + @" AND IS_ROLE=0)"
                    , errMsg = string.Empty;

                DbConnection dbConn = DbSources.Sources().GetConnection(iListenerId, out err);

                if (!(err == 0))
                    errMsg = @"нет соединения с БД";
                else
                {
                    m_tblValues = DbTSQLInterface.Select(ref dbConn, query, null, null, out err);

                    if (!(err == 0))
                        errMsg = @"Ошибка при чтении настроек для группы(роли) (irole = " + id_role + @"), пользователя (iuser=" + id_user + @")";
                    else
                        ;
                }

                if (!(err == 0))
                    throw new Exception(@"HProfiles::HProfiles () - " + errMsg + @"...");
                else
                    ;
            }

            public static bool IsAllowed(int id)
            {
                bool bRes = false;
                Int16 val = -1;
                
                bRes = Int16.TryParse (m_tblValues.Select (@"ID_UNIT=" + id) [0][@"VALUE"].ToString (), out val);

                if (bRes == true)
                    bRes = val == 1;
                else
                    ;

                return bRes;
            }
        }

        static HProfiles m_profiles;

        //Идентификаторы из БД
        //public enum ID_ROLES { ...

        //Данные о пользователе
        public enum INDEX_REGISTRATION { ID, DOMAIN_NAME, ROLE, ID_TEC, COUNT_INDEX_REGISTRATION };
        public static object [] s_REGISTRATION_INI = new object [(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION]; //Предустановленные в файле/БД конфигурации

        protected enum STATE_REGISTRATION { UNKNOWN = -1, CMD, INI, ENV, COUNT_STATE_REGISTRATION };
        protected string[] m_NameArgs; //Длина = COUNT_INDEX_REGISTRATION

        protected static object [] m_DataRegistration;
        protected STATE_REGISTRATION[] m_StateRegistration;

        //private bool compareIpVal (int [] ip_trust, int [] ip)
        //{
        //    bool bRes = true;
        //    int j = -1;

        //    for (j = 0; j < ip_trust.Length; j ++) {
        //        if (ip_trust [j] == ip [j])
        //            continue;
        //        else {
        //            bRes = false;
        //            break;
        //        }

        //    }

        //    return bRes;
        //}

        //private int [] strIpToVal (string [] str) {
        //    int j = -1;
        //    int [] val = new int[str.Length];
            
        //    for (j = 0; j < str.Length; j++)
        //    {
        //        Logging.Logg().Debug(@"val[" + j.ToString () + "] = " + val [j]);

        //        val[j] = Convert.ToInt32(str[j]);
        //    }

        //    return val;
        //}

        private bool m_bRegistration {
            get {
                bool bRes = true;

                if (! (m_StateRegistration == null))
                    for (int i = 0; i < m_StateRegistration.Length; i++)
                        if (m_StateRegistration[i] == STATE_REGISTRATION.UNKNOWN) {
                            bRes = false;
                            break;
                        }
                        else ;
                else
                    bRes = false;

                return bRes;
            }
        }

        protected DelegateObjectFunc[] f_arRegistration; // = { registrationCmdLine, registrationINI, registrationEnv };

        public HUsers(int iListenerId)
        {
            Logging.Logg().Action(@"HUsers::HUsers () - ... кол-во аргументов ком./строки = " + (Environment.GetCommandLineArgs().Length - 1) +
                @"; DomainUserName = " + Environment.UserDomainName + @"\" + Environment.UserName +
                @"; MashineName=" + Environment.MachineName);

            try {
                //Обрабатываемые слова 'командной строки'
                m_NameArgs = new string[] { @"iuser", @"udn", @"irole", @"itec" }; //Длина = COUNT_INDEX_REGISTRATION

                f_arRegistration = new DelegateObjectFunc[(int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION];
                f_arRegistration[(int)STATE_REGISTRATION.CMD] = registrationCmdLine;
                f_arRegistration[(int)STATE_REGISTRATION.INI] = registrationINI;
                f_arRegistration[(int)STATE_REGISTRATION.ENV] = registrationEnv;

                m_DataRegistration = new object[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
                m_StateRegistration = new STATE_REGISTRATION[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];

                ClearValues();
            } catch (Exception e) {
                Logging.Logg().Exception(e, @"HUsers::HUsers ()...");
            }

            Logging.Logg().Debug(@"HUsers::HUsers () - ... очистили значения ...");

            for (int i = 0; i < (int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION; i++)
                if (i == (int)STATE_REGISTRATION.ENV) f_arRegistration[i](iListenerId); else f_arRegistration[i](null);
        }

        protected void ClearValues () {
            int i = -1;
            for (i = 0; i < m_DataRegistration.Length; i++)
                m_DataRegistration[i] = null;

            for (i = 0; i < m_StateRegistration.Length; i++)
                m_StateRegistration[i] = STATE_REGISTRATION.UNKNOWN;
        }

        private void registrationCmdLine(object par)
        {
            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationCmdLine () - вХод ...");
            
            //Приоритет CMD_LINE
            string[] args = Environment.GetCommandLineArgs();

            //Есть ли параметры в CMD_LINE
            if (args.Length > 1)
            {
                if ((args.Length == 2) && ((args[1].Equals(@"/?") == true) || (args[1].Equals(@"?") == true) || (args[1].Equals(@"/help") == true) || (args[1].Equals(@"help") == true)))
                { //Выдать сообщение-подсказку...
                }
                else
                    ;

                for (int i = 1; i < m_NameArgs.Length; i++)
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        if (!(args[j].IndexOf(m_NameArgs[i]) < 0))
                        {
                            //Параметр найден
                            m_DataRegistration[i] = args[j].Substring(args[j].IndexOf('=') + 1, args[j].Length - (args[j].IndexOf('=') + 1));
                            m_StateRegistration[i] = STATE_REGISTRATION.CMD;

                            break;
                        }
                        else
                        {
                        }
                    }
                }
            }
            else
            {
            }
        }

        private void registrationINI(object par)
        {
            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - вХод ...");

            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - размер массива параметров INI = " + s_REGISTRATION_INI.Length);

            //Следующий приоритет INI
            if (m_bRegistration == false) {
                bool bValINI = false;
                for (int i = 1; i < m_DataRegistration.Length; i++)
                {
                    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - обработка параметра [" + i + @"]");

                    try
                    {
                        if (m_StateRegistration[i] == STATE_REGISTRATION.UNKNOWN)
                        {
                            bValINI = false;
                            //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - состояние параметра = " + m_StateRegistration[i].ToString());

                            //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - объект параметра = " + s_REGISTRATION_INI[i].ToString());

                            //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - тип параметра = " + s_REGISTRATION_INI[i].GetType().Name);

                            switch (s_REGISTRATION_INI[i].GetType().Name)
                            {
                                case @"String":
                                    bValINI = ((string)s_REGISTRATION_INI[i]).Equals(string.Empty);
                                    break;
                                case @"Int32":
                                    bValINI = ((Int32)s_REGISTRATION_INI[i]) < 0;
                                    break;
                                default:
                                    break;
                            }

                            if (bValINI == false)
                            {
                                m_DataRegistration[i] = s_REGISTRATION_INI[i];

                                m_StateRegistration[i] = STATE_REGISTRATION.INI;
                            }
                            else
                                ;
                        }
                        else
                            ;
                    } 
                    catch (Exception e)
                    {
                        Logging.Logg().Exception(e, @"HUsers::HUsers () - ... registrationINI () - параметр не обработан [" + i + @"]");
                    }
                }
            }
            else
            {
            }
        }

        private void registrationEnv(object par)
        {
            int idListener = (int)par //idListener = ((int [])par)[0]
                //, indxDomainName = ((int[])par)[1]
                ;

            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - вХод ... idListener = " + idListener + @"; m_bRegistration = " + m_bRegistration.ToString() + @"; m_StateRegistration = " + m_StateRegistration);

            //Следующий приоритет DataBase
            if (m_bRegistration == false) {
                Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = " + m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME].ToString ());

                try {
                    if (m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] == STATE_REGISTRATION.UNKNOWN) {
                        Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = " + Environment.UserDomainName + @"\" + Environment.UserName);
                        //Определить из ENV
                        //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                        m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = Environment.UserDomainName + @"\" + Environment.UserName;
                        m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = STATE_REGISTRATION.ENV;
                    }
                    else {
                    }
                } catch (Exception e) {
                    Logging.Logg().Exception(e, @"HUsers::HUsers () - ... registrationEnv () ... Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ ... ");
                    throw e;                    
                }

                int err = -1;
                DataTable dataUsers;

                DbConnection connDB = DbSources.Sources().GetConnection((int)par, out err);

                if ((! (connDB == null)) && (err == 0))
                {
                    //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                    GetUsers(ref connDB, @"DOMAIN_NAME=" + @"'" + m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] + @"'", string.Empty, out dataUsers, out err);

                    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - найдено пользователей = " + dataUsers.Rows.Count);

                    if ((err == 0) && (dataUsers.Rows.Count > 0))
                    {//Найдена хотя бы одна строка
                        int i = -1;
                        for (i = 0; i < dataUsers.Rows.Count; i ++)
                        {
                            //Проверка IP-адрес                    
                            //for (indxIP = 0; indxIP < listIP.Length; indxIP ++) {
                            //    if (listIP[indxIP].Equals(System.Net.IPAddress.Parse (dataUsers.Rows[i][@"IP"].ToString())) == true) {
                            //        //IP найден
                            //        break;
                            //    }
                            //    else
                            //        ;
                            //}

                            //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                            if (dataUsers.Rows[i][@"DOMAIN_NAME"].ToString().Equals((string)m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME], StringComparison.CurrentCultureIgnoreCase) == true) break; else ;
                        }

                        if (i < dataUsers.Rows.Count)
                        {
                            m_DataRegistration[(int)INDEX_REGISTRATION.ID] = Convert.ToInt32(dataUsers.Rows[i]["ID"]); m_StateRegistration[(int)INDEX_REGISTRATION.ID] = STATE_REGISTRATION.ENV;
                            m_DataRegistration[(int)INDEX_REGISTRATION.ROLE] = Convert.ToInt32(dataUsers.Rows[i]["ID_ROLE"]); m_StateRegistration[(int)INDEX_REGISTRATION.ROLE] = STATE_REGISTRATION.ENV;
                            m_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC] = Convert.ToInt32(dataUsers.Rows[i]["ID_TEC"]); m_StateRegistration[(int)INDEX_REGISTRATION.ID_TEC] = STATE_REGISTRATION.ENV;
                        }
                        else
                            throw new Exception("Пользователь не найден в списке БД конфигурации");
                    }
                    else
                    {//Не найдено ни одной строки
                        if (connDB == null)
                            throw new HException(-4, "Нет соединения с БД конфигурации");
                        else
                            if (err == 0)
                                throw new HException(-3, "Пользователь не найден в списке БД конфигурации");
                            else
                                throw new HException(-2, "Ошибка получения списка пользователей из БД конфигурации");
                    }
                } else {//Нет возможности для проверки
                    if (connDB == null)
                        throw new HException(-4, "Нет соединения с БД конфигурации");
                    else
                        if (! (err == 0))
                            throw new HException(-3, "Не удалось установить связь с БД конфигурации");
                        else
                            ;
                }
            }
            else {
                Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - m_bRegistration = " + m_bRegistration.ToString());
            }

            try {
                m_profiles = new HProfiles (idListener, (int)m_DataRegistration[(int)INDEX_REGISTRATION.ROLE], (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID]);
            } catch (Exception e) {
                throw new HException(-6, e.Message);
            }
        }

        //protected abstract void Registration (DataRow rowUser)  { }

        protected void Initialize (string baseMsg) {
            string strMes = baseMsg;

            System.Net.IPAddress[] listIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            int indxIP = -1;
            for (indxIP = 0; indxIP < listIP.Length; indxIP ++) {
                strMes += @", ip[" + indxIP + @"]=" + listIP[indxIP].ToString ();
            }

            strMes += @"; Version=" + Application.ProductVersion.ToString ()/*StatisticCommon.Properties.Resources.TradeMarkVersion*/;

            Logging.Logg().Action(strMes);
        }

        private static string getUsersRequest(string where, string orderby)
        {
            string strQuery = string.Empty;
            //strQuer//strQuery =  "SELECT * FROM users WHERE DOMAIN_NAME='" + Environment.UserDomainName + "\\" + Environment.UserName + "'";
            //strQuery =  "SELECT * FROM users WHERE DOMAIN_NAME='NE\\ChrjapinAN'";
            strQuery = "SELECT * FROM users";
            if ((!(where == null)) && (where.Length > 0))
                strQuery += " WHERE " + where;
            else
                ;

            if ((!(orderby == null)) && (orderby.Length > 0))
                strQuery += " ORDER BY " + orderby;
            else
                ;

            return strQuery;
        }

        /*public void GetUsers(string where, string orderby, out DataTable users, out int err)
        {
            err = 0;            
            users = new DataTable ();

            users = DbTSQLInterface.Select(connSettConfigDB, getUsersRequest(where, orderby), out err);
        }

        public static void GetUsers(ConnectionSettings connSett, string where, string orderby, out DataTable users, out int err)
        {
            err = 0;
            users = new DataTable();

            users = DbTSQLInterface.Select(connSett, getUsersRequest(where, orderby), out err);
        }*/

        public static void GetUsers(ref DbConnection conn, string where, string orderby, out DataTable users, out int err)
        {
            err = 0;
            users = null;
            
            if (! (conn == null))
            {
                users = new DataTable();
                Logging.Logg().Debug(@"HUsers::GetUsers () - запрос для поиска пользователей = [" + getUsersRequest(where, orderby) + @"]");
                users = DbTSQLInterface.Select(ref conn, getUsersRequest(where, orderby), null, null, out err);
            } else {
                err = -1;
            }
        }

        public static void GetRoles(ref DbConnection conn, string where, string orderby, out DataTable roles, out int err)
        {
            err = 0;
            roles = null;
            string query = string.Empty;

            if (! (conn == null))
            {
                roles = new DataTable();
                query = @"SELECT * FROM ROLES";

                if ((where.Equals(null) == true) || (where.Equals(string.Empty) == true))
                    query += @" WHERE ID < 500";
                else
                    query += @" WHERE " + where;

                roles = DbTSQLInterface.Select(ref conn, query, null, null, out err);
            }
            else
            {
                err = -1;
            }
        }

        public static int Id
        {
            get
            {
                return (m_DataRegistration == null) ? 0 : ((!((int)INDEX_REGISTRATION.ID_TEC < m_DataRegistration.Length)) || (m_DataRegistration[(int)INDEX_REGISTRATION.ID] == null)) ? 0 : (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID];
            }
        }

        public static string DomainName
        {
            get
            {
                return (string)m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME];
            }
        }

        public static int allTEC
        {
            get
            {
                return (m_DataRegistration == null) ? 0 : ((!((int)INDEX_REGISTRATION.ID_TEC < m_DataRegistration.Length)) || (m_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC] == null)) ? 0 : (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC];
            }
        }

        public static bool IsAllowed (int id) { return HProfiles.IsAllowed (id); }
    }
}
