using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASUTP.Core {
    public class Constants {
        /// <summary>
        /// Максимальное количество повторов
        /// </summary>
        public static volatile int MAX_RETRY = 3;
        /// <summary>
        /// Количество попыток проверки наличия результата в одном цикле
        /// </summary>
        public static volatile int MAX_WAIT_COUNT = 39;
        /// <summary>
        /// Интервал ожидания между проверками наличия результата
        ///  , при условии что в предыдущей итерации результат не был получен
        /// </summary>
        public static volatile int WAIT_TIME_MS = 106;
        /// <summary>
        /// Максимальное время ожидания окончания длительно выполняющейся операции
        /// </summary>
        public static int MAX_WATING
        {
            get
            {
                return MAX_RETRY * MAX_WAIT_COUNT * WAIT_TIME_MS;
                //return 6666;
            }
        }
    }
}
