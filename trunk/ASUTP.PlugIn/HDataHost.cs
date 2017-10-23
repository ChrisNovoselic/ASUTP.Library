using ASUTP;
using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace ASUTP.PlugIn {
    public abstract class HDataHost : IDataHost {
        public event DelegateObjectFunc EvtDataAskedHost;

        public virtual void DataAskedHost (object par)
        {
            // [0] - главный идентификатор
            // [1][0] - вспомогательный идентификатор
            // [1][...] - остальные параметры - parToAsked
            object [] parToAsked = null;

            try {
                parToAsked = new object [((par as object []) [1] as object []).Length - 1];
                for (int i = 0; i < parToAsked.Length; i++)
                    parToAsked [i] = ((par as object []) [1] as object []) [i + 1];

                //Вариант №1 - без потока
                //EvtDataAskedHost.BeginInvoke(new EventArgsDataHost((int)(par as object[])[0], new object[] { (par as object[])[1] }), new AsyncCallback(this.dataRecievedHost), new Random());
                var arHandlers = EvtDataAskedHost.GetInvocationList ();
                foreach (var handler in arHandlers.OfType<DelegateObjectFunc> ())
                    handler.BeginInvoke (new EventArgsDataHost (
                            (int)(par as object []) [0]
                            , (int)((par as object []) [1] as object []) [0]
                            , parToAsked)
                        , new AsyncCallback (this.dataRecievedHost), new Random ());

                ////Вариант №2 - с потоком
                //Thread thread = new Thread (new ParameterizedThreadStart (dataAskedHost));
                //thread.Start(par);
            } catch (Exception e) {
                Logging.Logg ().Exception (e, @"HDataHost::DataAskedHost () - ...", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        public bool IsEvtDataAskedHostHandled
        {
            get
            {
                return !(EvtDataAskedHost == null);
            }
        }
        ////Потоковая функция для вар.№2
        //private void dataAskedHost (object obj) {
        //    IAsyncResult res = EvtDataAskedHost.BeginInvoke(obj, new AsyncCallback(this.dataRecievedHost), null);
        //}

        private void dataRecievedHost (object res)
        {
            if ((res as AsyncResult).EndInvokeCalled == false)
                ; //((DelegateObjectFunc)((AsyncResult)res).AsyncDelegate).EndInvoke(res as AsyncResult);
            else
                ;
        }
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public abstract void OnEvtDataRecievedHost (object obj);
    }

}
