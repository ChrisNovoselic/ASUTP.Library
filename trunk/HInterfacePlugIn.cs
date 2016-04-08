using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Runtime.Remoting.Messaging;

using System.Windows.Forms; //Control

namespace HClassLibrary
{
    /// <summary>
    /// Класс для обмена данными между объектами клиент-сервер
    /// </summary>
    public class EventArgsDataHost {
        /// <summary>
        /// Идентификатор объекта-клиента, отправляющего сообщение
        ///  , сервер обязательно должен "знать" этот идентификатор
        ///  , а по нему определить объект-клиент для отправления ответа
        /// </summary>
        public int id_main;

        public int id_detail;
        /// <summary>
        /// Объект-клиент, получатель запрашиваемых данных
        /// </summary>
        public IDataHost reciever;
        /// <summary>
        /// Массив аргументов, детализирующие сообщение
        /// </summary>
        public object [] par;
        /// <summary>
        /// Конструктор - основной (с параметрами)
        /// </summary>
        /// <param name="id_">Идентификатор объекта</param>
        /// <param name="arObj">Массив аргументов сообщения</param>
        public EventArgsDataHost (int id_main_, int id_detail_, object [] arObj) {
            id_main = id_main_;
            id_detail = id_detail_;
            reciever = null;
            par = new object [arObj.Length];
            arObj.CopyTo(par, 0);
        }
        /// <summary>
        /// Конструктор - дополнительный (с параметрами)
        /// </summary>
        /// <param name="objReciever">Объект-клиент, получатель запрашиваемых данных</param>
        /// <param name="arObj">Массив аргументов сообщения</param>
        public EventArgsDataHost(IDataHost objReciever, object[] arObj)
        {
            id_detail = -1;
            reciever = objReciever;
            par = new object[arObj.Length];
            arObj.CopyTo(par, 0);
        }
    }
    /// <summary>
    /// Перечисление идентификаторов-типов сообщений
    /// </summary>
    public enum ID_DATA_ASKED_HOST {
        UNKNOWN = -1
        , INIT_SOURCE, INIT_SIGNALS
        , START, STOP, ACTIVATE
        , TABLE_RES
        , TO_INSERT/*, TO_START*/, TO_STOP
        , ERROR
    }
    /// <summary>
    /// Перечисление дополнительных идентификаторов-типов сообщений
    ///  , передавать в том же массиве аргументов для указания направления сообщения
    ///  (запросить, подтвердить получение)
    /// </summary>
    public enum ID_HEAD_ASKED_HOST
    {
        GET, CONFIRM
    }

    public interface IPlugIn
    {
        IPlugInHost Host { get; set; }

        object GetObject(int key);

        Type GetTypeObject(int key);
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
        /// <param name="par">Аргумент с детализацией запрашиваемых данных</param>
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
                    handler.BeginInvoke(new EventArgsDataHost((int)(par as object[])[0], (int)(par as object[])[1], new object[] { (par as object[])[2] }), new AsyncCallback(this.dataRecievedHost), new Random());

                ////Вариант №2 - с потоком
                //Thread thread = new Thread (new ParameterizedThreadStart (dataAskedHost));
                //thread.Start(par);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, @"HDataHost::DataAskedHost () - ...", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        public bool IsEvtDataAskedHostHandled { get { return ! (EvtDataAskedHost == null); } }
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

    public abstract class PlugInBase : HDataHost, IPlugIn
    {
        /// <summary>
        /// Объект интерфейса подписчика
        /// </summary>
        IPlugInHost _host;
        /// <summary>
        /// Массив зарегистрированных типов плюгИна
        /// </summary>
        protected Dictionary <int, Type> _types;
        /// <summary>
        /// Массив созданных
        /// </summary>
        protected Dictionary<int, object> _objects;
        /// <summary>
        /// Идентификатор плюгина
        /// </summary>
        public int _Id;
        /// <summary>
        /// Счетчик полученных команд/сообщений от подписчика по индексу
        /// </summary>
        protected Dictionary<KeyValuePair <int, int>, uint> m_dictDataHostCounter;
        
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
            m_dictDataHostCounter = new Dictionary<KeyValuePair<int, int>, uint>();
            _types = new Dictionary<int, Type>();
            _objects = new Dictionary<int, object>();
        }
        /// <summary>
        /// Зарегистрировать тип объекта библиотеки
        /// </summary>
        /// <param name="key">Ключ регистрируемого типа объекиа</param>
        /// <param name="type">Регистрируемый тип</param>
        protected virtual void registerType(int key, Type type)
        {
            _types.Add (key, type);
        }

        public List<string> GetListRegisterNameTypes()
        {
            List<string> listRes = new List<string> ();

            return listRes;
        }

        public Dictionary<int, Type> GetRegisterTypes()
        {
            return _types;
        }

        public bool IsRegistred(int key)
        {
            return _types.Keys.Contains(key);
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
        protected bool createObject (int key)
        {
            bool bRes = false;

            if (_objects.ContainsKey(key) == false)
            {
                _objects.Add(key, Activator.CreateInstance(_types[key], this));

                if (_objects[key] is Control)
                {
                    //if (_object is HPanelCommon)
                    //    (_object as HPanelCommon).Start();
                    //else
                    //    ;

                    //((Control)_object).HandleCreated += new EventHandler(plugInObject_HandleCreated);
                    //((Control)_object).HandleDestroyed += new EventHandler(plugInObject_HandleDestroyed);
                }
                else
                    ;

                bRes = true; //Объект только создан
            }
            else
            {
                //if (_object is Control)
                //{
                //    if (_object is HPanelCommon)
                //        if ((_object as HPanelCommon).Started == false)
                //            (_object as HPanelCommon).Start();
                //        else
                //            (_object as HPanelCommon).Stop();
                //    else
                //        ;
                //    //(_object as Control).
                //}
                //else
                //    ;

                ////_object = null;
            }

            return bRes;
        }
        ///// <summary>
        ///// Обработчик события "завершено создание элемента управления"
        ///// </summary>
        ///// <param name="obj">Элемент управления</param>
        ///// <param name="ev">Аргумент для сопровождения события</param>
        //private void plugInObject_HandleCreated (object obj, EventArgs ev) {
        //    m_evObjectHandleCreated.Set ();
        //}

        //private void plugInObject_HandleDestroyed(object obj, EventArgs ev)
        //{
        //    //m_evObjectHandleCreated.Reset();
        //}
        /// <summary>
        /// Возвратить объект 'плюгина'
        /// </summary>
        public object GetObject (int key)
        {
            return _objects[key];
        }
        /// <summary>
        /// Возвратить тип объекта 'плюгина'
        /// </summary>
        public Type GetTypeObject (int key) {
            return ((_types.ContainsKey(key) == false) ? Type.EmptyTypes[0] : _types[key]);
        }
        /// <summary>
        /// Отправить данные получателю (подписчику)
        /// </summary>
        /// <param name="par">Объект с передаваемыми данными (может быть массивом объектов)</param>
        public override void DataAskedHost(object par)
        {
            base.DataAskedHost(new object[] { _Id, (par as object[])[0], (par as object[])[1] });
        }
        
        //protected bool _MarkReversed;
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public override void OnEvtDataRecievedHost(object obj) {
            KeyValuePair<int, int> pair = new KeyValuePair<int, int>(((EventArgsDataHost)obj).id_main, ((EventArgsDataHost)obj).id_detail);

            if (m_dictDataHostCounter.ContainsKey(pair) == true)
                m_dictDataHostCounter[pair]++;
            else
                m_dictDataHostCounter.Add(pair, 1);
        }

        protected bool isDataHostMarked(int id_main, int id_detail)
        {
            KeyValuePair<int, int> pair = new KeyValuePair<int, int>(id_main, id_detail);

            return (m_dictDataHostCounter.ContainsKey(pair) == true)
                && (m_dictDataHostCounter[pair] % 2 == 1);
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
        int Register(IPlugIn plug);
    }
}
