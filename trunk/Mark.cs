using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms; //Для MessageBox

namespace HClassLibrary
{
    public class HMark
    {
        private Int32 m_mark;
        
        public HMark () {
            m_mark = 0;
        }

        public static Int32 Pow2Bit (int bit, int offset = 0) {
            return (Int32)Math.Pow(2, bit + offset);
        }

        private void marked (bool opt, int bit) {
            Int32 val = Pow2Bit (bit);

            if (opt == true)
                if (!((m_mark & val) == val)) m_mark += val; else ;
            else
                if ((m_mark & val) == val) m_mark -= val; else ;
        }

        public void Set(int bit, bool val)
        {
            marked(val, bit);
        }

        public void SetOf(HMark mark)
        {
            //for (int i = 0; i < sizeof (Int32) * 8; i ++)
            //    marked (IsMarked (i), i);
            m_mark = mark.Value;
        }

        public void Add(HMark mark)
        {
            for (int i = 0; i < sizeof(Int32) * 8; i++) {
                if ((IsMarked(mark.Value, i) == true) && (IsMarked(i) == false)) marked(true, i); else ;
            }
        }

        public void Marked (int bit) {
            marked (true, bit);
        }

        public void UnMarked()
        {
            m_mark = 0;
        }

        public void UnMarked (int bit) {
            marked(false, bit);
        }

        public bool IsMarked (int bit) {
            return IsMarked (m_mark, bit);
        }

        public static bool IsMarked(int val, int bit, int offset = 0)
        {
            bool bRes = false;

            if ((val & Pow2Bit(bit, offset)) == Pow2Bit(bit, offset))
            {
                bRes = true;
            }
            else
                ;

            return bRes;
        }

        public Int32 Value {
            get { return m_mark; }
        }
    }
}