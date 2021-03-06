﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Runtime.Remoting.Messaging;

using System.Windows.Forms; //Control

namespace HClassLibrary
{
    public abstract class PlugInMenuItem : PlugInBase
    {
        public int _IdTask;

        private struct MenuItem
        {
            public string _nameOwner;

            public string _name;
        }

        private Dictionary<int, MenuItem> m_Items;

        //public class PlugInMenuItemEventArgs : EventArgs
        //{
        //    public int m_key;

        //    public PlugInMenuItemEventArgs(int key)
        //    {
        //        m_key = key;
        //    }
        //}

        //public delegate void PlugInMenuItemEventHandler(object obj, PlugInMenuItemEventArgs ev);

        public PlugInMenuItem() : base ()
        {
            m_Items = new Dictionary<int, MenuItem>();
        }
        /// <summary>
        /// Зарегистрировать тип объекта в библиотеке, для отображения при обработке п. меню
        /// </summary>
        /// <param name="key">Идентификатор типа (панели)</param>
        /// <param name="type">Тип панели</param>
        /// <param name="nameMenuItemOwner">Текст родительского п. мен.</param>
        /// <param name="nameMenuItem">Текст п. меню</param>
        protected void register(int key, Type type, string nameMenuItemOwner, string nameMenuItem)
        {
            m_Items.Add(key, new MenuItem() { _nameOwner = nameMenuItemOwner, _name = nameMenuItem });

            registerType (key, type);
        }

        public string GetNameOwnerMenuItem(int key)
        {
            string strRes = string.Empty;

            try
            {
                strRes = m_Items[key]._nameOwner;
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, @"PluginMenuItem::GetNameMenuItem (key=" + key + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);
            }

            return strRes;
        }

        public string GetNameMenuItem(int key)
        {
            string strRes = string.Empty;

            try
            {
                strRes = m_Items[key]._name;
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, @"PluginMenuItem::GetNameMenuItem (key=" + key + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);
            }

            return strRes;
        }

        public List<string> GetNameMenuItems()
        {
            List<string> listRes = new List<string>();

            foreach (MenuItem item in m_Items.Values)
                listRes.Add (item._name);

            return listRes;
        }

        /// <summary>
        /// Обработчик выбора пункта меню для плюг'ина
        /// </summary>
        /// <param name="obj">объект-инициатор события</param>
        /// <param name="ev">параметры события</param>
        public virtual void OnClickMenuItem(object obj, /*PlugInMenuItem*/EventArgs ev)
        {
            int id = (int)(obj as ToolStripMenuItem).Tag; //_Id;
            
            createObject(id);
        }
    }
}
