using System;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using System.IO;

namespace HClassLibrary
{
    public class TCPServer
    {
        Thread m_thread;
        List<TCPReciever> m_listThreadClient;
        int m_port;
        ManualResetEvent m_evClose;

        public TCPServer()
        {
            InitializeComponent();
            m_port = 6666;

            m_thread = new Thread(new ParameterizedThreadStart(Thread_Proc));
            m_thread.Name = "TCP - socket server listen";
            m_thread.IsBackground = true;

            m_listThreadClient = new List <TCPReciever> ();

            m_evClose = new ManualResetEvent (true); //.Set ();
        }

        private void InitializeComponent()
        {
        }

        public void Start () {
            m_thread.Start ();
        }

        public void Stop () {
            bool joined = false;

            if (m_thread.IsAlive)
            {
                m_evClose.Reset ();

                joined = m_thread.Join(6666);
                if (joined == false)
                    m_thread.Abort();
                else
                    ;
            }
            else
                ;
        }

        void ExitTCPReciever (object tcpReciever)
        {
            //((TCPReciever)tcpReciever).Stop ();
            m_listThreadClient.Remove(((TCPReciever)tcpReciever));
        }

        private void Thread_Proc(object data)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, m_port);
            tcpListener.Start();

            Console.WriteLine("The Server has started on port " + m_port);

            while (m_evClose.WaitOne (66) == true)
            {
                if (tcpListener.Pending() == true) {
                    m_listThreadClient.Add (new TCPReciever(tcpListener.AcceptTcpClient()));
                    m_listThreadClient [m_listThreadClient.Count - 1].exitThread = ExitTCPReciever;
                    m_listThreadClient [m_listThreadClient.Count - 1].Start ();
                }
                else {
                }               
            }

            for (int i = 0; i < m_listThreadClient.Count; i ++) {
                m_listThreadClient [i].Stop ();
            }
        }
    }

    internal class TCPReciever
    {
        Socket m_socketReciever;
        ManualResetEvent m_evClose;
        Thread m_thread;

        public DelegateObjectFunc exitThread;

        public TCPReciever(TcpClient rec)
        {
            InitializeComponent();

            m_socketReciever = rec.Client;
            m_evClose = new ManualResetEvent(true);

            m_thread = new Thread(new ParameterizedThreadStart(Thread_Proc));
            m_thread.Name = "TCP - reciever";
            m_thread.IsBackground = true;
        }

        private void InitializeComponent()
        {
        }

        private void Thread_Proc(object data)
        {
            bool bParseMsg = false;
            string msgOfClient = "Client connected...";
            StreamWriter streamWriter;
            StreamReader streamReader;
            NetworkStream networkStream;

            try
            {
                if (m_socketReciever.Connected == true)
                {
                    networkStream = new NetworkStream(m_socketReciever);
                    streamWriter = new StreamWriter(networkStream);
                    streamReader = new StreamReader(networkStream);

                    while (m_evClose.WaitOne(66) == true)
                    {
                        Console.WriteLine(((IPEndPoint)m_socketReciever.LocalEndPoint).Address + ": " + msgOfClient);

                        msgOfClient = streamReader.ReadLine();
                        bParseMsg = false;

                        switch (msgOfClient)
                        {
                            case null:
                                break;
                            case "":
                                break;
                            case "INIT":
                                bParseMsg = true;
                                break;
                            case "ACCESS_LOGFILE_UNLOCK":
                                //Разрешить доступ к лог-файлу (продолжить работу в штатном режиме)
                                bParseMsg = true;
                                break;
                            case "ACCESS_LOGFILE_LOCK":
                                //Запретить доступ к лог-файлу (для возможности его чтения)
                                bParseMsg = true;
                                break;
                            case "DISCONNECT":
                                //Отобразить на консоли принятую команду
                                Console.WriteLine(((IPEndPoint)m_socketReciever.LocalEndPoint).Address + ": " + msgOfClient);
                                //Прервать цикл приема сообщений/команд
                                m_evClose.Reset();

                                bParseMsg = true;
                                break;
                            default:
                                break;
                        }

                        if (bParseMsg == true)
                        {
                            //Послать подтверждение приема команды
                            streamWriter.WriteLine("Ok");
                            streamWriter.Flush();
                        }
                        else
                            ;
                    }
                }
                else
                    ;

                if (m_socketReciever.Connected)
                {
                    //Разорвать соединение - если было открыто
                    m_socketReciever.Disconnect(false);
                }
                else
                    ;

                //Освободить все ресурсы связанные с соединением
                m_socketReciever.Close();

                Console.Read();
            }
            catch (Exception ex)
            { //SocketException, IOException
                Console.WriteLine(ex);
            }

            exitThread(this);
        }

        public void Start()
        {
            m_thread.Start();
        }

        public void Stop()
        {
            m_evClose.Reset();

            bool joined = m_thread.Join(666);
            if (joined == false)
                m_thread.Abort();
            else
                ;
        }
    }
}