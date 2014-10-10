using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace HClassLibrary
{    
    public class Users : object
    {
        //Идентификаторы из БД
        public enum ID_ROLES { KOM_DISP = 1, ADMIN, USER, NSS = 101, MAJOR_MASHINIST, MASHINIST, SOURCE_DATA = 501,
                            COUNT_ID_ROLES = 7};

        public enum INDEX_REGISTRATION {ID, DOMAIN_NAME, ROLE, ID_TEC, COUNT_INDEX_REGISTRATION};
        public static object[] s_REGISTRATION_INI = new object[(int)Users.INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
        private enum STATE_REGISTRATION {UNKNOWN = -1, CMD, INI, ENV, COUNT_STATE_REGISTRATION};
        private string[] m_NameArgs = { @"iuser", @"udn", @"irole", @"itec" }; //Длина = COUNT_INDEX_REGISTRATION

        private static object [] m_registration;
        private STATE_REGISTRATION[] m_StateRegistration;

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

        private bool m_bRegistration { get { bool bRes = true; for (int i = 0; i < (int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION; i++) if (m_StateRegistration [i] == STATE_REGISTRATION.UNKNOWN) { bRes = false; break; } else ; return bRes; } }

        private DelegateObjectFunc[] f_arRegistration; // = { registrationCmdLine, registrationINI, registrationEnv };

        public Users(int idListener)
        {
            m_registration = new object [(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
            m_StateRegistration = new STATE_REGISTRATION [(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
            int i = -1, j = -1;

            for (i = 0; i < (int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION; i ++)
                m_StateRegistration [i] = STATE_REGISTRATION.UNKNOWN;

            //object[] reg = new object[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];

            f_arRegistration = new DelegateObjectFunc[(int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION];
            f_arRegistration[(int)STATE_REGISTRATION.CMD] = registrationCmdLine;
            f_arRegistration[(int)STATE_REGISTRATION.INI] = registrationINI;
            f_arRegistration[(int)STATE_REGISTRATION.ENV] = registrationEnv;
            for (i = 0; i < (int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION; i++)
                if (i == (int)STATE_REGISTRATION.ENV) f_arRegistration[i](idListener); else f_arRegistration[i](null);

            Initialize ();
        }

        private void registrationCmdLine(object obj)
        {
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
                            m_registration[i] = args[j].Substring(args[j].IndexOf('=') + 1, args[j].Length - (args[j].IndexOf('=') + 1));
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

        private void registrationINI(object obj)
        {
            //Следующий приоритет INI
            if (m_bRegistration == false) {
                bool bValINI = false;
                for (int i = 1; i < (int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION; i ++) {
                    if (m_StateRegistration [i] == STATE_REGISTRATION.UNKNOWN) {
                        bValINI = false;
                        switch (Users.s_REGISTRATION_INI [i].GetType ().Name) {
                            case @"String":
                                bValINI = ((string)Users.s_REGISTRATION_INI [i]).Equals (string.Empty);
                                break;
                            case @"Int32":
                                bValINI = ((int)Users.s_REGISTRATION_INI [i]) < 0;
                                break;
                            default:
                                break;
                        }
                        
                        if (bValINI == false) {
                            m_registration [i] = Users.s_REGISTRATION_INI [i];

                            m_StateRegistration [i] = STATE_REGISTRATION.INI;
                        }
                        else
                            ;
                    }
                    else
                        ;
                }
            }
            else
            {
            }
        }

        private void registrationEnv(object obj)
        {
            //Следующий приоритет DataBase
            if (m_bRegistration == false) {
                if (m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] == STATE_REGISTRATION.UNKNOWN) {
                    //Определить из ENV
                    //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                    try { m_registration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = Environment.UserDomainName + @"\" + Environment.UserName; }
                    catch (Exception e) {
                        throw e;
                    }
                    m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = STATE_REGISTRATION.ENV;
                }
                else {
                }

                int err = -1;
                DataTable dataUsers;

                DbConnection connDB = DbSources.Sources().GetConnection((int)obj, out err);

                //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                GetUsers(ref connDB, @"DOMAIN_NAME=" + @"'" + m_registration[(int)INDEX_REGISTRATION.DOMAIN_NAME] + @"'", string.Empty, out dataUsers, out err);

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
                        if (dataUsers.Rows[i][@"DOMAIN_NAME"].ToString().Equals((string)m_registration[(int)INDEX_REGISTRATION.DOMAIN_NAME], StringComparison.CurrentCultureIgnoreCase) == true) break; else ;
                    }

                    if (i < dataUsers.Rows.Count)
                    {
                        m_registration[(int)INDEX_REGISTRATION.ID] = Convert.ToInt32(dataUsers.Rows[i]["ID"]); m_StateRegistration[(int)INDEX_REGISTRATION.ID] = STATE_REGISTRATION.ENV;
                        m_registration[(int)INDEX_REGISTRATION.ROLE] = Convert.ToInt32(dataUsers.Rows[i]["ID_ROLE"]); m_StateRegistration[(int)INDEX_REGISTRATION.ROLE] = STATE_REGISTRATION.ENV;
                        m_registration[(int)INDEX_REGISTRATION.ID_TEC] = Convert.ToInt32(dataUsers.Rows[i]["ID_TEC"]); m_StateRegistration[(int)INDEX_REGISTRATION.ID_TEC] = STATE_REGISTRATION.ENV;
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
            }
            else {
            }
        }

        private void Initialize () {
            string strMes = string.Empty;

            strMes = @"Пользователь: " + DomainName + @", (id=" + Id + @"), роль: " + Role + @", id_tec=" + allTEC;

            System.Net.IPAddress[] listIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            int indxIP = -1;
            for (indxIP = 0; indxIP < listIP.Length; indxIP ++) {
                strMes += @", ip[" + indxIP + @"]=" + listIP[indxIP].ToString ();
            }

            Logging.Logg().Action(strMes);
        }

        public static int Role {
            get {
                return (m_registration == null) ? 0 : ((! ((int)INDEX_REGISTRATION.ID_TEC < m_registration.Length)) || (m_registration[(int)INDEX_REGISTRATION.ROLE] == null)) ? (int)Users.ID_ROLES.ADMIN : (int)m_registration[(int)INDEX_REGISTRATION.ROLE];
            }
        }

        public static bool RoleIsDisp {
            get { return ((Users.Role == (int)Users.ID_ROLES.ADMIN) || (Users.Role == (int)Users.ID_ROLES.KOM_DISP) || (Users.Role == (int)Users.ID_ROLES.NSS)); }
        }

        public static int allTEC
        {
            get
            {
                return (m_registration == null) ? 0 : ((! ((int)INDEX_REGISTRATION.ID_TEC < m_registration.Length)) || (m_registration[(int)INDEX_REGISTRATION.ID_TEC] == null)) ? 0 : (int)m_registration[(int)INDEX_REGISTRATION.ID_TEC];
            }
        }

        public static int Id
        {
            get
            {
                return (m_registration == null) ? 0 : ((! ((int)INDEX_REGISTRATION.ID_TEC < m_registration.Length)) || (m_registration[(int)INDEX_REGISTRATION.ID] == null)) ? 0 : (int)m_registration[(int)INDEX_REGISTRATION.ID];
            }
        }

        public static string DomainName
        {
            get
            {
                return (string)m_registration [(int)INDEX_REGISTRATION.DOMAIN_NAME];
            }
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
            users = new DataTable();

            users = DbTSQLInterface.Select(ref conn, getUsersRequest(where, orderby), null, null, out err);
        }

        public static void GetRoles(ref DbConnection conn, string where, string orderby, out DataTable roles, out int err)
        {
            err = 0;
            roles = new DataTable();

            roles = DbTSQLInterface.Select(ref conn, @"SELECT * FROM ROLES WHERE ID < 500", null, null, out err);
        }
    }
}
