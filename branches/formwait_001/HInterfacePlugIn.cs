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

    public enum ID_DATA_ASKED_HOST {
        UNKNOWN = -1
        , INIT_SOURCE, INIT_SIGNALS
        , START, STOP, ACTIVATE
        , TABLE_RES
        , TO_INSERT
        , ERROR
    }

    public interface IPlugIn
    {
        IPlugInHost Host { get; set; }

        object Object { get; }

        Type TypeOfObject { get ; }
    }

    public interface IDataHost
    {
        /// <summary>
        /// Событие запроса данных для плюг'ина из главной формы
        /// </summary>
        event DelegateObjectFunc EvtDataAskedHost;
        /// <summary>
        /// Отиравить запрос на получение данных
        /// </summary>
        /// <param name="par"></param>
        void DataAskedHost(object par);
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        void OnEvtDataRecievedHost(object res);
    }

    public abstract class HDataHost : IDataHost
    {
        public event DelegateObjectFunc EvtDataAskedHost;

        public virtual void DataAskedHost(object par)
        {
            try
            {
                //Вариант №1 - без потока
                //EvtDataAskedHost.BeginInvoke(new EventArgsDataHost((int)(par as object[])[0], new object[] { (par as object[])[1] }), new AsyncCallback(this.dataRecievedHost), new Random());
                var arHandlers = EvtDataAskedHost.GetInvocationList();
                foreach (var handler in arHandlers.OfType<DelegateObjectFunc>())
                    handler.BeginInvoke(new EventArgsDataHost((int)(par as object[])[0], new object[] { (par as object[])[1] }), new AsyncCallback(this.dataRecievedHost), new Random());

                ////Вариант №2 - с потоком
                //Thread thread = new Thread (new ParameterizedThreadStart (dataAskedHost));
                //thread.Start(par);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"HDataHost::DataAskedHost () - ...");
            }
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

        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public abstract void OnEvtDataRecievedHost(object obj);
    }

    public abstract class PlugInMenuItem : PlugInBase
    {
        //Int16 IdOwnerMenuItem { get; }
        public abstract string NameOwnerMenuItem { get; }

        public abstract string NameMenuItem { get; }

        /// <summary>
        /// Обработчик выбора пункта меню для плюг'ина
        /// </summary>
        /// <param name="obj">объект-инициатор события</param>
        /// <param name="ev">параметры события</param>
        public abstract void OnClickMenuItem(object obj, EventArgs ev);
    }

    public abstract class PlugInBase : HDataHost, IPlugIn
    {
        IPlugInHost _host;
        //protected Type _type;
        protected object _object;
        public int _Id;
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

        public PlugInBase()
            : base()
        {
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
        /// <summary>
        /// Обработчик события "завершено создание элемента управления"
        /// </summary>
        /// <param name="obj">Элемент управления</param>
        /// <param name="ev">Аргумент для сопровождения события</param>
        private void plugInObject_HandleCreated (object obj, EventArgs ev) {
            m_evObjectHandleCreated.Set ();
        }
        /// <summary>
        /// Возвратить объект 'плюгина'
        /// </summary>
        public object Object
        {
            get
            {
                return _object;
            }
        }
        /// <summary>
        /// Возвратить тип объекта 'плюгина'
        /// </summary>
        public Type TypeOfObject {
            get {
                return _object.GetType ();
            }
        }
        /// <summary>
        /// Отправить данные получателю (подписчику)
        /// </summary>
        /// <param name="par">Объект с передаваемыми данными (может быть массивом объектов)</param>
        public override void DataAskedHost(object par)
        {
            base.DataAskedHost(new object [] {_Id, par});
        }
        //public event DelegateObjectFunc EvtDataRecievedHost;
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public override void OnEvtDataRecievedHost(object obj) {
            if (_object is Control)
                m_evObjectHandleCreated.WaitOne (System.Threading.Timeout.Infinite, true);
            else
                ;

            m_markDataHost.Marked(((EventArgsDataHost)obj).id);
        }
    }
    /// <summary>
    /// Интерфейс для контейнера 'плюгинов'
    /// </summary>
    public interface IPlugInHost
    {
        /// <summary>
        /// Регистрировать 'плюгин'
        /// </summary>
        /// <param name="plug">Регистрируемый 'плюгин'</param>
        /// <returns>Результат регистрации</returns>
        bool Register(IPlugIn plug);
    }
}
