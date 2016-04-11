using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
//using System.ComponentModel;
using System.Data;
//using System.Data.SqlClient;
using System.Data.OleDb;
using System.IO;
//using MySql.Data.MySqlClient;

using System.Globalization;

using HClassLibrary;

namespace HClassLibrary
{
    interface IHHandlerDb
    {
        void ActionReport(string msg);
        void ClearStates();
        void ClearValues();
        void ErrorReport(string msg);
        void ReportClear(bool bClear);
        void Request(int idListener, string request);
        void SetDelegateReport(DelegateStringFunc ferr, DelegateStringFunc fwar, DelegateStringFunc fact, DelegateBoolFunc fclr);
        void SetDelegateWait(DelegateFunc dStart, DelegateFunc dStop, DelegateFunc dStatus);
        void StartDbInterfaces();
        void Stop();
        void StopDbInterfaces();
        void WarningReport(string msg);
    }

    public abstract class HHandlerDb : HHandler, HClassLibrary.IHHandlerDb
    {
        /// <summary>
        /// ������� ������� ���������� � ����������� �������� (����������)
        /// </summary>
        protected DelegateFunc delegateStartWait;
        /// <summary>
        /// ������� ������� ���������� � ����������� �������� (����������)
        /// </summary>
        protected DelegateFunc delegateStopWait;
        /// <summary>
        /// ������� ������� ���������� � ����������� �������� (���������� ���������)
        /// </summary>
        protected DelegateFunc delegateEventUpdate;

        protected DelegateStringFunc errorReport;
        protected DelegateStringFunc warningReport;
        protected DelegateStringFunc actionReport;
        protected DelegateBoolFunc clearReportStates;
        /// <summary>
        /// ������������� (�������) ���������� � ���������� ���������� ��� ���������� �������
        /// </summary>
        protected int m_IdListenerCurrent;
        /// <summary>
        /// ������� ��������������� ���������� � ���������� ����������
        /// </summary>
        protected Dictionary <int, int []> m_dictIdListeners;
        /// <summary>
        /// ����������� - ��������
        /// </summary>
        public HHandlerDb()
            : base()
        {
            //������� ��������������� ���������� � ���������� ���������� - ������
            m_dictIdListeners = new Dictionary<int,int[]> ();
        }        
        /// <summary>
        /// ����������� ��������� ���������� � ������������� �� �����, ����, ����������� ����������
        /// </summary>
        /// <param name="id">���� ������ ���������� ����������</param>
        /// <param name="indx">��� ��������� ����������</param>
        /// <param name="connSett">��������� ���������� � ���������� ����������</param>
        /// <param name="name">������������ ��������� ����������</param>
        protected virtual void register(int id, int indx, ConnectionSettings connSett, string name)
        {
            string strDesc = @"����������=" + name + @", DESC=" + indx.ToString();
            m_dictIdListeners[id][indx] = DbSources.Sources().Register(connSett, true, strDesc);
            //Console.WriteLine (@"HHandlerDb::register (" + strDesc + @") - iListenerId=" + m_dictIdListeners[id][indx]);
        }
        /// <summary>
        /// ����� ��������� ��������
        /// </summary>
        public abstract void StartDbInterfaces();
        /// <summary>
        /// ���������� ��������� ��������
        /// </summary>
        private void stopDbInterfaces()
        {
            if (!(m_dictIdListeners == null))
                foreach (int key in m_dictIdListeners.Keys)
                    for (int i = 0; i < m_dictIdListeners[key].Length; i++)
                    {
                        if (!(m_dictIdListeners[key][i] < 0))
                        {
                            DbSources.Sources().UnRegister(m_dictIdListeners[key][i]);
                            m_dictIdListeners[key][i] = -1;
                        }
                        else
                            ;
                    }
            else
                //������ ������ ���-���� ����������������
                Logging.Logg().Error(@"HHandlerDb::stopDbInterfaces () - m_dictIdListeners == null ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// ���������� ��������� ��������
        /// </summary>
        public void StopDbInterfaces()
        {
            stopDbInterfaces();
        }
        /// <summary>
        /// ���������� �������� ���������� � ����������� ��������
        /// </summary>
        /// <param name="dStart">������� (����������)</param>
        /// <param name="dStop"></param>
        /// <param name="dStatus"></param>
        public void SetDelegateWait(DelegateFunc dStart, DelegateFunc dStop, DelegateFunc dStatus)
        {
            this.delegateStartWait = dStart;
            this.delegateStopWait = dStop;
            this.delegateEventUpdate = dStatus;
        }
        /// <summary>
        /// ���������� �������� ���������� � ����������� ���������� ���������
        /// </summary>
        /// <param name="ferr"></param>
        /// <param name="fwar"></param>
        /// <param name="fact"></param>
        /// <param name="fclr"></param>
        public void SetDelegateReport(DelegateStringFunc ferr, DelegateStringFunc fwar, DelegateStringFunc fact, DelegateBoolFunc fclr)
        {
            this.errorReport = ferr;
            this.warningReport = fwar;
            this.actionReport = fact;
            this.clearReportStates = fclr;
        }

        protected void MessageBox(string msg, MessageBoxButtons btn = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            //MessageBox.Show(this, msg, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Logging.Logg().Error(msg, Logging.INDEX_MESSAGE.NOT_SET);
        }

        //protected abstract bool InitDbInterfaces ();

        public void Request(int idListener, string request)
        {
            DbSources.Sources().Request(m_IdListenerCurrent = idListener, request);
        }

        protected virtual int response(int idListener, out bool error, out object outobj/*, bool bIsTec*/)
        {
            //return DbSources.Sources().Response(idListener, out error, out table);

            int iRes = -1;
            DataTable table = null;
            iRes = DbSources.Sources().Response(idListener, out error, out table);
            outobj  = table as DataTable;

            return iRes;
        }

        protected int response(out bool error, out object outobj/*, bool bIsTec*/)
        {
            return response(m_IdListenerCurrent, out error, out outobj);
        }

        //protected abstract int StateCheckResponse(int /*StatesMachine*/ state, out bool error, out DataTable table);

        //protected abstract int StateResponse(int /*StatesMachine*/ state, DataTable table);

        public override void ClearStates()
        {
            base.ClearStates ();

            if (!(clearReportStates == null))
                clearReportStates (true);
            else
                ;
        }

        public abstract void ClearValues();

        public override void Stop()
        {
            StopDbInterfaces ();
            
            base.Stop ();
        }        
        /// <summary>
        /// ���������� ������ �� ��������� �������� ������� ������� ~ ���� ����
        /// </summary>
        /// <param name="typeDB">��� ����</param>
        /// <param name="idListatener">�������� ������������� ���������� � ��</param>
        protected void GetCurrentTimeRequest(DbInterface.DB_TSQL_INTERFACE_TYPE typeDB, int idListener)
        {
            string query = string.Empty;

            switch (typeDB)
            {
                case DbInterface.DB_TSQL_INTERFACE_TYPE.MySQL:
                    query = @"SELECT now()";
                    break;
                case DbInterface.DB_TSQL_INTERFACE_TYPE.MSSQL:
                    query = @"SELECT GETDATE()";
                    break;
                case DbInterface.DB_TSQL_INTERFACE_TYPE.Oracle:
                    query = @"SELECT SYSTIMESTAMP FROM dual";
                    break;
                default:
                    break;
            }

            if (query.Equals(string.Empty) == false)
                Request(idListener, query);
            else
                ;
        }
        /// <summary>
        /// �������� ������ ��������� � ������� ��� �����������
        /// </summary>
        /// <param name="msg">������-���������� ������</param>
        public void ErrorReport (string msg) {
            if (!(errorReport == null))
                //�������� ������-������ ��� �����������
                errorReport (msg);
            else
                ;
        }
        /// <summary>
        /// �������� ������ ��������� � ��������������� ��� �����������
        /// </summary>
        /// <param name="msg"></param>
        public void WarningReport(string msg)
        {
            if (!(warningReport == null))
                //�������� ������-�������������� ��� �����������
                warningReport(msg);
            else
                ;
        }
        /// <summary>
        /// �������� ������ ��������� � ��������� �������� ��� �����������
        /// </summary>
        /// <param name="msg"></param>
        public void ActionReport(string msg)
        {
            if (! (actionReport == null))
                //�������� ������-�������� ��� �����������
                actionReport(msg);
            else
                ;
        }
        /// <summary>
        /// �������� ��� ���������� ����� ��������� ��� �����������
        /// </summary>
        /// <param name="bClear"></param>
        public void ReportClear (bool bClear)
        {
            clearReportStates (bClear);
        }
    }
}
