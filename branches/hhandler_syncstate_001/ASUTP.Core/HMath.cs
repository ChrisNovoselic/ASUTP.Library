using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASUTP.Core {
    public static class HMath {
        public static double doubleParse (string valIn)
        {
            double valOut = double.NaN;
            int iPartInt = Int32.MinValue, iPartFract = Int32.MinValue;
            string valPart = string.Empty;

            valIn = valIn.Trim ();

            int i = 0;
            while (i < valIn.Length) {
                if (Char.IsDigit (valIn [i]) == false)
                    break;
                else
                    ;

                valPart += valIn [i];
                i++;
            }

            if (valPart.Length > 0)
                iPartInt = Int32.Parse (valPart);
            else
                ;
            valPart = string.Empty;

            i++;

            while (i < valIn.Length) {
                if (Char.IsDigit (valIn [i]) == false)
                    break;
                else
                    ;

                valPart += valIn [i];
                i++;
            }

            if (valPart.Length > 0)
                iPartFract = Int32.Parse (valPart);
            else
                ; //???Проверка на количество "нецифровых" символов...

            if (!(iPartInt == Int32.MinValue))
                valOut = iPartInt;
            else
                ;

            if (!(iPartFract == Int32.MinValue))
                if (!(iPartInt == Int32.MinValue))
                    valOut += iPartFract / Math.Pow (10, valPart.Length);
                else
                    valOut = iPartFract / Math.Pow (10, valPart.Length);
            else
                ;

            return valOut;
        }

        public static void doubleParse (string valIn, out double valOut)
        {
            try {
                //valOut = double.Parse(valIn, ProgramBase.ss_MainCultureInfo);
                valOut = double.Parse (valIn, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowThousands);
            } catch (Exception e) {
                valOut = doubleParse (valIn);

                if (valOut == double.NaN)
                    throw new Exception (@"HMath::doubleParse () - вход = " + valIn, e);
                else
                    ;
            }
        }
        /// <summary>
        /// Возвести число в степень 2 
        /// </summary>
        /// <param name="number">Число, возводимое в степень 2</param>
        /// <param name="offset">Слагаемое к числу, возводимого в степень (используется для битов)</param>
        /// <returns>Значение числа в степени 2</returns>
        public static Int32 Pow2 (int number, int offset = 0)
        {
            return (Int32)Math.Pow (2, number + offset);
        }
        /// <summary>
        /// Function to get random number
        /// </summary>
        private static readonly Random random = new Random ();
        /// <summary>
        /// Объект синхронизации для получения случайного целочисленного значения в диапазоне
        /// </summary>
        private static readonly object syncLock = new object ();
        /// <summary>
        /// Возвраить случайное целочисленное значение в диапазоне
        /// </summary>
        /// <param name="min">Нижняя (левая) граница диапазона</param>
        /// <param name="max">Верхняя (правая) граница диапазона</param>
        /// <returns>Случайное целочисленное значение</returns>
        public static int GetRandomNumber (int min = 1, int max = Int32.MaxValue)
        {
            lock (syncLock) { // synchronize
                return random.Next (min, max);
            }
        }
    }
}
