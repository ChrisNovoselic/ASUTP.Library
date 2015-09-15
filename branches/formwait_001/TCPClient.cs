using System;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

using System.Net;
using System.Net.Sockets;

using System.IO;

namespace HClassLibrary
{
    public class TCPClient
    {
        int m_port;

        TcpClient m_tcpClient;
        NetworkStream m_networkStream;
        StreamReader m_streamReader;
        StreamWriter m_streamWriter;

        public TCPClient()
        {
            InitializeComponent();

            m_port = 6666;
            m_tcpClient = null;
        }

        private void InitializeComponent()
        {

        }

        public bool Connected {
            get {
                bool bRes = false;

                if (! (m_tcpClient == null))
                    bRes = m_tcpClient.Connected;
                else
                    ;

                return bRes;
            }
        }

        public void Init (string hostName) {
            try
            {
                m_tcpClient = new TcpClient(hostName, m_port);

                if (Connected == true) {
                    m_networkStream = m_tcpClient.GetStream();
                    m_streamReader = new StreamReader(m_networkStream);
                    m_streamWriter = new StreamWriter(m_networkStream);

                    SendRecieve ("INIT");
                }
                else
                    ;
            }
            catch (Exception ex)
            { //SocketException, IOException
                Console.WriteLine(ex);
            }
        }

        private void SendRecieve (string msg)
        {
            string response;

            m_streamWriter.WriteLine(msg);
            m_streamWriter.Flush();

            //m_networkStream = m_tcpClient.GetStream();
            //m_streamReader = new StreamReader(m_networkStream);
            //m_streamWriter = new StreamWriter(m_networkStream);

            response = m_streamReader.ReadLine();
            if (response == "Ok")
                ;
            else
                ;
        }

        public void Disconnect () {
            if (Connected == true)
                SendRecieve ("DISCONNECT");
            else
                ;

            if (!(m_tcpClient == null))
            {
                m_tcpClient.Close ();
            }
            else
                ;
        }
    }
}