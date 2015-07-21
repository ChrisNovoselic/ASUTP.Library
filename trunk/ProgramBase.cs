using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

using System.Threading;
using System.Diagnostics; //Process

namespace HClassLibrary
{
    public delegate void DelegateFunc();
    public delegate void DelegateIntFunc(int param);
    public delegate void DelegateIntIntFunc(int param1, int param2);
    public delegate int IntDelegateIntIntFunc(int param1, int param2);
    public delegate void DelegateStringFunc(string param);
    public delegate void DelegateBoolFunc(bool param);
    public delegate void DelegateObjectFunc(object obj);
    public delegate void DelegateRefObjectFunc(ref object obj);
    public delegate void DelegateDateFunc(DateTime date);

    public delegate int IntDelegateFunc();
    public delegate int IntDelegateIntFunc(int param);
    
    public delegate string StringDelegateIntFunc (int param);
    public delegate string StringDelegateStringFunc(string keyParam);

    public class HException : Exception
    {
        public int m_code;

        public HException(int code, string msg)
            : base(msg)
        {
            m_code = code;
        }
    }

    public enum Errors
    {
        NoError,
        InvalidValue,
        NoAccess,
        NoSet,
        ParseError,
    }

    public enum TYPE_DATABASE_CFG { CFG_190, CFG_200, UNKNOWN };

    public static class ProgramBase
    {
        public enum ID_APP { STATISTIC = 1, TRANS_GTP, TRANS_GTP_TO_NE22, TRANS_GTP_FROM_NE22, TRANS_BYISK_GTP_TO_NE22, TRANS_MODES_CENTRE, TRANS_MODES_CENTRE_GUI, TRANS_MODES_CENTRE_CMD, TRANS_MODES_TERMINALE, TRANS_TG }

        public static System.Globalization.CultureInfo ss_MainCultureInfo = new System.Globalization.CultureInfo(@"ru-Ru");

        public static string MessageWellcome = "***************Старт приложения...***************"
            , MessageExit = "***************Выход из приложения...***************"
            , MessageAppAbort = @"Приложение будет закрыто...";

        public const Int32 TIMER_START_INTERVAL = 666;

        //Журналирование старта приложения
        public static void Start(bool bGUI = true)
        {
            if (bGUI == true)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            else
                ;

            //Установка разделителя целой и дробной части
            ss_MainCultureInfo.NumberFormat.NumberDecimalSeparator = @",";

            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
                ProgramBase.ss_MainCultureInfo;

            s_iMessageShowUnhandledException = 1;
            s_iMessageShowUnhandledExceptionDetail = 1;

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            if (Logging.s_mode == Logging.LOG_MODE.UNKNOWN)
                Logging.s_mode = Logging.LOG_MODE.FILE_EXE;
            else ;
            Logging.Logg().PostStart(MessageWellcome);
        }

        //Журналирование завершения приложения
        public static void Exit()
        {
            List <Form> listApplicationOpenForms = new List<Form> ();
            foreach (Form f in Application.OpenForms)
                listApplicationOpenForms.Add (f);

            foreach (Form f in listApplicationOpenForms)
            {
                if (f is FormMainBase)
                    (f as FormMainBase).Close(true);
                else
                    if (f is FormWait)
                    {
                        //Здесь м. возникнуть ошибка -
                        // вызов формы из потока в котором форма не была создана ???
                        (f as FormWait).StopWaitForm();
                    }
                    else
                        f.Close();
            }

            Logging.Logg().PostStop(MessageExit);
            Logging.Logg().Stop ();

            DbSources.Sources().UnRegister();

            System.ComponentModel.CancelEventArgs cancelEvtArgs = new System.ComponentModel.CancelEventArgs (true);
            Application.Exit(cancelEvtArgs);
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string strHeader = "ProgramBase::Application_ThreadException () - ...";
            if (s_iMessageShowUnhandledException > 0) MessageBox.Show((IWin32Window)null, e.Exception.Message + Environment.NewLine + MessageAppAbort, strHeader); else ;

            // here you can log the exception ...
            Logging.Logg().Exception(e.Exception, Logging.INDEX_MESSAGE.NOT_SET, strHeader);

            Exit ();            
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string strHeader = "ProgramBase::CurrentDomain_UnhandledException () - ..."
                , strBody = string.Empty;
            if (s_iMessageShowUnhandledException > 0)
                if (s_iMessageShowUnhandledExceptionDetail > 0)
                    strBody = (e.ExceptionObject as Exception).ToString ();
                else
                    strBody = (e.ExceptionObject as Exception).Message;
            else ;

            MessageBox.Show((IWin32Window)null, strBody + Environment.NewLine + MessageAppAbort, strHeader);

            // here you can log the exception ...
            Logging.Logg().Exception(e.ExceptionObject as Exception, Logging.INDEX_MESSAGE.NOT_SET, strHeader);

            Exit ();
        }

        //???
        public static void Abort() { }

        public static int s_iAppID = -1;
        public static int s_iMessageShowUnhandledException = -1;
        public static int s_iMessageShowUnhandledExceptionDetail = -1;

        public static string AppName
        {
            get
            {
                return Logging.AppName + ".exe";
            }
        }

        public static string AppProductVersion
        {
            get
            {
                return Application.ProductVersion.ToString ()/*StatisticCommon.Properties.Resources.TradeMarkVersion*/
                    + @" (" + File.GetLastWriteTime(Application.ExecutablePath).ToString(@"dd.MM.yyyy HH:mm:ss") + @")";
            }
        }

        public static void AppExit()
        {
            string commandLineArgs = getCommandLineArgs();
            string exePath = Application.ExecutablePath;
            try
            {
                Application.Exit();
                wait_allowingEvents(6666);
            }
            catch (ArgumentException ex)
            {
                throw;
            }
        }

        public static void AppRestart()
        {
            string commandLineArgs = getCommandLineArgs();
            string exePath = Application.ExecutablePath;
            try
            {
                Application.Exit();
                wait_allowingEvents(6666);
            }
            catch (ArgumentException ex)
            {
                throw;
            }
            Process.Start(exePath, commandLineArgs);
        }

        static string getCommandLineArgs()
        {
            Queue<string> args = new Queue<string>(Environment.GetCommandLineArgs());
            args.Dequeue(); // args[0] is always exe path/filename
            return string.Join(" ", args.ToArray());
        }

        static void wait_allowingEvents(int durationMS)
        {
            DateTime start = DateTime.Now;
            do
            {
                Application.DoEvents();
            } while (start.Subtract(DateTime.Now).TotalMilliseconds > durationMS);
        }
    }
}