using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ASUTP.PlugIn {

    public abstract class HPlugIns : Dictionary<int, PlugInBase>, IPlugInHost
    //, IEnumerable <IPlugIn>
    {
        /// <summary>
        /// Перечисление состояний библиотеки
        /// </summary>
        public enum STATE_DLL {
            UNKNOWN = -3, NOT_LOAD, TYPE_MISMATCH, LOADED,
        }
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
        public HPlugIns ()
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
        public int Register (IPlugIn plug)
        {
            //??? важная функция для взимного обмена сообщенями
            return 0;
        }
#if _SEPARATE_APPDOMAIN
        private UnhandledExceptionEventHandler fSeparateAppDomain_UnhandledException;

        /// <summary>
        /// Признак инициализации домена для загрузки в него плюгИнов
        /// </summary>
        protected bool isInitPluginAppDomain { get { return (!(m_appDomain == null)) && (!(m_proxyAppDomain == null)); } }
        /// <summary>
        /// Инициализация домена для загрузки в него плюгИнов
        /// </summary>
        /// <param name="name">Наименование плюгИна</param>
        /// <param name="delegateSeparateAppDomain_UnhandledException">Обработчик необработанных секциями try/catch исключений</param>
        private void initPluginDomain(string name, UnhandledExceptionEventHandler delegateSeparateAppDomain_UnhandledException)
        {
            fSeparateAppDomain_UnhandledException = delegateSeparateAppDomain_UnhandledException;

            m_appDomain = AppDomain.CreateDomain("PlugInAppDomain::" + name, s_domEvidence, s_domSetup);
            m_appDomain.UnhandledException += new UnhandledExceptionEventHandler(fSeparateAppDomain_UnhandledException);

            Type type = typeof(ProxyAppDomain);
            m_proxyAppDomain = (ProxyAppDomain)m_appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
        }
#endif
        /// <summary>
        /// Выгрузить из памяти загруженные плюгИны
        /// </summary>
        public void UnloadPlugIn ()
        {
#if _SEPARATE_APPDOMAIN
            if (isInitPluginAppDomain == true)
            {
                m_appDomain.UnhandledException -= new UnhandledExceptionEventHandler (fSeparateAppDomain_UnhandledException);
                AppDomain.Unload(m_appDomain);

                m_appDomain = null;
                m_proxyAppDomain = null;
            }
            else
                ;
#endif
            Clear ();
        }
        /// <summary>
        /// Загрузить плюгИн с указанным наименованием
        /// </summary>
        /// <param name="name">Наименование плюгИна</param>
#if _SEPARATE_APPDOMAIN
        /// <param name="delegateSeparateAppDomain_UnhandledException">Обработчик необработанных секциями try/catch исключений</param>
#endif
        /// <param name="iRes">Результат загрузки (код ошибки)</param>
        /// <returns>Загруженный плюгИн</returns>
        protected PlugInBase load (string name
#if _SEPARATE_APPDOMAIN
            , UnhandledExceptionEventHandler delegateSeparateAppDomain_UnhandledException
#endif
            , out int iRes)
        {
            PlugInBase plugInRes = null;
            Assembly ass = null;
            iRes = -1;

            Type objType = null;
            try {
#if _SEPARATE_APPDOMAIN
                if (isInitPluginAppDomain == false)
                    initPluginDomain(name, delegateSeparateAppDomain_UnhandledException);
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

                if (!(ass == null)) {
                    objType = ass.GetType (name + ".PlugIn");
                } else
                    ;
            } catch (Exception e) {
                Logging.Logg ().Exception (e, @"FormMain::loadPlugin () ... LoadFrom () ... plugIn.Name = " + name, Logging.INDEX_MESSAGE.NOT_SET);
            }

            if (!(objType == null))
                try {
                    plugInRes = ((PlugInBase)Activator.CreateInstance (objType));
                    plugInRes.Host = (IPlugInHost)this; //Вызов 'Register'

                    iRes = 0;
                } catch (Exception e) {
                    iRes = -2;

                    Logging.Logg ().Exception (e, @"FormMain::loadPlugin () ... CreateInstance ... plugIn.Name = " + name, Logging.INDEX_MESSAGE.NOT_SET);
                } else
                Logging.Logg ().Error (@"FormMain::loadPlugin () ... Assembly.GetType()=null ... plugIn.Name = " + name, Logging.INDEX_MESSAGE.NOT_SET);

            return plugInRes;
        }
    }
}
