using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HClassLibrary
{
    public class TcpClientAsync
    {
        //private enum STATE {READ, WRITE, COUNT_STATE};

        private IPAddress[] m_addresses;
        private int m_port;
        private WaitHandle m_addressesSet,
                m_addressesGet;
        private TcpClient m_tcpClient;
        private int m_failedConnectionCount;

        private string m_ValueToCreate;

        //private ManualResetEvent[] m_arEvState;
        private AutoResetEvent m_evWrite;

        public DelegateStringFunc delegateErrorConnect;
        public delegate void DelegateTcpAsyncFunc(TcpClient res, string data);
        public DelegateTcpAsyncFunc delegateConnect, delegateRead;

        /// <summary>
        /// Конструктор по известному IP-адресу
        /// </summary>
        /// <param name="address">The IP Address of the server</param>
        /// <param name="port">The port of the server</param>
        public TcpClientAsync(IPAddress address, int port) : this (new[] { address }, port) { }

        /// <summary>
        /// Construct a new client where multiple IP Addresses for
        /// the same client are known.
        /// </summary>
        /// <param name="addresses">The array of known IP Addresses</param>
        /// <param name="port">The port of the server</param>
        public TcpClientAsync(IPAddress[] addresses, int port) : this (port)
        {
            m_ValueToCreate = addresses.ToString ();
            this.m_addresses = addresses;
        }

        /// <summary>
        /// Конструктор по IP-адресу или сетевому имени компьютера
        /// известному в домене.
        /// </summary>
        /// <param name="hostNameOrAddress">Сетевое имя или IP-адрес</param>
        /// <param name="port">Номер порт для подключения к серверу</param>
        public TcpClientAsync(string hostNameOrAddress, int port) : this (port)
        {
            m_ValueToCreate = hostNameOrAddress;
            m_addressesSet = new AutoResetEvent(false);
            m_addressesGet = new AutoResetEvent(false);
            Dns.BeginGetHostAddresses(hostNameOrAddress.Split (';')[0], GetHostAddressesCallback, null);
        }

        /// <summary>
        /// Защищенный конструктор для других конструкторов
        /// общий для нескольких операций.
        /// </summary>
        /// <param name="port"></param>
        private TcpClientAsync(int port)
        {
            if (port < 0)
                throw new ArgumentException();
            else
                ;

            this.m_port = port;
            this.m_tcpClient = new TcpClient();
            this.m_Encoding = Encoding.Default;

            m_evWrite = new AutoResetEvent (false);
        }

        public TcpClientAsync()
        {
            this.m_tcpClient = new TcpClient();
            this.m_Encoding = Encoding.Default;

            m_evWrite = new AutoResetEvent(false);
        }

        /// <summary>
        /// Объект, использующийся для кодировки/декодировки отправляемых/получаемых сообщений.
        /// </summary>
        public Encoding m_Encoding { get; set; }

        public void Connect(IPAddress addresses, int port)
        {
            m_ValueToCreate = addresses.ToString ();
            this.m_addresses = new [] {addresses};
            this.m_port = port;

            ((AutoResetEvent)m_evWrite).Reset();
        }
        
        public void Connect(IPAddress[] addresses, int port)
        {
            m_ValueToCreate = addresses.ToString();
            this.m_addresses = addresses;
            this.m_port = port;

            ((AutoResetEvent)m_evWrite).Reset();
        }

        public void Connect(string hostNameOrAddress, int port)
        {
            m_ValueToCreate = hostNameOrAddress;
            if (m_addressesGet == null)
                m_addressesGet = new AutoResetEvent (false);
            else
                ((AutoResetEvent)m_addressesGet).Reset();

            if (m_addressesSet == null)
                m_addressesSet = new AutoResetEvent (false);
            else
                ((AutoResetEvent)m_addressesSet).Reset();

            this.m_port = port;

            ((AutoResetEvent)m_evWrite).Reset();
            
            Dns.BeginGetHostAddresses(hostNameOrAddress.Split (';')[0], GetHostAddressesCallback, null);

            Connect ();
        }

        /// <summary>
        /// Попытка соединения по какому-либо из заданных IP-адресов
        /// </summary>
        public void Connect()
        {
            int indxAddresses = -1;
            if ((!(m_addressesSet == null)) && (!(m_addressesGet == null)))
                //Ожидание пока адрес не будет установлен или произойдет ошибка при его опрежелении из DNS
                indxAddresses = WaitHandle.WaitAny(new WaitHandle[] { m_addressesGet, m_addressesSet });
            else
                ;

            switch (indxAddresses)
            {
                case 0:
                    ErrorConnect ();
                    break;
                case 1:
                    //Установить счетчик сбойных подключений в '0'
                    Interlocked.Exchange(ref m_failedConnectionCount, 0);

                    if (m_tcpClient.Connected == false)
                        m_tcpClient = new TcpClient ();
                    else
                        ;

                    //Start the async connect operation
                    m_tcpClient.BeginConnect(m_addresses, m_port, ConnectCallback, null);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Запись строки в поток с использованием кодировки по умолчанию.
        /// </summary>
        /// <param name="data">Строка для записи</param>
        /// <returns>A WaitHandle может использоваться для определения
        /// завершения операции записи.</returns>
        public void Write(string data)
        {
            byte[] bytes = m_Encoding.GetBytes(data);
            Write(bytes);
        }

        /// <summary>
        /// Запись массива байтов в поток записи открытого соединения
        /// </summary>
        /// <param name="bytes">Массив для записи</param>
        /// <returns>WaitHandle может использоваться для определения
        /// завершения операции записи.</returns>
        public void Write(byte[] bytes)
        {
            if (m_evWrite.WaitOne (666) == true)
            {
                //Запись разрешена
                NetworkStream networkStream = null;
                try { networkStream = m_tcpClient.GetStream(); }
                catch (Exception e)
                {
                    Logging.Logg().Exception(e, "TCPClientAsync::Write(byte[] ) - ...");
                }
                //Начало операции асинхронной записи
                networkStream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, null);
            }
            else
                ; //Запись запрещена при этом происходит потеря сообщения 'bytes'
        }

        /// <summary>
        ///Функция возврата для операции записи
        /// </summary>
        /// <param name="result">The AsyncResult object</param>
        private void WriteCallback(IAsyncResult result)
        {
            if (m_tcpClient.Connected == true)
            {
                NetworkStream networkStream = m_tcpClient.GetStream();
                networkStream.EndWrite(result);
            }
            else
                ;

            m_evWrite.Set();

        }

        /// <summary>
        /// Функция возврата для операции соединения
        /// </summary>
        /// <param name="result">The AsyncResult object</param>
        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                m_tcpClient.EndConnect(result);
            }
            catch (Exception e)
            {
                //Инкрементация количества сбойных попыток установить соединение
                Interlocked.Increment(ref m_failedConnectionCount);

                if (!(m_failedConnectionCount < m_addresses.Length))
                {//Сбой соединения для всех IP-адресов
                    ErrorConnect ();
                }
                else
                    ;

                Console.WriteLine("TCPClientAsync::ConnectCallback () - An error has occured when call this methode...type of exception: " + e.GetType().FullName);

                return;
            }

            //Запрос на соединение завершился успешно
            NetworkStream networkStream = m_tcpClient.GetStream();
            byte[] buffer = new byte[m_tcpClient.ReceiveBufferSize];

            //Подключены и ожидаем сообщения
            networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);

            //Событие готовности к записи
            m_evWrite.Set ();

            //Внешний метод обработки события "успешное соединение"
            delegateConnect(m_tcpClient, null);
        }

        public bool Equals (TcpClient obj)
        {
            return m_tcpClient.Equals (obj);
        }

        public bool Equals(string ValueToCreate)
        {
            return m_ValueToCreate.Equals(ValueToCreate);
        }

        /// <summary>
        /// Callback for Read operation
        /// </summary>
        /// <param name="result">The AsyncResult object</param>
        private void ReadCallback(IAsyncResult result)
        {
            int read = 0;
            NetworkStream networkStream = null;

            try
            {
                networkStream = m_tcpClient.GetStream(); 
                read = networkStream.EndRead(result);
            }
            catch (Exception excpt)
            {
                //An error has occured when reading
                Console.WriteLine("TCPClientAsync::ReadCallback () - An error has occured when call this methode...type of exception: " + excpt.GetType().FullName);
            }

            if (read == 0)
            //if ((read == 0) && (networkStream == null))
            {
                //The connection has been closed.
                m_evWrite.Reset();

                return;
            }
            else
                ;

            if (! (networkStream == null))
            {
                byte[] buffer = result.AsyncState as byte[];
                string data = this.m_Encoding.GetString(buffer, 0, read);

                //Do something with the data object here.
                Console.WriteLine(((IPEndPoint)m_tcpClient.Client.LocalEndPoint).Address + ": " + data);

                delegateRead(m_tcpClient, data);

                if ((!(m_tcpClient == null)) && (m_tcpClient.Connected == true))
                    //Then start reading from the network again.
                    networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);
                else
                    ;
            }
            else
                ;
        }

        /// <summary>
        /// Callback for Get Host Addresses operation
        /// </summary>
        /// <param name="result">The AsyncResult object</param>
        private void GetHostAddressesCallback(IAsyncResult result)
        {
            try {
                m_addresses = Dns.EndGetHostAddresses(result);
            }
            catch (Exception e)
            {
                Console.WriteLine("TCPClientAsync::GetHostAddressesCallback () - Dns.EndGetHostAddresses()");

                ((AutoResetEvent)m_addressesGet).Set();
                
                return;
            }

            //Signal the m_addresses are now set
            ((AutoResetEvent)m_addressesSet).Set();
        }

        public void Disconnect()
        {
            m_evWrite.WaitOne (666);

            if (!(m_tcpClient == null))
            {
                if (m_tcpClient.Client.Connected == true)
                {                    
                    try { m_tcpClient.Client.Shutdown (SocketShutdown.Both); }
                    catch (Exception e)
                    {
                        Console.WriteLine("TCPClientAsync::Disconnect () - tcpClient.Client.Shutdown (SocketShutdown.Both) - An error has occured when call this methode...type exception: " + e.GetType().FullName);
                    }

                    try { m_tcpClient.Close(); }
                    catch (Exception e)
                    {
                        //Logging.Logg().Exception(e, "TCPClientAsync::Disconnect () - tcpClient.Close()");
                        Console.WriteLine("TCPClientAsync::Disconnect () - tcpClient.Close () - An error has occured when call this methode...type exception: " + e.GetType().FullName);
                    }

                    //m_tcpClient = null;
                }
                else
                    ;
            }
            else
                ;
        }

        private void ErrorConnect ()
        {
            delegateErrorConnect(m_ValueToCreate);
        }
    }
}