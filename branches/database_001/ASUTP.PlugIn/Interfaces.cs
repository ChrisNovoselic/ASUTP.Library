using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASUTP.PlugIn
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
        public EventArgsDataHost (int id_main_, int id_detail_, object [] arObj)
        {
            id_main = id_main_;
            id_detail = id_detail_;
            reciever = null;
            par = new object [arObj.Length];
            arObj.CopyTo (par, 0);
        }
        /// <summary>
        /// Конструктор - дополнительный (с параметрами)
        /// </summary>
        /// <param name="objReciever">Объект-клиент, получатель запрашиваемых данных</param>
        /// <param name="arObj">Массив аргументов сообщения</param>
        public EventArgsDataHost (IDataHost objReciever, object [] arObj)
        {
            id_detail = -1;
            reciever = objReciever;
            par = new object [arObj.Length];
            arObj.CopyTo (par, 0);
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
    public enum ID_HEAD_ASKED_HOST {
        UNKNOWN = -1
            , GET, CONFIRM
    }

    public interface IPlugIn {
        IPlugInHost Host
        {
            get; set;
        }

        object GetObject (int key);

        Type GetTypeObject (int key);

        int GetKeyType (Type type);

        int GetKeyType (string typeName);
    }

    public interface IDataHost {
        /// <summary>
        /// Событие запроса данных для плюг'ина из главной формы
        /// </summary>
        event DelegateObjectFunc EvtDataAskedHost;
        /// <summary>
        /// Отиравить запрос на получение данных
        /// </summary>
        /// <param name="par">Аргумент с детализацией запрашиваемых данных</param>
        void DataAskedHost (object par);
        /// <summary>
        /// Обработчик события ответа от главной формы
        /// </summary>
        /// <param name="obj">объект класса 'EventArgsDataHost' с идентификатором/данными из главной формы</param>
        void OnEvtDataRecievedHost (object res);
    }

    /// <summary>
    /// Интерфейс для контейнера 'плюгинов'
    /// </summary>
    public interface IPlugInHost {
        /// <summary>
        /// Регистрировать 'плюгин'
        /// </summary>
        /// <param name="plug">Регистрируемый 'плюгин'</param>
        /// <returns>Результат регистрации</returns>
        int Register (IPlugIn plug);
    }
}
