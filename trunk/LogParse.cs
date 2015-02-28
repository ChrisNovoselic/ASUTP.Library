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
        public static int[] s_IdTypeMessages;

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
            m_thread.Name = "������ ���-�����";

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
                    Console.WriteLine("LogParse::Stop () - m_semAllowed.Release() - ����� �� ��� ������� ��� ������ ���������");
                }
            }
            else
                ;

            //m_bAllowed = true;
        }

        protected virtual void Thread_Proc (object data)
        {
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
                ProgramBase.ss_MainCultureInfo;

            Console.WriteLine("��������� ��������� ���-�����. ���������� �����: {0}", (int)data);

            Exit ();
        }

        public void Clear ()
        {
            //m_tableLog.Clear();
        }

        public int Select (int indxType, string beg, string end, ref DataRow []res)
        {
            int iRes = -1;
            string where = string.Empty;

            //m_tableLog.Clear();

            if (beg.Equals (string.Empty) == false)
            {
                where = "DATE_TIME>='" + DateTime.Parse (beg).ToString ("yyyyMMdd HH:mm:ss") + "'";
                if (end.Equals (string.Empty) == false)
                    where += " AND DATE_TIME<'" + DateTime.Parse (end).ToString("yyyyMMdd HH:mm:ss") + "'";
                else
                    ;
            }
            else
                ;

            if (!(indxType < 0))
            {
                if (where.Equals (string.Empty) == false)
                    where += " AND ";
                else
                    ;

                where += "TYPE=" + s_IdTypeMessages[indxType];
            }
            else
                ;

            res = m_tableLog.Select(where, "DATE_TIME");

            return res.Length > 0 ? res.Length : -1;
        }
    }

    public class LogParse_File : LogParse
    {
        public enum TYPE_LOGMESSAGE
        {
            START, STOP,
            DBOPEN, DBCLOSE, DBEXCEPTION,
            ERROR, DEBUG,
            DETAIL,
            SEPARATOR, SEPARATOR_DATETIME,
            UNKNOWN, COUNT_TYPE_LOGMESSAGE
        };
        public static string[] DESC_LOGMESSAGE = { "������", "�����",
                                            "�� �������", "�� �������", "�� ����������",
                                            "������", "�������",
                                            "�����������",
                                            "������./�����.", "������./����/�����",
                                            "�������������� ���" };

        protected string[] SIGNATURE_LOGMESSAGE = { ProgramBase.MessageWellcome, ProgramBase.MessageExit,
                                                    DbTSQLInterface.MessageDbOpen, DbTSQLInterface.MessageDbClose, DbTSQLInterface.MessageDbException,
                                                    "!������!", "!�������!",
                                                    string.Empty,
                                                    Logging.MessageSeparator, Logging.DatetimeStampSeparator,
                                                    string.Empty, "�������������� ���" };

        public LogParse_File()
        {
            s_IdTypeMessages = new int[] {
                0, 1
                , 2, 3, 4
                , 5, 6
                , 7
                , 8, 9
                , 10
                , 11
            };
        }

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

            Console.WriteLine("������ ��������� ���-�����. ������: {0}", (text as string).Length);

            //������ 1-�� ������ ���-�����
            line = content.ReadLine();

            while (!(line == null))
            {
                //�������� �� ����������� ���������-���������
                if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR]) == true)
                {
                    //��������� ������ - ����/�����
                    line = content.ReadLine();

                    //������� ������� ����/�����
                    bValid = DateTime.TryParse(line, out dtMsg);
                    if (bValid == true)
                    {
                        //��������� ������ - ����������� ����/�����-���������
                        content.ReadLine();
                        //��������� ������ - ���������
                        line = content.ReadLine();
                    }
                    else
                    {
                        //��������� ����/����� ����������� ���������
                        if (m_tableLog.Rows.Count > 0)
                            dtMsg = (DateTime)m_tableLog.Rows[m_tableLog.Rows.Count - 1]["DATE_TIME"];
                        else
                            ; //dtMsg = new DateTime(1666, 06, 06, 06, 06, 06);
                    }
                }
                else
                {
                    //������� ������� ����/�����
                    bValid = DateTime.TryParse(line, out dtMsg);
                    if (bValid == true)
                    {
                        //��������� ������ - ����������� ����/�����-���������
                        content.ReadLine();
                        //��������� ������ - ���������
                        line = content.ReadLine();
                    }
                    else
                    {
                        //��������� ����/����� ����������� ���������
                        //dtMsg = new DateTime(1666, 06, 06, 06, 06, 06);
                        dtMsg = (DateTime)m_tableLog.Rows[m_tableLog.Rows.Count - 1]["DATE_TIME"];

                        if (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR_DATETIME]) == true)
                        {
                            //��������� ������ - ���������
                            line = content.ReadLine();
                        }
                        else
                        {
                            //������ �������� ���������/����������� ���������
                        }
                    }
                }

                if (line == null)
                {
                    break;
                }
                else
                    ;

                //�������� �� ����������� ���������-���������
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

                    //���������� ������ � ������� � ����������
                    m_tableLog.Rows.Add(new object[] { dtMsg, (int)typeMsg, line });

                    //������ ������ ���������
                    line = content.ReadLine();

                    while ((!(line == null)) && (line.Contains(SIGNATURE_LOGMESSAGE[(int)TYPE_LOGMESSAGE.SEPARATOR]) == false))
                    {
                        //���������� ������ � ������� � ����������
                        m_tableLog.Rows.Add(new object[] { dtMsg, (int)TYPE_LOGMESSAGE.DETAIL, line });

                        //������ ������ ���������
                        line = content.ReadLine();
                    }
                }
            }

            base.Thread_Proc(m_tableLog.Rows.Count);
        }
    }    
}
