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
            m_dictIdListeners[id][indx] = DbSources.Sources().Register(connSett, true, @"����������=" + name + @", DESC=" + indx.ToString());
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
        protected void GetCurrentTimeRequest(DbInterface.DB_TSQL_INTERFACE_TYPE typeDB, int idListatener)
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
                Request(idListatener, query);
            else
                ;
        }

        /// <summary>
        /// ������������ � �� ��� ���� "������ - ����������� ����� ��"
        /// </summary>
        private static string s_Name_Moscow_TimeZone = @"Russian Standard Time";

        /// <summary>
        /// �������� ����/����� � ���� "������ - ����������� ����� ��"
        /// </summary>
        /// <param name="dt">����/����� ��� ����������</param>
        /// <returns></returns>
        public static DateTime ToMoscowTimeZone(DateTime dt)
        //public static DateTime ToCurrentTimeZone(DateTime dt, int offset_msc)
        {
            DateTime dtRes;

            if (! (dt.Kind == DateTimeKind.Local)) {
            //    dtRes = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.FindSystemTimeZoneById(s_Name_Moscow_TimeZone));
                dtRes = dt.Add(GetUTCOffsetOfMoscowTimeZone ());
            } else {
                dtRes = dt - TimeZoneInfo.Local.GetUtcOffset (dt);
                if (dtRes.IsDaylightSavingTime () == true) {
                    dtRes = dtRes.AddHours(-1);
                } else { }

                dtRes = dtRes.Add(GetUTCOffsetOfMoscowTimeZone());
            //    //dtRes = dtRes.Add(GetUTCOffsetOfCurrentTimeZone(offset_msc));
            }

            return dtRes;
        }

        public static DateTime ToMoscowTimeZone()
        {
            DateTime dtRes
                , dt = DateTime.Now;

            if (!(dt.Kind == DateTimeKind.Local))
                dtRes = dt.Add(GetUTCOffsetOfMoscowTimeZone());
            else
            {
                dtRes = dt - TimeZoneInfo.Local.GetUtcOffset(dt);
                if (dtRes.IsDaylightSavingTime() == true)
                {
                    dtRes = dtRes.AddHours(-1);
                }
                else { }

                dtRes = dtRes.Add(GetUTCOffsetOfMoscowTimeZone());
            }

            return dtRes;
        }

        //public static TimeSpan GetOffsetOfCurrentTimeZone()
        //{
        //    return DateTime.Now - HAdmin.ToCurrentTimeZone(TimeZone.CurrentTimeZone.ToUniversalTime(DateTime.Now));
        //}
        /// <summary>
        /// ���������� �������� ���� "������ - ����������� ����� ��" �� UTC
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetUTCOffsetOfMoscowTimeZone()
        {
            DateTime dtNow = DateTime.Now;

            ////������������ ���� �� ��
            //System.Collections.ObjectModel.ReadOnlyCollection <TimeZoneInfo> tzi = TimeZoneInfo.GetSystemTimeZones ();
            //foreach (TimeZoneInfo tz in tzi) {
            //    Console.WriteLine (tz.DisplayName + @", " +  tz.StandardName + @", " + tz.Id);
            //}

            return
            ////������� �1 - ��������, ���� � ������������ ����������� ���������� (�������� ������� 26.10.2014)
            //    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dtNow, HAdmin.s_Name_Moscow_TimeZone) - DateTime.UtcNow
            ////������� �2 - ��������, ���� � ������������ ����������� ���������� (�������� ������� 26.10.2014)
            //    TimeZoneInfo.FindSystemTimeZoneById(HAdmin.s_Name_Moscow_TimeZone).GetUtcOffset(dtNow)
            ////������� �3 - ��������, ���� � ������������ ����������� ���������� (�������� ������� 26.10.2014) + �������� �������� ���� ������������ �� ���
            //    DateTime.UtcNow - dtNow - TimeSpan.FromHours(offset_msc)
            //������� �4
                TimeSpan.FromHours (3)
            ////������� �5
            //    TimeSpan.FromHours(TimeZone.CurrentTimeZone.GetUtcOffset(dtNow).Hours - TimeZoneInfo.FindSystemTimeZoneById(HHandlerDb.s_Name_Moscow_TimeZone).GetUtcOffset(dtNow).Hours)
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
