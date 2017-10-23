using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASUTP.Core {
    /// <summary>
    /// Класс для хранения массива признаков
    /// Значение каждого признака хранится в одном из битов (адреса) объекта
    /// </summary>
    public class HMark {
        /// <summary>
        /// Совокупность значений признаков
        /// </summary>
        private Int32 m_mark;

        public DelegateIntFunc ValueChanged;
        /// <summary>
        /// Конструктор объекта
        /// </summary>
        /// <param name="val">Совокупность значений признаков</param>
        public HMark (int val)
        {
            m_mark = val;
        }
        /// <summary>
        /// Конструктор объекта
        /// </summary>
        /// <param name="arNumBits">Индексы (номера)</param>
        public HMark (/*params */int [] arNumBits)
        {
            foreach (int bit in arNumBits)
                marked (true, bit);
        }
        /// <summary>
        /// Присвоить признак по указанному адресу (номеру бита)
        /// </summary>
        /// <param name="opt">Значение признака</param>
        /// <param name="bit">Номер бита</param>
        private void marked (bool opt, int bit)
        {
            Int32 val = HMath.Pow2 (bit);

            if (opt == true)
                if (!((m_mark & val) == val)) {
                    m_mark += val;
                    ValueChanged?.Invoke (val);
                } else
                    ;
            else
                if ((m_mark & val) == val) {
                m_mark -= val;
                ValueChanged?.Invoke (val);
            } else
                ;
        }
        /// <summary>
        /// Установить значение признака по указанному адресу
        /// </summary>
        /// <param name="bit">Номер бита</param>
        /// <param name="val">Значение признака</param>
        public void Set (int bit, bool val)
        {
            marked (val, bit);
        }
        /// <summary>
        /// Установить значение всех признаков
        /// </summary>
        /// <param name="mark">Исходное значение для всех признаков</param>
        public void SetOf (HMark mark)
        {
            //for (int i = 0; i < sizeof (Int32) * 8; i ++)
            //    marked (IsMarked (i), i);
            m_mark = mark.Value;
        }
        /// <summary>
        /// Установить значение всех признаков
        /// </summary>
        /// <param name="val">Исходное значение для всех признаков</param>
        public void SetOf (int val)
        {
            m_mark = val;
        }
        /// <summary>
        /// Добавить истинные признаки при их отсутствии
        /// </summary>
        /// <param name="mark">Исходное значение для всех признаков</param>
        public void Add (HMark mark)
        {
            int cntBit = -1
                , valChanged = 0;

            Delegate [] arHandler = ValueChanged == null ? new Delegate [] { } : ValueChanged.GetInvocationList ();

            foreach (DelegateIntFunc f in arHandler)
                ValueChanged -= f;

            cntBit = sizeof (Int32) * 8;

            for (int i = 0; i < cntBit; i++) {
                if ((IsMarked (mark.Value, i) == true)
                    && (IsMarked (i) == false)) {
                    marked (true, i);

                    valChanged += HMath.Pow2 (i);
                } else
                    ;
            }

            foreach (DelegateIntFunc f in arHandler)
                ValueChanged += new DelegateIntFunc (f);

            ValueChanged?.Invoke (valChanged);
        }
        /// <summary>
        /// Установить значение признака ИСТИНА по адресу (номеру бита)
        /// </summary>
        /// <param name="bit">Номер бита</param>
        public void Marked (int bit)
        {
            marked (true, bit);
        }
        /// <summary>
        /// Установить все признаки в ЛОЖЬ
        /// </summary>
        public void UnMarked ()
        {
            m_mark = 0;
        }
        /// <summary>
        /// Установить значение признака в ЛОЖЬ по адресу (номеру бита)
        /// </summary>
        /// <param name="bit">Номер бита</param>
        public void UnMarked (int bit)
        {
            marked (false, bit);
        }
        /// <summary>
        /// Проверить установлено ли значение признака в ИСТИНА по адресу (номеру бита)
        /// </summary>
        /// <param name="bit">Номер бита</param>
        /// <returns>Признак установки значения</returns>
        public bool IsMarked (int bit)
        {
            return IsMarked (m_mark, bit);
        }
        /// <summary>
        /// Проверить установлен ли хотя бы один признак в ИСТИНА
        /// </summary>
        /// <returns>Признак установки значения</returns>
        public bool IsMarked ()
        {
            bool bRes = false;
            for (int iBit = 0; iBit < sizeof (Int32); iBit++)
                if ((bRes = IsMarked (m_mark, iBit)) == true)
                    break;
                else
                    ;

            return bRes;
        }
        /// <summary>
        /// Проверить установлено ли значение признака в ИСТИНА по адресу (номеру бита) и доп./смещению
        /// </summary>
        /// <param name="val">Все значения признаков</param>
        /// <param name="bit">Номер бита</param>
        /// <param name="offset">Доп./смещение</param>
        /// <returns>Признак установки значения</returns>
        public static bool IsMarked (int val, int bit, int offset = 0)
        {
            bool bRes = false;

            if ((val & HMath.Pow2 (bit, offset)) == HMath.Pow2 (bit, offset)) {
                bRes = true;
            } else
                ;

            return bRes;
        }
        /// <summary>
        /// Возвратить значение всех признаков в виде целого числа
        /// </summary>
        public Int32 Value
        {
            get
            {
                return m_mark;
            }
        }
    }
}
