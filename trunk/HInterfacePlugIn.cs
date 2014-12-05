using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace HClassLibrary
{
    public class EventArgsDataHost {
        public int id;
        public object par;

        public EventArgsDataHost (int id_, object o) {
            id = id_;
            par = o;
        }
    }
    
    public interface IPlugIn
    {
        IPlugInHost Host { get; set; }

        object Object { get; }
    }

    public abstract class HPlugIn : IPlugIn
    {
        IPlugInHost _host;
        protected object _object;
        public Int16 _Id;

        public IPlugInHost Host
        {
            get { return _host; }
            set
            {
                _host = value;
                _host.Register(this);
            }
        }

        public HPlugIn () : base () {
            //EvtDataRecievedHost += new DelegateObjectFunc(OnEvtDataRecievedHost);
        }

        protected abstract bool createObject();

        public object Object
        {
            get
            {
                createObject();

                return _object;
            }
        }

        protected void DataAskedHost(object par)
        {
            //Вариант №1 - без потока
            EvtDataAskedHost.BeginInvoke(new EventArgsDataHost (_Id, par), new AsyncCallback(this.dataRecievedHost), new Random ());

            ////Вариант №2 - спотоком
            //Thread thread = new Thread (new ParameterizedThreadStart (dataAskedHost));
            //thread.Start(par);
        }

        ////Потоковая функция для вар.№2
        //private void dataAskedHost (object obj) {
        //    IAsyncResult res = EvtDataAskedHost.BeginInvoke(obj, new AsyncCallback(this.dataRecievedHost), null);
        //}

        private void dataRecievedHost(object res)
        {
            if ((res as AsyncResult).EndInvokeCalled == false)
                ; //((DelegateObjectFunc)((AsyncResult)res).AsyncDelegate).EndInvoke(res as AsyncResult);
            else
                ;
        }

        //Int16 IdOwnerMenuItem { get; }
        public abstract string NameOwnerMenuItem { get; }

        public abstract string NameMenuItem { get; }

        public abstract void OnClickMenuItem (object obj, EventArgs ev);

        public event DelegateObjectFunc EvtDataAskedHost;
        //public event DelegateObjectFunc EvtDataRecievedHost;
        public /*protected*/ abstract void OnEvtDataRecievedHost(object obj);
    }

    public interface IPlugInHost
    {
        bool Register(IPlugIn plug);
    }
}
