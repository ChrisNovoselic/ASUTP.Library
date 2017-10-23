using ASUTP.Core;
using ASUTP.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace ASUTP.Control {
    /// <summary>
    /// Класс панели-клиента для отправки_запросов/получения_значений от 
    /// </summary>
    public class PanelCommonDataHost : HPanelCommon, IDataHost {
        /// <summary>
        /// Конструктор - основной (с параметрами по умолчанию)
        /// </summary>
        /// <param name="iColumnCount">Количество столбцов в макете панели</param>
        /// <param name="iRowCount">Количество строк в макете панели</param>
        public PanelCommonDataHost (int iColumnCount = 16, int iRowCount = 16)
            : base (iColumnCount, iRowCount)
        {
            this.ColumnCount = iColumnCount;
            this.RowCount = iRowCount;

            initialize ();
        }
        /// <summary>
        /// Конструктор - допоплнительный (с параметрами по умолчанию)
        /// </summary>
        /// <param name="container">Контэйнер для панели</param>
        /// <param name="iColumnCount">Количество столбцов в макете панели</param>
        /// <param name="iRowCount">Количество строк в макете панели</param>
        public PanelCommonDataHost (IContainer container, int iColumnCount = 16, int iRowCount = 16)
            : base (container, iColumnCount, iRowCount)
        {
            container.Add (this);

            this.ColumnCount = iColumnCount;
            this.RowCount = iRowCount;

            initialize ();
        }

        private void initialize ()
        {
            initializeLayoutStyle ();
        }
        /// <summary>
        /// Событие для запроса значений из объекта сервера
        /// </summary>
        public event DelegateObjectFunc EvtDataAskedHost;
        /// <summary>
        /// Отправить запрос главной форме
        /// </summary>
        /// <param name="idOwner">Идентификатор панели, отправляющей запрос</param>
        /// <param name="par"></param>
        public void DataAskedHost (object par)
        {
            EvtDataAskedHost.BeginInvoke (new EventArgsDataHost (-1, -1, new object [] { par }), new AsyncCallback (this.dataRecievedHost), new Random ());
        }
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public virtual void OnEvtDataRecievedHost (object res)
        {
        }
        /// <summary>
        /// Метод обратного вызова при окончании обработки события 'EvtDataAskedHost'
        /// </summary>
        /// <param name="res">Результат выполнения асинхронной операции обработки события</param>
        private void dataRecievedHost (object res)
        {
            if ((res as AsyncResult).EndInvokeCalled == false)
                ; //((DelegateObjectFunc)((AsyncResult)res).AsyncDelegate).EndInvoke(res as AsyncResult);
            else
                ;
        }

        protected override void initializeLayoutStyle (int cols = -1, int rows = -1)
        {
            initializeLayoutStyleEvenly (cols, rows);
        }
    }
}
