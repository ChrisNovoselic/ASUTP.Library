using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
//using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Data.OleDb;
using System.IO;
//using MySql.Data.MySqlClient;
using System.Globalization;
using ASUTP;
using ASUTP.Database;

namespace ASUTP.Helper
{
    public class Passwords : object
    {
        MD5CryptoServiceProvider md5;

        public enum INDEX_ROLES : uint { COM_DISP = 1, ADMIN, NSS, LK_DISP };

        /// <summary>
        /// ������������ ������������
        /// </summary>
        public static string getOwnerPass(int indx_role)
        {
            string[] ownersPass = { "����������", "��������������", "����", "��-����������" };

            return ownersPass[indx_role - 1];
        }

        //private volatile Errors passResult;
        private volatile string passReceive;
        private volatile uint m_indxRolePass;
        private volatile uint m_idExtPass;
        private Object m_lockObj;

        /// <summary>
        /// ������������� ���������� � ���������� ���������� ��� ���������� �������
        /// </summary>
        private int m_idListener;

        public Passwords()
        {
            Initialize();
        }

        private void Initialize()
        {
            md5 = new MD5CryptoServiceProvider();

            m_lockObj = new Object();
        }

        /// <summary>
        /// ������������� ���������� � ���������� ���������� ��� ���������� �������
        /// </summary>
        public void SetIdListener (int idListener) { m_idListener = idListener; }

        void MessageBox(string msg, MessageBoxButtons btn = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            //MessageBox.Show(this, msg, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Logging.Logg().Error(msg, Logging.INDEX_MESSAGE.NOT_SET);
        }

        /// <summary>
        /// ������� ��������� ������ ��� ������������  
        /// </summary>
        private Errors GetPassword(out int er)
        {
            Errors errRes = Errors.NoError;
            DbConnection conn = DbSources.Sources ().GetConnection (m_idListener, out er);
            DataTable passTable = DbTSQLInterface.Select(ref conn, GetPassRequest(), null, null, out er);
            if (er == 0)
                if (!(passTable.Rows[0][0] is DBNull))
                    passReceive = passTable.Rows[0][0].ToString();
                else
                    errRes = Errors.ParseError;
            else
                errRes = Errors.NoAccess;

            return errRes;
        }

        public static void Code () {}
        public static void Decode () {}

        /// <summary>
        /// ������� �������� ��������� ������ ��� ������������
        /// </summary>
        public bool SetPassword(string password, uint idExtPass, uint indxRolePass)
        {
            int err = -1;
            Errors passResult = Errors.NoError;

            m_idExtPass = idExtPass;
            m_indxRolePass = indxRolePass;

            byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(password));

            StringBuilder hashedString = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
                hashedString.Append(hash[i].ToString("x2"));

            passResult = GetPassword(out err);

            if (!(passResult == Errors.NoError))
            {
                //MessageBox.Show(this, "������ ��������� ������ " + getOwnerPass () + ". ������ �� �������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox("������ ��������� ������ " + getOwnerPass((int)m_indxRolePass) + ". ������ �� �������.");

                return false;
            }
            else
                ;

            DbConnection conn = DbSources.Sources().GetConnection(m_idListener, out err);
            if (passReceive == null)
                DbTSQLInterface.ExecNonQuery(ref conn, SetPassRequest(hashedString.ToString(), true), null, null,out err);
            else
                DbTSQLInterface.ExecNonQuery(ref conn, SetPassRequest(hashedString.ToString(), false), null, null, out err);

            if (passResult != Errors.NoError)
            {
                //MessageBox.Show(this, "������ ���������� ������ " + getOwnerPass () + ". ������ �� �������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox("������ ���������� ������ " + getOwnerPass((int)m_indxRolePass) + ". ������ �� �������.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ������� �������� ������ ������������
        /// </summary>
        public Errors ComparePassword(string password, uint id_ext, uint indx_role)
        {
            int err = -1;
            Errors errRes = Errors.NoError;
            passReceive = null;

            //if (connSettConfigDB == null)
            //    return HAdmin.Errors.NoAccess;
            //else
            //    ;

            m_idExtPass = id_ext;
            m_indxRolePass = indx_role;

            if (password.Length < 1)
            {
                //MessageBox.Show(this, "����� ������ ������ ����������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox("����� ������ ������ ����������.");

                return Errors.InvalidValue;
            }
            else
                ;

            string hashFromForm = "";
            byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(password));

            StringBuilder hashedString = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
                hashedString.Append(hash[i].ToString("x2"));

            GetPassword(out err);

            if (!(errRes == Errors.NoError))
            {
                //MessageBox.Show(this, "������ ��������� ������ " + getOwnerPass () + ".", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox("������ ��������� ������ " + getOwnerPass((int)m_indxRolePass) + ".");

                return errRes;
            }
            else
                ;

            if (passReceive == null)
            {
                //MessageBox.Show(this, "������ �� ����������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox("������ �� ����������.");

                return Errors.NoSet;
            }
            else
            {
                hashFromForm = hashedString.ToString();

                if (hashFromForm != passReceive)
                {
                    //MessageBox.Show(this, "������ ����� �������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox("������ ����� �������.");

                    return Errors.InvalidValue;
                }
                else
                    return Errors.NoError;
            }
        }

        /// <summary>
        /// ������� ��������� ������ ������� �� �������� ������
        /// </summary>
        private string GetPassRequest()
        {
            string strRes = string.Empty;
            strRes = "SELECT HASH FROM passwords WHERE ID_ROLE=" + m_indxRolePass;

            if (! (m_idExtPass < 0))
                strRes += " AND ID_EXT =" + m_idExtPass;
            else
                ;

            return strRes;
        }

        /// <summary>
        /// ������� �������� ������ ��� ������������
        /// </summary>
        private bool GetPassResponse(DataTable table)
        {
            if (table.Rows.Count != 0)
                try
                {
                    if (table.Rows[0][0] is System.DBNull)
                        passReceive = "";
                    else
                        passReceive = (string)table.Rows[0][0];
                }
                catch
                {
                    return false;
                }
            else
                passReceive = null;

            return true;
        }

        /// <summary>
        /// ������� ��������� ������� ��� ��������� ������
        /// <returns>������</returns>
        /// </summary>
        private string SetPassRequest(string password, bool insert)
        {
            string query = string.Empty;

            if (insert)
                query = "INSERT INTO passwords (ID_EXT, ID_ROLE, HASH) VALUES (" + m_idExtPass +  ", " + m_indxRolePass + ", '" + password + "')";
            else
            {
                query = "UPDATE passwords SET HASH='" + password + "'";
                query += " WHERE ID_EXT=" + m_idExtPass + " AND ID_ROLE=" + m_indxRolePass;
            }

            return query;
        }
    }
}
