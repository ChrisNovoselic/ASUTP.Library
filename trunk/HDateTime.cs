using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HClassLibrary
{
    public class HDateTime
    {
        /// <summary>
        /// Перечисление - идентификаторы периодов времени
        /// </summary>
        public enum INTERVAL : short { UNKNOWN = -1, MINUTES, HOURS, COUNT_ID_TIME }
        /// <summary>
        /// Массив со строковыми наименованиями месяцев в году
        /// </summary>
        public readonly static string[] NameMonths = { @"январь", @"февраль", @"март"
            , @"апрель", @"май", @"июнь"
            , @"июль", @"август", @"сентябрь"
            , @"октябрь", @"ноябрь", @"декабрь" };
        /// <summary>
        /// Наименование в ОС для зоны "Москва - стандартное время РФ"
        /// </summary>
        private static string s_Name_Moscow_TimeZone = @"Russian Standard Time";
        /// <summary>
        /// Привести дату/время к зоне "Москва - стандартное время РФ"
        /// </summary>
        /// <param name="dt">Дата/время для приведения</param>
        /// <returns>Дата/время в МСК</returns>
        public static DateTime ToMoscowTimeZone(DateTime dt)
        //public static DateTime ToCurrentTimeZone(DateTime dt, int offset_msc)
        {
            DateTime dtRes;

            switch (dt.Kind)
            {
                case DateTimeKind.Unspecified: // предполагается UTC
                    dtRes = dt.Add(TS_MSK_OFFSET_OF_UTCTIMEZONE);
                    break;
                case DateTimeKind.Local:
                    dtRes = dt - TimeZoneInfo.Local.GetUtcOffset(dt); // получить UTC
                    dtRes = dtRes.Add(TS_MSK_OFFSET_OF_UTCTIMEZONE);
                    break;
                default: // предполагается МСК
                    //dtRes = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.FindSystemTimeZoneById(s_Name_Moscow_TimeZone));
                    dtRes = dt; //.Add(TS_NSK_OFFSET_OF_MOSCOWTIMEZONE);
                    break;
            }

            if (dtRes.IsDaylightSavingTime() == true)
            {
                dtRes = dtRes.AddHours(1);
            }
            else
            {
            }

            return dtRes;
        }

        /// <summary>
        ///  Возвратить текущие дату/время в МСК
        /// </summary>
        /// <returns></returns>
        public static DateTime ToMoscowTimeZone()
        {
            //DateTime dtRes
            //    , dt = DateTime.Now;

            //if (!(dt.Kind == DateTimeKind.Local))
            //    dtRes = dt.Add(TS_NSK_OFFSET_OF_MOSCOWTIMEZONE);
            //else
            //{
            //    dtRes = dt - TimeZoneInfo.Local.GetUtcOffset(dt);
            //    if (dtRes.IsDaylightSavingTime() == true)
            //    {
            //        dtRes = dtRes.AddHours(1);
            //    }
            //    else { }

            //    dtRes = dtRes.Add(TS_MSK_OFFSET_OF_UTCTIMEZONE); //TS_NSK_OFFSET_OF_MOSCOWTIMEZONE
            //}

            return
                //dtRes
                ToMoscowTimeZone(DateTime.Now)
                ;
        }

        /// <summary>
        /// Разность между локальным текущим времененм и МСК текущим временем
        /// </summary>
        public static TimeSpan TS_NSK_OFFSET_OF_MOSCOWTIMEZONE {
            get
            {
                return
                    //TimeZoneInfo.Local.BaseUtcOffset - TimeZoneInfo.FindSystemTimeZoneById(s_Name_Moscow_TimeZone).BaseUtcOffset
                    new TimeSpan(4, 0, 0)
                        ;
            }
        }

        //public static TimeSpan GetOffsetOfCurrentTimeZone()
        //{
        //    return DateTime.Now - HAdmin.ToCurrentTimeZone(TimeZone.CurrentTimeZone.ToUniversalTime(DateTime.Now));
        //}
        /// <summary>
        /// Возвратить смещение зоны "Москва - стандартное время РФ" от UTC
        /// </summary>
        /// <returns></returns>
        public static TimeSpan TS_MSK_OFFSET_OF_UTCTIMEZONE
        {
            get
            {
                DateTime dtNow = DateTime.Now;

                ////Перечисление всех зо ОС
                //System.Collections.ObjectModel.ReadOnlyCollection <TimeZoneInfo> tzi = TimeZoneInfo.GetSystemTimeZones ();
                //foreach (TimeZoneInfo tz in tzi) {
                //    Console.WriteLine (tz.DisplayName + @", " +  tz.StandardName + @", " + tz.Id);
                //}

                return
                    ////Вариант №1 - работает, если у пользователя установлено обновление (сезонный переход 26.10.2014)
                    //    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dtNow, HAdmin.s_Name_Moscow_TimeZone) - DateTime.UtcNow
                    ////Вариант №2 - работает, если у пользователя установлено обновление (сезонный переход 26.10.2014)
                    //    TimeZoneInfo.FindSystemTimeZoneById(HAdmin.s_Name_Moscow_TimeZone).GetUtcOffset(dtNow)
                    ////Вариант №3 - работает, если у пользователя установлено обновление (сезонный переход 26.10.2014) + известно смещение зоны пользователя от МСК
                    //    DateTime.UtcNow - dtNow - TimeSpan.FromHours(offset_msc)
                    //Вариант №4
                    TimeSpan.FromHours(3)
                    ////Вариант №5
                    //    TimeSpan.FromHours(TimeZone.CurrentTimeZone.GetUtcOffset(dtNow).Hours - TimeZoneInfo.FindSystemTimeZoneById(HHandlerDb.s_Name_Moscow_TimeZone).GetUtcOffset(dtNow).Hours)
                    ;
            }
        }

        public static bool IsMonthBoundary(DateTime dt)
        {
            bool bRes = false;

            if ((dt.Day == 1)
                && (dt.Hour == 0)
                && (dt.Minute == 0)
                && (dt.Second == 0))
                bRes = true;
            else
                ;

            return bRes;
        }

        public static DateTime ToCurrentMonthBoundary(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0);
        }

        public static DateTime ToNextMonthBoundary(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0).AddMonths(1);//??? обнуляет время перед.суток
        }
    }

    public class DateTimeRange
    {
        public DateTimeRange()
        {
            clear();
        }

        public DateTimeRange(DateTime begin, DateTime end)
        {
            Begin = begin;
            End = end;
        }

        public void Set(DateTime begin, DateTime end)
        {
            Begin = begin;
            End = end;
        }

        public DateTime Begin { get; private set; }
        public DateTime End { get; private set; }

        public bool Includes(DateTime value)
        {
            return (Begin <= value) && (value <= End);
        }

        public bool Includes(DateTimeRange range)
        {
            return (Begin <= range.Begin) && (range.End <= End);
        }

        //public void Clear()
        //{
        //    clear();
        //}

        private void clear()
        {
            Begin = DateTime.MinValue;
            End = DateTime.MaxValue;
        }

        //public bool IsEmpty
        //{
        //    get { return (Begin.Equals(DateTime.MinValue) == true) && (End.Equals(DateTime.MaxValue) == true); }
        //}
    }
}
