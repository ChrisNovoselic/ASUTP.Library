﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASUTP.PlugIn {
    public abstract class PlugInBase : HDataHost, IPlugIn {
        /// <summary>
        /// Объект интерфейса подписчика
        /// </summary>
        IPlugInHost _host;
        /// <summary>
        /// Массив зарегистрированных типов плюгИна
        /// </summary>
        protected Dictionary<int, Type> _types;
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
        private Dictionary<KeyValuePair<int, int>, uint> _dictDataHostCounter;

        public IPlugInHost Host
        {
            get
            {
                return _host;
            }
            set
            {
                _host = value;
                _host.Register (this);
            }
        }

        public PlugInBase ()
            : base ()
        {
            _dictDataHostCounter = new Dictionary<KeyValuePair<int, int>, uint> ();
            _types = new Dictionary<int, Type> ();
            _objects = new Dictionary<int, object> ();
        }

        /// <summary>
        /// Зарегистрировать тип объекта библиотеки
        /// </summary>
        /// <param name="key">Ключ регистрируемого типа объекиа</param>
        /// <param name="type">Регистрируемый тип</param>
        protected virtual void registerType (int key, Type type)
        {
            _types.Add (key, type);
        }

        /// <summary>
        /// Возвратить список наименований зарегистрированных типов библиотек
        /// </summary>
        /// <returns></returns>
        public List<string> GetListRegisterNameTypes ()
        {
            List<string> listRes = new List<string> ();

            return _types.Values.Select (type => {
                return type.GetType ().Name;
            }).ToList ();
        }

        /// <summary>
        /// Возвратить словарь зарегистрированных типов библиотек
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Type> GetRegisterTypes ()
        {
            return _types;
        }

        /// <summary>
        /// Возвратить признак регистрации типа библиотеки по ее идентификатору
        /// </summary>
        /// <param name="key">Ключ-идентификатор типа библиотеки</param>
        /// <returns>Признак регистрации типа библиотеки</returns>
        public bool IsRegistred (int key)
        {
            return _types.Keys.Contains (key);
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

            if (_objects.ContainsKey (key) == false) {
                try {
                    _objects.Add (key, Activator.CreateInstance (_types [key], this));

                    if (_objects [key] is System.Windows.Forms.Control) {
                        //if (_object is HPanelCommon)
                        //    (_object as HPanelCommon).Start();
                        //else
                        //    ;

                        //((Control)_object).HandleCreated += new EventHandler(plugInObject_HandleCreated);
                        //((Control)_object).HandleDestroyed += new EventHandler(plugInObject_HandleDestroyed);
                    } else
                        ;

                    iRes = 1; //Объект только создан
                } catch (Exception e) {
                    iRes = -1; //Ошибка при создании объекта

                    Logging.Logg ().Exception (e, @"PlugInBase::createObject (ID=" + _Id + @", key=" + key + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);
                }


            } else {
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
            return _objects [key];
        }
        /// <summary>
        /// Возвратить тип объекта 'плюгина'
        /// </summary>
        public Type GetTypeObject (int key)
        {
            return ((_types.ContainsKey (key) == false) ? Type.EmptyTypes [0] : _types [key]);
        }
        /// <summary>
        /// Возвратить идентификатор одного из зарегистрированных типов объекта 'плюгина'
        /// </summary>
        public int GetKeyType (Type type)
        {
            int iRes = -1;

            foreach (KeyValuePair<int, Type> pair in _types)
                if (pair.Value.Name == type.Name) {
                    iRes = pair.Key;

                    break;
                } else
                    ;

            return iRes;
        }
        /// <summary>
        /// Возвратить идентификатор одного из зарегистрированных типов объекта 'плюгина'
        /// </summary>
        public int GetKeyType (string typeName)
        {
            int iRes = -1;

            foreach (KeyValuePair<int, Type> pair in _types)
                if (pair.Value.Name == typeName) {
                    iRes = pair.Key;

                    break;
                } else
                    ;

            return iRes;
        }
        /// <summary>
        /// Отправить данные получателю (подписчику)
        /// </summary>
        /// <param name="par">Объект с передаваемыми данными (может быть массивом объектов)</param>
        public override void DataAskedHost (object par)
        {
            base.DataAskedHost (new object [] { _Id, par });
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
        public override void OnEvtDataRecievedHost (object obj)
        {
            SetDataHostMark (((EventArgsDataHost)obj).id_main, ((EventArgsDataHost)obj).id_detail, true);

            //Console.WriteLine(@"PlugInBase::OnEvtDataRecievedHost (id=" + pair.Key + @", key=" + pair.Value + @") - counter=" + m_dictDataHostCounter[pair]);
        }

        public void SetDataHostMark (KeyValuePair<int, int> pair, bool val)
        {
            if (Monitor.TryEnter (this) == true) {
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
                    _dictDataHostCounter.Add (pair, (uint)((val == true) ? 1 : 0));

                Monitor.Exit (this);
            } else
                ;
        }

        /// <summary>
        /// Установить принудительно признак использования элемента в объекте библиотеки
        /// </summary>
        /// <param name="id_obj">Идентификатор типа библиотеки</param>
        /// <param name="key">Идентификатор элемента(??? экземпляр подкласса) в объекте библиотеки</param>
        /// <param name="val">Признак использования</param>
        public void SetDataHostMark (int id_obj, int key, bool val)
        {
            SetDataHostMark (new KeyValuePair<int, int> (id_obj, key), val);
        }

        protected bool isDataHostMarked (KeyValuePair<int, int> pair)
        {
            return (_dictDataHostCounter.ContainsKey (pair) == true)
                && (_dictDataHostCounter [pair] % 2 == 1);
        }

        /// <summary>
        /// Возвратить признак использования элемента в объекте библиотеки
        /// </summary>
        /// <param name="id_main">Идентификтор типа объекта библиотеки</param>
        /// <param name="id_detail">Идентификатор элемента в объекте библиотеки</param>
        /// <returns>Признак использования элемента</returns>
        protected bool isDataHostMarked (int id_main, int id_detail)
        {
            return isDataHostMarked(new KeyValuePair<int, int> (id_main, id_detail));
        }
    }
}
