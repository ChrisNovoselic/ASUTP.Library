using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection; //Assembly

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
        UNKNOWN = -1
            , GET, CONFIRM
    }

    public interface IPlugIn
    {
        IPlugInHost Host { get; set; }        

        object GetObject(int key);

        Type GetTypeObject(int key);

        int GetKeyType(Type type);

        int GetKeyType(string typeName);
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
            // [0] - главный идентификатор
            // [1][0] - вспомогательный идентификатор
            // [1][...] - остальные параметры - parToAsked
            object[] parToAsked = null;

            try
            {
                parToAsked = new object[((par as object[])[1] as object[]).Length - 1];
                for (int i = 0; i < parToAsked.Length; i ++)
                    parToAsked[i] = ((par as object[])[1] as object[])[i + 1];

                //Вариант №1 - без потока
                //EvtDataAskedHost.BeginInvoke(new EventArgsDataHost((int)(par as object[])[0], new object[] { (par as object[])[1] }), new AsyncCallback(this.dataRecievedHost), new Random());
                var arHandlers = EvtDataAskedHost.GetInvocationList();
                foreach (var handler in arHandlers.OfType<DelegateObjectFunc>())
                    handler.BeginInvoke(new EventArgsDataHost(
                            (int)(par as object[])[0]
                            , (int)((par as object[])[1] as object [])[0]
                            , parToAsked)
                        , new AsyncCallback(this.dataRecievedHost), new Random());

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
        private Dictionary<KeyValuePair <int, int>, uint> _dictDataHostCounter;
        
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
            _dictDataHostCounter = new Dictionary<KeyValuePair<int, int>, uint>();
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

        /// <summary>
        /// Возвратить список наименований зарегистрированных типов библиотек
        /// </summary>
        /// <returns></returns>
        public List<string> GetListRegisterNameTypes()
        {
            List<string> listRes = new List<string> ();

            return _types.Values.Select(type => {
                return type.GetType ().Name; }).ToList();
        }

        /// <summary>
        /// Возвратить словарь зарегистрированных типов библиотек
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Type> GetRegisterTypes()
        {
            return _types;
        }

        /// <summary>
        /// Возвратить признак регистрации типа библиотеки по ее идентификатору
        /// </summary>
        /// <param name="key">Ключ-идентификатор типа библиотеки</param>
        /// <returns>Признак регистрации типа библиотеки</returns>
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
        protected int createObject (int key)
        {
            int iRes = 0; // не было выполнено никаких действий

            if (_objects.ContainsKey(key) == false)
            {
                try
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

                    iRes = 1; //Объект только создан
                }
                catch (Exception e)
                {
                    iRes = -1; //Ошибка при создании объекта

                    Logging.Logg().Exception(e, @"PlugInBase::createObject (ID=" + _Id + @", key=" + key + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);
                }

                
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

            return iRes;
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
        /// Возвратить идентификатор одного из зарегистрированных типов объекта 'плюгина'
        /// </summary>
        public int GetKeyType(Type type)
        {
            int iRes = -1;

            foreach (KeyValuePair<int, Type> pair in _types)
                if (pair.Value.Name == type.Name)
                {
                    iRes = pair.Key;

                    break;
                }
                else
                    ;

            return iRes;
        }
        /// <summary>
        /// Возвратить идентификатор одного из зарегистрированных типов объекта 'плюгина'
        /// </summary>
        public int GetKeyType(string typeName)
        {
            int iRes = -1;

            foreach (KeyValuePair<int, Type> pair in _types)
                if (pair.Value.Name == typeName)
                {
                    iRes = pair.Key;

                    break;
                }
                else
                    ;

            return iRes;
        }
        /// <summary>
        /// Отправить данные получателю (подписчику)
        /// </summary>
        /// <param name="par">Объект с передаваемыми данными (может быть массивом объектов)</param>
        public override void DataAskedHost(object par)
        {
            base.DataAskedHost(new object[] { _Id, par  });
        }

        /// <summary>
        /// Ключ(идентификатор) единственного зарегистрированного типа в объекте библиотеки
        /// </summary>
        public int KeySingleton
        {
            get
            {
                return (_objects.Count == 1) ? _objects.Keys.ElementAt (0) : -1;
            }
        }

        //protected bool _MarkReversed;
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        public override void OnEvtDataRecievedHost(object obj) {
            KeyValuePair<int, int> pair;

            if (Monitor.TryEnter (this) == true) {
                pair = new KeyValuePair<int, int> (((EventArgsDataHost)obj).id_main, ((EventArgsDataHost)obj).id_detail);

                if (_dictDataHostCounter.ContainsKey (pair) == true)
                    _dictDataHostCounter [pair]++;
                else
                    _dictDataHostCounter.Add (pair, 1);

                Monitor.Exit (this);
            } else
                ;

            //Console.WriteLine(@"PlugInBase::OnEvtDataRecievedHost (id=" + pair.Key + @", key=" + pair.Value + @") - counter=" + m_dictDataHostCounter[pair]);
        }

        /// <summary>
        /// Установить принудительно признак использования элемента в объекте библиотеки
        /// </summary>
        /// <param name="id_obj">Идентификатор типа библиотеки</param>
        /// <param name="key">Идентификатор элемента(??? экземпляр подкласса) в объекте библиотеки</param>
        /// <param name="val">Признак использования</param>
        public void SetDataHostMark (int id_obj, int key, bool val)
        {
            KeyValuePair<int, int> pair;

            if (Monitor.TryEnter (this) == true) {
                pair = new KeyValuePair<int, int> (id_obj, key);

                if (_dictDataHostCounter.ContainsKey (pair) == true) {
                    if (val == true)
                        _dictDataHostCounter [pair]++;
                    else
                        if (val == false)
                        _dictDataHostCounter [pair]--;
                    else
                        ; // недостижимый код

                    //Console.WriteLine(@"PlugInULoader::SetMark (id=" + id_obj + @", key=" + key + @", val=" + val + @") - counter=" + m_dictDataHostCounter[pair] + @" ...");
                } else
                    ;
            } else
                ;
        }

        /// <summary>
        /// Возвратить признак использования элемента в объекте библиотеки
        /// </summary>
        /// <param name="id_main">Идентификтор типа объекта библиотеки</param>
        /// <param name="id_detail">Идентификатор элемента в объекте библиотеки</param>
        /// <returns>Признак использования элемента</returns>
        protected bool isDataHostMarked(int id_main, int id_detail)
        {
            KeyValuePair<int, int> pair = new KeyValuePair<int, int>(id_main, id_detail);

            return (_dictDataHostCounter.ContainsKey(pair) == true)
                && (_dictDataHostCounter[pair] % 2 == 1);
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

    public abstract class HPlugIns : Dictionary<int, PlugInBase>, IPlugInHost
    //, IEnumerable <IPlugIn>
    {
        /// <summary>
        /// Перечисление состояний библиотеки
        /// </summary>
        public enum STATE_DLL { UNKNOWN = -3, NOT_LOAD, TYPE_MISMATCH, LOADED, }
#if _SEPARATE_APPDOMAIN
        //http://stackoverflow.com/questions/658498/how-to-load-an-assembly-to-appdomain-with-all-references-recursively
        //http://lsd.luminis.eu/load-and-unload-assembly-in-appdomains/
        //http://www.codeproject.com/Articles/453778/Loading-Assemblies-from-Anywhere-into-a-New-AppDom
        private class ProxyAppDomain : MarshalByRefObject
        {
            public Assembly GetAssembly(string AssemblyPath)
            {
                try
                {
                    return Assembly.LoadFrom(AssemblyPath);
                    //If you want to do anything further to that assembly, you need to do it here.
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// Домен для загрузки плюгИнов
        /// </summary>
        private AppDomain m_appDomain;
        /// <summary>
        /// Домен-посредник для загрузки плюгИнов
        /// </summary>
        private ProxyAppDomain m_proxyAppDomain;
        /// <summary>
        /// Объект с параметрами безопасности для создания домена (для загрузки плюгИнов)
        /// </summary>
        private static System.Security.Policy.Evidence s_domEvidence = AppDomain.CurrentDomain.Evidence;
        /// <summary>
        /// Объект с параметрами среды окружения для создания домена (для загрузки плюгИнов)
        /// </summary>
        private static AppDomainSetup s_domSetup = new AppDomainSetup();
#endif
        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        /// <param name="fClickMenuItem">Делегат обработки сообщения - ваыбор п. меню</param>
        public HPlugIns()
        {
#if _SEPARATE_APPDOMAIN
            s_domSetup = new AppDomainSetup();
            s_domSetup.ApplicationBase = System.Environment.CurrentDirectory;
            s_domEvidence = AppDomain.CurrentDomain.Evidence;
#else
#endif
        }
        /// <summary>
        /// Установить взамосвязь
        /// </summary>
        /// <param name="plug">Загружаемый плюгИн</param>
        /// <returns>Признак успешности загрузки</returns>
        public int Register(IPlugIn plug)
        {
            //??? важная функция для взимного обмена сообщенями
            return 0;
        }
#if _SEPARATE_APPDOMAIN
        /// <summary>
        /// Признак инициализации домена для загрузки в него плюгИнов
        /// </summary>
        protected bool isInitPluginAppDomain { get { return (!(m_appDomain == null)) && (!(m_proxyAppDomain == null)); } }
        /// <summary>
        /// Инициализация домена для загрузки в него плюгИнов
        /// </summary>
        private void initPluginDomain(string name)
        {
            m_appDomain = AppDomain.CreateDomain("PlugInAppDomain::" + name, s_domEvidence, s_domSetup);
            m_appDomain.UnhandledException += new UnhandledExceptionEventHandler(ProgramBase.SeparateAppDomain_UnhandledException);

            Type type = typeof(ProxyAppDomain);
            m_proxyAppDomain = (ProxyAppDomain)m_appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
        }
#endif
        /// <summary>
        /// Выгрузить из памяти загруженные плюгИны
        /// </summary>
        public void UnloadPlugIn()
        {
#if _SEPARATE_APPDOMAIN
            if (isInitPluginAppDomain == true)
            {
                m_appDomain.UnhandledException -= ProgramBase.SeparateAppDomain_UnhandledException;
                AppDomain.Unload(m_appDomain);

                m_appDomain = null;
                m_proxyAppDomain = null;
            }
            else
                ;
#endif
            Clear();
        }
        /// <summary>
        /// Загрузить плюгИн с указанным наименованием
        /// </summary>
        /// <param name="name">Наименование плюгИна</param>
        /// <param name="iRes">Результат загрузки (код ошибки)</param>
        /// <returns>Загруженный плюгИн</returns>
        protected PlugInBase load(string name, out int iRes)
        {
            PlugInBase plugInRes = null;
            Assembly ass = null;
            iRes = -1;

            Type objType = null;
            try
            {
#if _SEPARATE_APPDOMAIN
                if (isInitPluginAppDomain == false)
                    initPluginDomain(name);
                else
                    ;
#endif
                ass =
#if _SEPARATE_APPDOMAIN
                    m_proxyAppDomain.GetAssembly
#else
                    Assembly.LoadFrom
#endif
                        (Environment.CurrentDirectory + @"\" + name + @".dll");

                if (!(ass == null))
                {
                    objType = ass.GetType(name + ".PlugIn");
                }
                else
                    ;
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, @"FormMain::loadPlugin () ... LoadFrom () ... plugIn.Name = " + name, Logging.INDEX_MESSAGE.NOT_SET);
            }

            if (!(objType == null))
                try
                {
                    plugInRes = ((PlugInBase)Activator.CreateInstance(objType));
                    plugInRes.Host = (IPlugInHost)this; //Вызов 'Register'

                    iRes = 0;
                }
                catch (Exception e)
                {
                    iRes = -2;

                    Logging.Logg().Exception(e, @"FormMain::loadPlugin () ... CreateInstance ... plugIn.Name = " + name, Logging.INDEX_MESSAGE.NOT_SET);
                }
            else
                Logging.Logg().Error(@"FormMain::loadPlugin () ... Assembly.GetType()=null ... plugIn.Name = " + name, Logging.INDEX_MESSAGE.NOT_SET);

            return plugInRes;
        }
    }
}
