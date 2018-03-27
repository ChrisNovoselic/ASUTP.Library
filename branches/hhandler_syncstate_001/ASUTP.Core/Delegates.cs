using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASUTP.Core
{
    /// <summary>
    /// Тип для делегата без аргументов и без возвращаемого значения
    /// </summary>
    public delegate void DelegateFunc ();
    /// <summary>
    /// Тип для делегата с аргументом типа 'int' и без возвращаемого значения
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    public delegate void DelegateIntFunc (int param);
    /// <summary>
    /// Тип для делегата с аргументами типа 'int', 'int' и без возвращаемого значения
    /// </summary>
    /// <param name="param1">Аргумент 1</param>
    /// <param name="param2">Аргумент 2</param>
    public delegate void DelegateIntIntFunc (int param1, int param2);
    /// <summary>
    /// Тип для делегата с аргументами типа 'int', 'int' с возвращаемым значением типа 'int'
    /// </summary>
    /// <param name="param1">Аргумент 1</param>
    /// <param name="param2">Аргумент 2</param>
    /// <returns>Результат выполнения</returns>
    public delegate int IntDelegateIntIntFunc (int param1, int param2);
    /// <summary>
    /// Тип для делегата с аргументом типа 'string' и без возвращаемого значения
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    public delegate void DelegateStringFunc (string param);
    /// <summary>
    /// Тип для делегата с аргументом типа 'bool' и без возвращаемого значения
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    public delegate void DelegateBoolFunc (bool param);
    /// <summary>
    /// Тип для делегата с аргументом типа 'object' и без возвращаемого значения
    /// </summary>
    /// <param name="obj">Аргумент 1</param>
    public delegate void DelegateObjectFunc (object obj);
    /// <summary>
    /// Тип для делегата с аргументом типа 'ссылка на object' с и без возвращаемого значения
    /// </summary>
    /// <param name="obj">Аргумент 1</param>
    public delegate void DelegateRefObjectFunc (ref object obj);
    /// <summary>
    /// Тип для делегата с аргументом типа 'DateTime' с и без возвращаемого значения
    /// </summary>
    /// <param name="date">Аргумент 1</param>
    public delegate void DelegateDateFunc (DateTime date);
    /// <summary>
    /// Тип для делегата с аргументом типа 'DateTime' и без возвращаемого значения
    /// </summary>
    /// <returns>Результат выполнения</returns>
    public delegate int IntDelegateFunc ();
    /// <summary>
    /// Тип для делегата с аргументом типа 'int' с возвращаемым значения типа 'int'
    /// </summary>
    /// <param name="param">>Аргумент 1</param>
    /// <returns>Результат выполнения</returns>
    public delegate int IntDelegateIntFunc (int param);
    /// <summary>
    /// Тип для делегата без аргументов с возвращаемым значения типа 'string'
    /// </summary>
    /// <returns>Результат выполнения</returns>
    public delegate string StringDelegateFunc ();
    /// <summary>
    /// Тип для делегата с аргументом типа 'int' с возвращаемым значения типа 'string'
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    /// <returns>Результат выполнения</returns>
    public delegate string StringDelegateIntFunc (int param);
    /// <summary>
    /// Тип для делегата с аргументом типа 'string' с возвращаемым значения типа 'string'
    /// </summary>
    /// <param name="keyParam">Аргумент 1</param>
    /// <returns>Результат выполнения</returns>
    public delegate string StringDelegateStringFunc (string keyParam);
}
