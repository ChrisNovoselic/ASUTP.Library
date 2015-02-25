using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Data; //DataTable
using System.Threading;
using System.Data.Common; //DbConnection

namespace HClassLibrary
{
    public abstract class LogParse
    {
        public enum TYPE_LOGMESSAGE { START, STOP,
                                    DBOPEN, DBCLOSE, DBEXCEPTION,
                                    ERROR, DEBUG,
                                    DETAIL,
                                    SEPARATOR, SEPARATOR_DATETIME,
                                    UNKNOWN, COUNT_TYPE_LOGMESSAGE };
        public static string[] DESC_LOGMESSAGE = { "Запуск", "Выход",
                                            "БД открыть", "БД закрыть", "БД исключение",
                                            "Ошибка", "Отладка",
                                            "Детализация",
                                            "Раздел./сообщ.", "Раздел./дата/время",
                                            "Неопределенный тип" };

        protected string[] SIGNATURE_LOGMESSAGE = { ProgramBase.MessageWellcome, ProgramBase.MessageExit,
                                                    DbTSQLInterface.MessageDbOpen, DbTSQLInterface.MessageDbClose, DbTSQLInterface.MessageDbException,
                                                    "!Ошибка!", "!Отладка!",
                                                    string.Empty,
                                                    Logging.MessageSeparator, Logging.DatetimeStampSeparator,
                                                    string.Empty, "Неопределенный тип" };

        private Thread m_thread;
        protected DataTable m_tableLog;
        Semaphore m_semAllowed;
        //bool m_bAllowed;

        public DelegateFunc Exit;

        public LogParse ()
        {
            m_semAllowed = new Semaphore (1, 1);

            m_tableLog = new DataTable("ContentLog");
            DataColumn[] cols = new DataColumn[] { new DataColumn("DATE_TIME", typeof(DateTime)),
                                                    new DataColumn("TYPE", typeof(Int32)),
                                                    new DataColumn ("MESSAGE", typeof (string)) };
            m_tableLog.Columns.AddRange(cols);

            m_thread = null;
        }

        public void Start(object text)
        {
            m_semAllowed.WaitOne ();

            m_thread = new Thread(new ParameterizedThreadStart(Thread_Proc));
            m_thread.IsBackground = true;
            m_thread.Name = "Разбор лог-файла";

            m_thread.Start (text);
        }

        public void Stop ()
        {
            //if (m_bAllowed == false)
            //    return;
            //else
            //    ;

            bool joined = false;
            if ((!(m_thread == null)))
            {
                if (m_thread.IsAlive == true)
                {
                    //m_bAllowed = false;
                    joined = m_thread.Join (6666);
                    if (joined == false)
                        m_thread.Abort ();
                    else
                        ;
                }
                else
                    ;

                try { m_semAllowed.Release(); }
                catch (Exception e)
                {
                    Console.WriteLine("LogParse::Stop () - m_semAllowed.Release() - поток не был запущен или штатно завршился");
                }
            }
            else
                ;

            //m_bAllowed = true;
        }

        protected virtual void Thread_Proc (object data)
        {
            Console.WriteLine("Окончание обработки лог-файла. Обработано строк: {0}", m_tableLog.Rows.Count);

            Stop ();

            Exit ();
        }

        public int Select (int type, string beg, string end, ref DataRow []res)
        {
            int iRes = -1;
            string where = string.Empty;

            if (!(beg.Equals (string.Empty) == true))
            {
                where = "DATE_TIME>='" + DateTime.Parse (beg).ToString ("yyyyMMdd HH:mm:ss") + "'";
                if (!(end.Equals (string.Empty) == true))
                    where += " AND DATE_TIME<'" + DateTime.Parse (end).ToString("yyyyMMdd HH:mm:ss") + "'";
                else
                    ;
            }
            else
                ;

            if (!(type < 0))
            {
                if (!(where.Equals (string.Empty) == true))
                    where += " AND ";
                else
                    ;

                where += "TYPE=" + type;
            }
            else
                ;

            res = m_tableLog.Select(where, "DATE_TIME");

            return iRes;
        }
    }

    public class LogParse_File : LogParse
    {
        protected override void Thread_Proc(object text)
        {
            string line;
            TYPE_LOGMESSAGE typeMsg;
            DateTime dtMsg;
            bool bValid = false;

            line = string.Empty;

            typeMsg = TYPE_LOGMESSAGE.UNKNOWN;

            StringReader content = new StringReader(text as string);

            m_tableLog.Clear();

            Console.WriteLine("Начало обработки лог-файла. Размер: {0}", (text as string).Length);

            //Чтение 1-ой строки лог-файла
            line = content.ReadLine();

            while (!(line == null))
            {
                //Проверка на разделитель сообщение-сообщение
                if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR]) == true)
                {
                    //Следующая строка - дата/время
                    line = content.ReadLine();

                    //Попытка разбора дата/время
                    bValid = DateTime.TryParse(line, out dtMsg);
                    if (bValid == true)
                    {
                        //Следующая строка - разделитель дата/время-сообщение
                        content.ReadLine();
                        //Следующая строка - сообщение
                        line = content.ReadLine();
                    }
                    else
                    {
                        //Установка дата/время предыдущего сообщения
                        if (m_tableLog.Rows.Count > 0)
                            dtMsg = (DateTime)m_tableLog.Rows[m_tableLog.Rows.Count - 1]["DATE_TIME"];
                        else
                            ; //dtMsg = new DateTime(1666, 06, 06, 06, 06, 06);
                    }
                }
                else
                {
                    //Попытка разбора дата/время
                    bValid = DateTime.TryParse(line, out dtMsg);
                    if (bValid == true)
                    {
                        //Следующая строка - разделитель дата/время-сообщение
                        content.ReadLine();
                        //Следующая строка - сообщение
                        line = content.ReadLine();
                    }
                    else
                    {
                        //Установка дата/время предыдущего сообщения
                        //dtMsg = new DateTime(1666, 06, 06, 06, 06, 06);
                        dtMsg = (DateTime)m_tableLog.Rows[m_tableLog.Rows.Count - 1]["DATE_TIME"];

                        if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR_DATETIME]) == true)
                        {
                            //Следующая строка - сообщение
                            line = content.ReadLine();
                        }
                        else
                        {
                            //Строка содержит сообщение/детализация сообщения
                        }
                    }
                }

                if (line == null)
                {
                    break;
                }
                else
                    ;

                //Проверка на разделитель сообщение-сообщение
                if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR]) == true)
                {
                    continue;
                }
                else
                {
                    if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.START]) == true)
                    {
                        typeMsg = TYPE_LOGMESSAGE.START;
                    }
                    else
                        if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.STOP]) == true)
                        {
                            typeMsg = TYPE_LOGMESSAGE.STOP;
                        }
                        else
                            if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.DBOPEN]) == true)
                            {
                                typeMsg = TYPE_LOGMESSAGE.DBOPEN;
                            }
                            else
                                if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.DBCLOSE]) == true)
                                {
                                    typeMsg = TYPE_LOGMESSAGE.DBCLOSE;
                                }
                                else
                                    if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.DBEXCEPTION]) == true)
                                    {
                                        typeMsg = TYPE_LOGMESSAGE.DBEXCEPTION;
                                    }
                                    else
                                        if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.ERROR]) == true)
                                        {
                                            typeMsg = TYPE_LOGMESSAGE.ERROR;
                                        }
                                        else
                                            if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.DEBUG]) == true)
                                            {
                                                typeMsg = TYPE_LOGMESSAGE.DEBUG;
                                            }
                                            else
                                                typeMsg = TYPE_LOGMESSAGE.DETAIL;

                    //Добавление строки в таблицу с собщениями
                    m_tableLog.Rows.Add(new object[] { dtMsg, (int)typeMsg, line });

                    //Чтение строки сообщения
                    line = content.ReadLine();

                    while ((!(line == null)) && (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR]) == false))
                    {
                        //Добавление строки в таблицу с собщениями
                        m_tableLog.Rows.Add(new object[] { dtMsg, (int)TYPE_LOGMESSAGE.DETAIL, line });

                        //Чтение строки сообщения
                        line = content.ReadLine();
                    }
                }
            }

            base.Thread_Proc (text as object);
        }
    }

    public class LogParse_DB : LogParse
    {
        protected override void Thread_Proc(object data)
        {
            int err = -1;
            DbConnection connLoggingDB = DbSources.Sources().GetConnection(Int32.Parse((data as string).Split(',')[0]), out err);

            string query = @"SELECT DATEPART (DD, [DATETIME_WR]) as DD, DATEPART (MM, [DATETIME_WR]) as MM, DATEPART (YYYY, [DATETIME_WR]) as [YYYY]"
                    + @" FROM [techsite-2.X.X].[dbo].[logging]"
                    + @" WHERE [ID_USER] = 52"
                    + @" GROUP BY DATEPART (DD, [DATETIME_WR]), DATEPART (MM, [DATETIME_WR]), DATEPART (YYYY, [DATETIME_WR])"
                    + @" ORDER BY [DD]";

            DbTSQLInterface.Select(ref connLoggingDB, query, null, null, out err);

            base.Thread_Proc (data);
        }
    }
}
