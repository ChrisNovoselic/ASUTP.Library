using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms; //Application.ProductVersion
//using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO; //File

namespace HClassLibrary
{    
    public class HUsers : object, IDisposable
    {
        private class DbTable_dictionary
        {
            enum INDEX_QUERY { SELECT, INSERT, UPDATE, DELETE }

            //ConnectionSettings m_connSett;
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

        public void Dispose ()
        {
        }

        protected virtual HProfiles createProfiles(int iListenerId, int id_role, int id_user)
        {
            return new HProfiles(iListenerId, id_role, id_user);
        }

        protected class HProfiles
        {
            public static string m_nameTableProfilesData = @"profiles"
                , m_nameTableProfilesUnit = @"profiles_unit";
            
            protected static DataTable m_tblValues;
            protected static DataTable m_tblTypes;

            public static DataTable GetTableUnits { get { return m_tblTypes; } }

            /// <summary>
            /// ������� ����������� ������������
            /// </summary>
            public HProfiles(int iListenerId, int id_role, int id_user)
            {
                Update (iListenerId, id_role, id_user, true);
            }

            public void  Update (int iListenerId, int id_role, int id_user, bool bThrow)
            {
                int err = -1;
                string query = string.Empty
                    , errMsg = string.Empty;

                DbConnection dbConn = DbSources.Sources().GetConnection(iListenerId, out err);

                if (!(err == 0))
                    errMsg = @"��� ���������� � ��";
                else
                {
                    query = @"SELECT * FROM " + m_nameTableProfilesData + @" WHERE (ID_EXT=" + id_role + @" AND IS_ROLE=1)" + @" OR (ID_EXT=" + id_user + @" AND IS_ROLE=0)";
                    m_tblValues = DbTSQLInterface.Select(ref dbConn, query, null, null, out err);

                    if (!(err == 0))
                        errMsg = @"������ ��� ������ �������� ��� ������(����) (irole = " + id_role + @"), ������������ (iuser=" + id_user + @")";
                    else
                    {
                        query = @"SELECT * from " + m_nameTableProfilesUnit;
                        m_tblTypes = DbTSQLInterface.Select(ref dbConn, query, null, null, out err);

                        if (!(err == 0))
                            errMsg = @"������ ��� ������ ����� ������ �������� ��� ������(����) (irole = " + id_role + @"), ������������ (iuser=" + id_user + @")";
                        else
                            ;
                    }
                }

                if (
                    (!(err == 0))
                    && (bThrow == true)
                    )
                    throw new Exception(@"HProfiles::HProfiles () - " + errMsg + @"...");
                else
                    ;
            }

            /// <summary>
            /// ������� ��������� �������
            /// </summary>
            /// <param name="id">ID ����(unit)</param>
            /// <returns></returns>
            public static object GetAllowed(int id)
            {
                object objRes = false;
                bool bValidate = false;
                int indxRowAllowed = -1;
                Int16 val = -1
                   ,  type = -1;
                string strVal = string.Empty;

                DataRow [] rowsAllowed = m_tblValues.Select (@"ID_UNIT=" + id);
                switch (rowsAllowed.Length)
                {
                    case 1:
                        indxRowAllowed = 0;
                        break;
                    case 2:
                        //� ����. � ����������� ����������� 'id' ���������� ��� ��� "����", ��� � ��� "������������"
                        // ��������� ������� ������ � 'IS_ROLE' == 0 (������������)
                        // ...
                        foreach (DataRow r in rowsAllowed)
                        {
                            indxRowAllowed++;
                            if (Int16.Parse(r[@"IS_ROLE"].ToString()) == 0)
                                break;
                            else
                                ;
                        }
                        break;
                    default: //������ - ����������
                        throw new Exception(@"HUsers.HProfiles::GetAllowed (id=" + id + @") - �� ������� �� ����� ������...");
                        //Logging.Logg().Error(@"HUsers.HProfiles::GetAllowed (id=" + id + @") - �� ������� �� ����� ������...", Logging.INDEX_MESSAGE.NOT_SET);
                        break;
                }

                // �������� �� �����, �.�. ���������� ����������
                //if ((!(indxRowAllowed < 0))
                //    && (indxRowAllowed < rowsAllowed.Length))
                //{
                    strVal = !(indxRowAllowed < 0) ? rowsAllowed[indxRowAllowed][@"VALUE"].ToString().Trim() : string.Empty;

                    //�� �������������� ��������� ������ ����� ���...
                    Int16.TryParse(m_tblTypes.Select(@"ID=" + id)[0][@"ID_UNIT"].ToString(), out type);
                    switch (type)
                    {
                        case 8: //bool
                            bValidate = Int16.TryParse(strVal, out val);
                            if (bValidate == true)
                                objRes = val == 1;
                            else
                                objRes = false;

                            objRes = objRes.ToString();
                            break;
                        case 9: //string
                        case 10: //int
                            objRes = strVal;
                            break;
                        default:
                            throw new Exception(@"HUsers.HProfiles::GetAllowed (id=" + id + @") - �� ������ ��� ���������...");
                    }
                //} else ;

                return objRes;
            }

            /// <summary>
            /// ������� ���������� ���� ������� ��� ������������
            /// </summary>
            public static void SetAllowed(int iListenerId, int id, string val)
            {
                string query = string.Empty;
                int err = -1
                    , cntRows = -1;
                DbConnection dbConn = null;

                //��������� ������� �������������� ������...
                cntRows = m_tblValues.Select(@"ID_UNIT=" + id).Length;
                switch (cntRows)
                {
                    case 1: //������� ������...
                        query = @"INSERT INTO " + m_nameTableProfilesData + @" ([ID_EXT],[IS_ROLE],[ID_UNIT],[VALUE]) VALUES (" + Id + @",0," + id + @",'" + val + @"')";
                        break;
                    case 2: //���������� ������...
                        query = @"UPDATE " + m_nameTableProfilesData + @" SET [VALUE]='" + val + @"' WHERE ID_EXT=" + Id + @" AND IS_ROLE=0 AND ID_UNIT=" + id;
                        break;
                    default: //������ - ����������
                        throw new Exception(@"HUsers.HProfiles::SetAllowed (id=" + id + @") - �� ������� �� ����� ������...");
                }

                dbConn = DbSources.Sources().GetConnection(iListenerId, out err);
                if ((!(dbConn == null)) && (err == 0))
                {
                    DbTSQLInterface.ExecNonQuery(ref dbConn, query, null, null, out err);
                    //��������� ��������� ����������...
                    if (err == 0)
                    {//�������� ������� ���������������� ��������...
                        switch (cntRows)
                        {
                            case 1: //������� ������...
                                m_tblValues.Rows.Add(new object [] { Id, 0, id, val });
                                break;
                            case 2: //���������� ������...
                                DataRow[] rows = m_tblValues.Select(@"ID_EXT=" + Id + @" AND IS_ROLE=0 AND ID_UNIT=" + id);
                                rows[0][@"VALUE"] = val;
                                break;
                            default: //������ - ����������
                                //������ ���������� - ������� ����������...
                                break;
                        }
                    }
                    else
                        ;
                }
                else
                    ;
            }
        }

        protected static HProfiles m_profiles;

        //�������������� �� ��
        //public enum ID_ROLES { ...

        //������ � ������������
        public enum INDEX_REGISTRATION { ID, DOMAIN_NAME, ROLE, ID_TEC, COUNT_INDEX_REGISTRATION };
        public static object [] s_REGISTRATION_INI = new object [(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION]; //����������������� � �����/�� ������������

        protected enum STATE_REGISTRATION { UNKNOWN = -1, CMD, INI, ENV, COUNT_STATE_REGISTRATION };
        protected string[] m_NameArgs; //����� = COUNT_INDEX_REGISTRATION

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

        private DelegateObjectFunc[] f_arRegistration; // = { registrationCmdLine, registrationINI, registrationEnv };

        public HUsers(int iListenerId)
        {
            Logging.Logg().Action(@"HUsers::HUsers () - ... ���-�� ���������� ���./������ = " + (Environment.GetCommandLineArgs().Length - 1) +
                @"; DomainUserName = " + Environment.UserDomainName + @"\" + Environment.UserName +
                @"; MashineName=" + Environment.MachineName
                , Logging.INDEX_MESSAGE.NOT_SET);

            try {
                //�������������� ����� '��������� ������'
                m_NameArgs = new string[] { @"iuser", @"udn", @"irole", @"itec" }; //����� = COUNT_INDEX_REGISTRATION

                f_arRegistration = new DelegateObjectFunc[(int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION];
                f_arRegistration[(int)STATE_REGISTRATION.CMD] = registrationCmdLine;
                f_arRegistration[(int)STATE_REGISTRATION.INI] = registrationINI;
                f_arRegistration[(int)STATE_REGISTRATION.ENV] = registrationEnv;

                m_DataRegistration = new object[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
                m_StateRegistration = new STATE_REGISTRATION[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];

                ClearValues();
            } catch (Exception e) {
                Logging.Logg().Exception(e, @"HUsers::HUsers ()...", Logging.INDEX_MESSAGE.NOT_SET);
            }

            Logging.Logg().Debug(@"HUsers::HUsers () - ... �������� �������� ...", Logging.INDEX_MESSAGE.NOT_SET);

            for (int i = 0; i < (int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION; i++)
                if (i == (int)STATE_REGISTRATION.ENV) f_arRegistration[i](iListenerId); else f_arRegistration[i](null);
        }

        public static void Update (int iListenerId)
        {
            m_profiles.Update(iListenerId, (int)m_DataRegistration[(int)INDEX_REGISTRATION.ROLE], (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID], false);
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
            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationCmdLine () - ���� ...", Logging.INDEX_MESSAGE.NOT_SET);
            
            //��������� CMD_LINE
            string[] args = Environment.GetCommandLineArgs();

            //���� �� ��������� � CMD_LINE
            if (args.Length > 1)
            {
                if ((args.Length == 2) && ((args[1].Equals(@"/?") == true) || (args[1].Equals(@"?") == true) || (args[1].Equals(@"/help") == true) || (args[1].Equals(@"help") == true)))
                { //������ ���������-���������...
                }
                else
                    ;

                for (int i = 1; i < m_NameArgs.Length; i++)
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        if (!(args[j].IndexOf(m_NameArgs[i]) < 0))
                        {
                            //�������� ������
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

        /// <summary>
        /// ������� ������� ��� ������ ������������
        /// </summary>
        private void registrationINI(object par)
        {
            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - ���� ...", Logging.INDEX_MESSAGE.NOT_SET);

            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - ������ ������� ���������� INI = " + s_REGISTRATION_INI.Length, Logging.INDEX_MESSAGE.NOT_SET);

            //��������� ��������� INI
            if (m_bRegistration == false) {
                bool bValINI = false;
                for (int i = 1; i < m_DataRegistration.Length; i++)
                {
                    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - ��������� ��������� [" + i + @"]", Logging.INDEX_MESSAGE.NOT_SET);

                    try
                    {
                        if (m_StateRegistration[i] == STATE_REGISTRATION.UNKNOWN)
                        {
                            bValINI = false;
                            //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - ��������� ��������� = " + m_StateRegistration[i].ToString());

                            //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - ������ ��������� = " + s_REGISTRATION_INI[i].ToString());

                            //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - ��� ��������� = " + s_REGISTRATION_INI[i].GetType().Name);

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
                        Logging.Logg().Exception(e, @"HUsers::HUsers () - ... registrationINI () - �������� �� ��������� [" + i + @"]", Logging.INDEX_MESSAGE.NOT_SET);
                    }
                }
            }
            else
            {
            }
        }

        /// <summary>
        /// ������ �������� ������������ 
        /// </summary>
        private void registrationEnv(object par)
        {
            int idListener = (int)par //idListener = ((int [])par)[0]
                //, indxDomainName = ((int[])par)[1]
                ;

            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - ���� ... idListener = " + idListener + @"; m_bRegistration = " + m_bRegistration.ToString() + @"; m_StateRegistration = " + m_StateRegistration, Logging.INDEX_MESSAGE.NOT_SET);

            //��������� ��������� DataBase
            if (m_bRegistration == false) {
                Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = " + m_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME].ToString(), Logging.INDEX_MESSAGE.NOT_SET);

                try {
                    if (m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] == STATE_REGISTRATION.UNKNOWN) {
                        Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = " + Environment.UserDomainName + @"\" + Environment.UserName, Logging.INDEX_MESSAGE.NOT_SET);
                        //���������� �� ENV
                        //�������� ���_������������
                        m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = Environment.UserDomainName + @"\" + Environment.UserName;
                        m_StateRegistration [(int)INDEX_REGISTRATION.DOMAIN_NAME] = STATE_REGISTRATION.ENV;
                    }
                    else {
                    }
                } catch (Exception e) {
                    Logging.Logg().Exception(e, @"HUsers::HUsers () - ... registrationEnv () ... �������� ���_������������ ... ", Logging.INDEX_MESSAGE.NOT_SET);
                    throw e;                    
                }

                int err = -1;
                DataTable dataUsers;

                DbConnection connDB = DbSources.Sources().GetConnection((int)par, out err);

                if ((! (connDB == null)) && (err == 0))
                {
                    //�������� ���_������������
                    GetUsers(ref connDB, @"DOMAIN_NAME=" + @"'" + m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] + @"'", string.Empty, out dataUsers, out err);

                    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - ������� ������������� = " + dataUsers.Rows.Count, Logging.INDEX_MESSAGE.NOT_SET);

                    if ((err == 0) && (dataUsers.Rows.Count > 0))
                    {//������� ���� �� ���� ������
                        int i = -1;
                        for (i = 0; i < dataUsers.Rows.Count; i ++)
                        {
                            //�������� IP-�����                    
                            //for (indxIP = 0; indxIP < listIP.Length; indxIP ++) {
                            //    if (listIP[indxIP].Equals(System.Net.IPAddress.Parse (dataUsers.Rows[i][@"IP"].ToString())) == true) {
                            //        //IP ������
                            //        break;
                            //    }
                            //    else
                            //        ;
                            //}

                            //�������� ���_������������
                            if (dataUsers.Rows[i][@"DOMAIN_NAME"].ToString().Trim().Equals((string)m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME], StringComparison.CurrentCultureIgnoreCase) == true) break; else ;
                        }

                        if (i < dataUsers.Rows.Count)
                        {
                            m_DataRegistration[(int)INDEX_REGISTRATION.ID] = Convert.ToInt32(dataUsers.Rows[i]["ID"]); m_StateRegistration[(int)INDEX_REGISTRATION.ID] = STATE_REGISTRATION.ENV;
                            m_DataRegistration[(int)INDEX_REGISTRATION.ROLE] = Convert.ToInt32(dataUsers.Rows[i]["ID_ROLE"]); m_StateRegistration[(int)INDEX_REGISTRATION.ROLE] = STATE_REGISTRATION.ENV;
                            m_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC] = Convert.ToInt32(dataUsers.Rows[i]["ID_TEC"]); m_StateRegistration[(int)INDEX_REGISTRATION.ID_TEC] = STATE_REGISTRATION.ENV;
                        }
                        else
                            throw new Exception("������������ �� ������ � ������ �� ������������");
                    }
                    else
                    {//�� ������� �� ����� ������
                        if (connDB == null)
                            throw new HException(-4, "��� ���������� � �� ������������");
                        else
                            if (err == 0)
                                throw new HException(-3, "������������ �� ������ � ������ �� ������������");
                            else
                                throw new HException(-2, "������ ��������� ������ ������������� �� �� ������������");
                    }
                } else {//��� ����������� ��� ��������
                    if (connDB == null)
                        throw new HException(-4, "��� ���������� � �� ������������");
                    else
                        if (! (err == 0))
                            throw new HException(-3, "�� ������� ���������� ����� � �� ������������");
                        else
                            ;
                }
            }
            else {
                Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - m_bRegistration = " + m_bRegistration.ToString(), Logging.INDEX_MESSAGE.NOT_SET);
            }

            try {
                m_profiles = createProfiles (idListener, (int)m_DataRegistration[(int)INDEX_REGISTRATION.ROLE], (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID]);
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

            strMes += @"; Version(����/�����)=" + ProgramBase.AppProductVersion;

            Logging.Logg().Action(strMes, Logging.INDEX_MESSAGE.NOT_SET);
        }

        /// <summary>
        /// ������� ��������� ������ ������� ������������
        ///  /// <returns>������ ������ �������</returns>
        /// </summary>
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

        /// <summary>
        /// ������� ������� ��� ������ ������������
        /// </summary>
        public static void GetUsers(ref DbConnection conn, string where, string orderby, out DataTable users, out int err)
        {
            err = 0;
            users = null;
            
            if (! (conn == null))
            {
                users = new DataTable();
                Logging.Logg().Debug(@"HUsers::GetUsers () - ������ ��� ������ ������������� = [" + getUsersRequest(where, orderby) + @"]", Logging.INDEX_MESSAGE.NOT_SET);
                users = DbTSQLInterface.Select(ref conn, getUsersRequest(where, orderby), null, null, out err);
            } else {
                err = -1;
            }
        }

        /// <summary>
        /// ������� ������ ����� �� ��
        /// </summary>
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

        /// <summary>
        /// ���������� �� ������������
        /// </summary>
        public static int Id
        {
            get
            {
                return (m_DataRegistration == null) ? 0 : ((!((int)INDEX_REGISTRATION.ID_TEC < m_DataRegistration.Length)) || (m_DataRegistration[(int)INDEX_REGISTRATION.ID] == null)) ? 0 : (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID];
            }
        }

        /// <summary>
        /// ���������� �������� ���
        /// </summary>
        public static string DomainName
        {
            get
            {
                return (string)m_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME];
            }
        }

        /// <summary>
        /// ���������� �� ���
        /// </summary>
        public static int allTEC
        {
            get
            {
                return (m_DataRegistration == null) ? 0 : ((!((int)INDEX_REGISTRATION.ID_TEC < m_DataRegistration.Length)) || (m_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC] == null)) ? 0 : (int)m_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC];
            }
        }


        public static bool IsAllowed (int id) { return bool.Parse(GetAllowed (id)); }
        public static DataTable GetTableProfileUnits { get { return HProfiles.GetTableUnits; } }
        public static string GetAllowed(int id) { return (string)HProfiles.GetAllowed(id); }
        public static void SetAllowed(int iListenerId, int id, string val) { HProfiles.SetAllowed(iListenerId, id, val); }
    }
}
