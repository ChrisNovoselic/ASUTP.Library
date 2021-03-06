﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Runtime.Remoting.Messaging;

using System.Windows.Forms; //Control

namespace HClassLibrary
{
    public class EventArgsDataHost {
        public int id;
        public object [] par;

        public EventArgsDataHost (int id_, object [] arObj) {
            id = id_;
            par = new object [arObj.Length];
            arObj.CopyTo(par, 0);
        }
    }

    public interface IPlugIn
    {
        IPlugInHost Host { get; set; }

        object Object { get; }

        Type TypeOfObject { get ; }
    }

    public abstract class HPlugIn : IPlugIn
    {
        IPlugInHost _host;
        //protected Type _type;
        protected object _object;
        public Int16 _Id;
        protected HMark m_markDataHost;
        private ManualResetEvent m_evObjectHandleCreated;

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
            m_markDataHost = new HMark ();
            m_evObjectHandleCreated = new ManualResetEvent (false);
            //EvtDataRecievedHost += new DelegateObjectFunc(OnEvtDataRecievedHost);
        }

        ////Вариант №1 - создание объекта по шаблону
        //private static T CreateType<T>() where T : new()
        //{
        //    return new T();
        //}

        ////Вариант №2 - создание сущности объекта по шаблону
        //private static T GetInstance<T>(params object[] args)
        //{
        //    return (T)Activator.CreateInstance(typeof(T), args);
        //}

        ////Вариант №3 - создание сущности объекта по шаблону - возвращает 'object'
        //private static object GetInstance<T>(params object[] args)
        //{
        //    return Activator.CreateInstance(typeof(T), args);
        //}

        ////Вариант №1
        //protected bool createObject <T>() {
        //    bool bRes = false;
            
        //    if (_object == null) {
        //        _object = GetInstance <T> (this);

        //        bRes = true; //Объект только создан
        //    }
        //    else
        //        ;

        //    return bRes;
        //}

        //Вариант №2
        /// <summary>
        /// Создание объекта(объектов) библиотеки
        /// </summary>
        /// <returns>признак создания</returns>
        protected bool createObject (Type type)
        {
            bool bRes = false;

            if (_object == null)
            {
                _object = Activator.CreateInstance(type, this);

                if (_object is Control)
                    ((Control)_object).HandleCreated += new EventHandler(plugInObject_HandleCreated);
                else
                    ;

                bRes = true; //Объект только создан
            }
            else
                ;

            return bRes;
        }

        private void plugInObject_HandleCreated (object obj, EventArgs ev) {
            m_evObjectHandleCreated.Set ();
        }

        public object Object
        {
            get
            {
                return _object;
            }
        }

        public Type TypeOfObject {
            get {
                return _object.GetType ();
            }
        }

        protected void DataAskedHost(object par)
        {
            //Вариант №1 - без потока
            EvtDataAskedHost.BeginInvoke(new EventArgsDataHost (_Id, new object [] { par } ), new AsyncCallback(this.dataRecievedHost), new Random ());

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

        /// <summary>
        /// Обработчик выбора пункта меню для плюг'ина
        /// </summary>
        /// <param name="obj">объект-инициатор события</param>
        /// <param name="ev">параметры события</param>
        public abstract void OnClickMenuItem (object obj, EventArgs ev);

        /// <summary>
        /// Событие запроса данных для плюг'ина из главной формы
        /// </summary>
        public event DelegateObjectFunc EvtDataAskedHost;
        //public event DelegateObjectFunc EvtDataRecievedHost;
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public virtual void OnEvtDataRecievedHost(object obj) {
            if (_object is Control)
                m_evObjectHandleCreated.WaitOne (-1);
            else
                ;

            m_markDataHost.Marked(((EventArgsDataHost)obj).id);
        }
    }

    public interface IPlugInHost
    {
        bool Register(IPlugIn plug);
    }
}
