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
        /// Делегат функции оповещения о выполняемой операции (выполнение)
        /// </summary>
        protected DelegateFunc delegateStartWait;
        /// <summary>
        /// Делегат функции оповещения о выполняемой операции (завершение)
        /// </summary>
        protected DelegateFunc delegateStopWait;
        /// <summary>
        /// Делегат функции оповещения о выполняемой операции (обновление состояния)
        /// </summary>
        protected DelegateFunc delegateEventUpdate;

        protected DelegateStringFunc errorReport;
        protected DelegateStringFunc warningReport;
        protected DelegateStringFunc actionReport;
        protected DelegateBoolFunc clearReportStates;
        /// <summary>
        /// Идентификатор (текущий) соединения с источником информации при выполнении запроса
        /// </summary>
        protected int m_IdListenerCurrent;
        /// <summary>
        /// Словарь идентификаторов соединения с источником информации
        /// </summary>
        protected Dictionary <int, int []> m_dictIdListeners;
        /// <summary>
        /// Конструктор - основной
        /// </summary>
        public HHandlerDb()
            : base()
        {
            //Словарь идентификаторов соединения с источником информации - пустой
            m_dictIdListeners = new Dictionary<int,int[]> ();
        }        
        /// <summary>
        /// Регистрация источника информации с наименованием по ключю, типу, параметрами соединения
        /// </summary>
        /// <param name="id">Ключ группы источников информации</param>
        /// <param name="indx">Тип источника информации</param>
        /// <param name="connSett">Параметры соединения с источником информации</param>
        /// <param name="name">Наименование источника информации</param>
        protected virtual void register(int id, int indx, ConnectionSettings connSett, string name)
        {
            string strDesc = @"ИстчнкИнфо=" + name + @", DESC=" + indx.ToString();
            m_dictIdListeners[id][indx] = DbSources.Sources().Register(connSett, true, strDesc);
            //Console.WriteLine (@"HHandlerDb::register (" + strDesc + @") - iListenerId=" + m_dictIdListeners[id][indx]);
        }
        /// <summary>
        /// Старт обработки запросов
        /// </summary>
        public abstract void StartDbInterfaces();
        /// <summary>
        /// Остановить обрабртку запросов
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
                //Вообще нельзя что-либо инициализировать
                Logging.Logg().Error(@"HHandlerDb::stopDbInterfaces () - m_dictIdListeners == null ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// Остановить обрабртку запросов
        /// </summary>
        public void StopDbInterfaces()
        {
            stopDbInterfaces();
        }
        /// <summary>
        /// Установить делегаты оповещения о выполняемой операции
        /// </summary>
        /// <param name="dStart">Делегат (выполнение)</param>
        /// <param name="dStop"></param>
        /// <param name="dStatus"></param>
        public void SetDelegateWait(DelegateFunc dStart, DelegateFunc dStop, DelegateFunc dStatus)
        {
            this.delegateStartWait = dStart;
            this.delegateStopWait = dStop;
            this.delegateEventUpdate = dStatus;
        }
        /// <summary>
        /// Установить делегаты оповещения о результатах выполнения опрерации
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
            //MessageBox.Show(this, msg, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

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
        /// Отправляет запрос на получение текущего времени сервера ~ типа СУБД
        /// </summary>
        /// <param name="typeDB">Тип СУБД</param>
        /// <param name="idListatener">Активный идентификатор соединения с БД</param>
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
        /// Передать строку сообщения с ошибкой для отображения
        /// </summary>
        /// <param name="msg">Строка-содержание ошибки</param>
        public void ErrorReport (string msg) {
            if (!(errorReport == null))
                //Передать строку-ошибку для отображения
                errorReport (msg);
            else
                ;
        }
        /// <summary>
        /// Передать строку сообщения с предупреждением для отображения
        /// </summary>
        /// <param name="msg"></param>
        public void WarningReport(string msg)
        {
            if (!(warningReport == null))
                //Передать строку-предупреждение для отображения
                warningReport(msg);
            else
                ;
        }
        /// <summary>
        /// Передать строку сообщения с описанием действия для отображения
        /// </summary>
        /// <param name="msg"></param>
        public void ActionReport(string msg)
        {
            if (! (actionReport == null))
                //Передать строку-действие для отображения
                actionReport(msg);
            else
                ;
        }
        /// <summary>
        /// Очистить все переданные ранее сообщения для отображения
        /// </summary>
        /// <param name="bClear"></param>
        public void ReportClear (bool bClear)
        {
            clearReportStates (bClear);
        }
    }
}
