using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HClassLibrary
{
    public class HDateTime
    {
        public enum INTERVAL : short { UNKNOWN = -1, MINUTES, HOURS, COUNT_ID_TIME }

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

            if (!(dt.Kind == DateTimeKind.Local))
            {
                //    dtRes = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.FindSystemTimeZoneById(s_Name_Moscow_TimeZone));
                dtRes = dt.Add(GetUTCOffsetOfMoscowTimeZone());
            }
            else
            {
                dtRes = dt - TimeZoneInfo.Local.GetUtcOffset(dt);
                if (dtRes.IsDaylightSavingTime() == true)
                {
                    dtRes = dtRes.AddHours(-1);
                }
                else { }

                dtRes = dtRes.Add(GetUTCOffsetOfMoscowTimeZone());
                //    //dtRes = dtRes.Add(GetUTCOffsetOfCurrentTimeZone(offset_msc));
            }

            return dtRes;
        }

        public static DateTime ToMoscowTimeZone()
        {
            DateTime dtRes
                , dt = DateTime.Now;

            if (!(dt.Kind == DateTimeKind.Local))
                dtRes = dt.Add(GetUTCOffsetOfMoscowTimeZone());
            else
            {
                dtRes = dt - TimeZoneInfo.Local.GetUtcOffset(dt);
                if (dtRes.IsDaylightSavingTime() == true)
                {
                    dtRes = dtRes.AddHours(-1);
                }
                else { }

                dtRes = dtRes.Add(GetUTCOffsetOfMoscowTimeZone());
            }

            return dtRes;
        }

        //public static TimeSpan GetOffsetOfCurrentTimeZone()
        //{
        //    return DateTime.Now - HAdmin.ToCurrentTimeZone(TimeZone.CurrentTimeZone.ToUniversalTime(DateTime.Now));
        //}
        /// <summary>
        /// Возвратить смещение зоны "Москва - стандартное время РФ" от UTC
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetUTCOffsetOfMoscowTimeZone()
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
            return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0).AddMonths(1);
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
