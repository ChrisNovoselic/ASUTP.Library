using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ASUTP.Helper
{
    /// <summary>
    /// Интерфейс для базовой формы главного окна приложения
    /// </summary>
    public interface IFormMainBase
    {
        /// <summary>
        /// Закрыть окно
        /// </summary>
        /// <param name="bForce">Признак принудительного</param>
        void Close (bool bForce);
    }

    /// <summary>
    /// Интерфейс для формы, индицирующей выполнение длительной операции
    /// </summary>
    public interface IFormWait
    {
        /// <summary>
        /// Вызвать на отображение окно
        /// </summary>
        /// <param name="ptParent">Позиция отображения родительского окна</param>
        /// <param name="szParent">Размер родительского окна</param>
        void StartWaitForm (Point ptParent, Size szParent);

        /// <summary>
        /// Снять с отображения окно
        /// </summary>
        /// <param name="bExit"></param>
        void StopWaitForm (bool bExit = false);
    }
}
