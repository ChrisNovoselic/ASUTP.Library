using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ASUTP.Core
{
    /// <summary>
    /// Библиотека функций WinApi
    /// </summary>
    static public class WinApi
    {
        /// <summary>
        /// Служит для генерации сообщения WM_SYSCOMMAND
        /// </summary>
        public const int WM_SYSCOMMAND = 0x0112;
        /// <summary>
        /// Constant value was found in the "windows.h" header file.
        /// </summary>
        public const int WM_ACTIVATEAPP = 0x001C;
        /// <summary>
        /// Для использования как wParam при WM_SYSCOMMAND. Отправляет приложению сообщение, что ПОЛЬЗОВТЕЛЬ захотел закрыть окно
        /// </summary>
        public const int SC_CLOSE = 0xF060; // close the window
        /// <summary>
        /// Для использования как wParam при WM_SYSCOMMAND. Отправляет приложению сообщение,
        /// что ПОЛЬЗОВТЕЛЬ захотел восстановить нормальный размер окна
        /// </summary>
        public const int SC_RESTORE = 0xF120;
        /// <summary>
        /// Служит для закрытия приложения от имени TaskManager
        /// </summary>
        public const int WM_CLOSE = 0x0010;
        /// <summary>
        /// Перечисление всех окон
        /// </summary>
        /// <param name="lpEnumFunc"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProcDel lpEnumFunc, IntPtr lParam);
        /// <summary>
        /// Тип для делегата при использовании в функции поиска
        /// </summary>
        /// <param name="hWnd">Дескриптор окна</param>
        /// <param name="lParam">Аргумент 1</param>
        /// <returns>Результат выполнения</returns>
        public delegate bool EnumWindowsProcDel(IntPtr hWnd, IntPtr lParam);
        /// <summary>
        /// Определяет свернуто ли приложение
        /// </summary>
        /// <param name="hWnd">дескриптор окна</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int IsIconic(IntPtr hWnd);
        /// <summary>
        /// Получает заголовок окна
        /// </summary>
        /// <param name="hWnd">дескриптор окна</param>
        /// <param name="lpString"></param>
        /// <param name="nMaxCount"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        /// <summary>
        /// получает размер заголовка окна
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        /// <summary>
        /// Поиск дочерних окон
        /// </summary>
        /// <param name="parentHandle">имя родителя</param>
        /// <param name="childAfter"></param>
        /// <param name="lclassName"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        public const int HWND_BROADCAST = 0xffff;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWUNTRAY = 3;
        public const int SW_RESTORE = 9;
        /// <summary>
        /// Посылает сообщение оконной функции указанного окна
        /// </summary>
        /// <param name="hWnd">дескриптор окна</param>
        /// <param name="Msg">сообщение</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern bool SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// Отправить сообщение окну
        /// </summary>
        /// <param name="hWnd">Дескриптор окна</param>
        /// <param name="Msg">Сообщение (идентификатор)</param>
        /// <param name="wParam">Аргумент 1</param>
        /// <param name="lParam">Аргумент 2</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// Отправить сообщение потоку
        /// </summary>
        /// <param name="threadId">Тдентификатор потока</param>
        /// <param name="msg">Сообщение (идентификатор)</param>
        /// <param name="wParam">Аргумент 1</param>
        /// <param name="lParam">Аргумент 2</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// Получение ид потока, который создал окно
        /// </summary>
        /// <param name="hWnd">дескриптор окна</param>
        /// <param name="lpdwProcessId">номер ид</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId); 
        /// <summary>
        /// Найти дескриптор окна
        /// </summary>
        /// <param name="ClassName"></param>
        /// <param name="WindowName"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string ClassName, string WindowName);
        /// <summary>
        /// Разворачивает окно
        /// </summary>
        /// <param name="hWnd">дескриптор окна</param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImportAttribute("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        /// <summary>
        /// Выводит поверх всех окон приложение
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);
        /// <summary>
        /// Проверка видимости окна
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        public static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }
        /// <summary>
        /// Получает данные о состоянии окна (WindowState)
        /// </summary>
        /// <param name="hWnd">дескриптор окна</param>
        /// <param name="lpwndpl">состояние окна</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }
    }
}
