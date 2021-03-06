﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ASUTP.Core {
    /// <summary>
    /// Класс для хранения значения ключа при (де)шифрации
    /// </summary>
    public class Crypt {
        /// <summary>
        /// 1-ая (публичная) часть ключа (де)шифрования
        /// </summary>
        public static string KEY = "AsDfGhJkL;";

        private static uint MAGIC = 0xA5A55A5A;
        private static int magicPeriod = 10;
        private static uint [] key = {
                                0x3a39ce37,   0xd3faf5cf,   0xabc27737,   0x5ac52d1b,   0x5cb0679e,   0x4fa33742,
                                0xd3822740,   0x99bc9bbe,   0xd5118e9d,   0xbf0f7315,   0xd62d1c7e,   0xc700c47b,
                                0xb78c1b6b,   0x21a19045,   0xb26eb1be,   0x6a366eb4,   0x5748ab2f,   0xbc946e79,
                                0xc6a376d2,   0x6549c2c8,   0x530ff5ee,   0x468dde7d,   0xd5730a1d,   0x4cd04dc6,
                                0x2939bbdb,   0xa9ba4650,   0xac9526e8,   0xbe5ee304,   0xa1fad5f0,   0x6a2d519a,
                                0x63ef8ce2,   0x9a86ee22,   0xc089c2b8,   0x43242ef6,   0xa51e03aa,   0x9cf2d0a4,
                                0x83c061ba,   0x9be96a4d,   0x8fe51550,   0xba645bd6,   0x2826a2f9,   0xa73a3ae1,
                                0x4ba99586,   0xef5562e9,   0xc72fefd3,   0xf752f7da,   0x3f046f69,   0x77fa0a59,
                                0x80e4a915,   0x87b08601,   0x9b09e6ad,   0x3b3ee593,   0xe990fd5a,   0x9e34d797,
                                0x2cf0b7d9,   0x022b8b51,   0x96d5ac3a,   0x017da67d,   0xd1cf3ed6,   0x7c7d2d28,
                                0x1f9f25cf,   0xadf2b89b,   0x5ad6b472,   0x5a88f54c,   0xe029ac71,   0xe019a5e6,
                                0x47b0acfd,   0xed93fa9b,   0xe8d3c48d,   0x283b57cc,   0xf8d56629,   0x79132e28,
                                0x785f0191,   0xed756055,   0xf7960e44,   0xe3d35e8c,   0x15056dd4,   0x88f46dba,
                                0x03a16125,   0x0564f0bd,   0xc3eb9e15,   0x3c9057a2,   0x97271aec,   0xa93a072a,
                                0x1b3f6d9b,   0x1e6321f5,   0xf59c66fb,   0x26dcf319,   0x7533d928,   0xb155fdf5,
                                0x03563482,   0x8aba3cbb,   0x28517711,   0xc20ad9f8,   0xabcc5167,   0xccad925f,
                                0x4de81751,   0x3830dc8e,   0x379d5862,   0x9320f991,   0xea7a90c2,   0xfb3e7bce,
                                0x5121ce64,   0x774fbe32,   0xa8b6e37e,   0xc3293d46,   0x48de5369,   0x6413e680,
                                0xa2ae0810,   0xdd6db224,   0x69852dfd,   0x09072166,   0xb39a460a,   0x6445c0dd,
                                0x586cdecf,   0x1c20c8ae,   0x5bbef7dd,   0x1b588d40,   0xccd2017f,   0x6bb4e3bb,
                                0xdda26a7e,   0x3a59ff45,   0x3e350a44,   0xbcb4cdd5,   0x72eacea8,   0xfa6484bb,
                                0x8d6612ae,   0xbf3c6f47,   0xd29be463,   0x542f5d9e,   0xaec2771b,   0xf64e6370,
                                0x740e0d8d,   0xe75b1357,   0xf8721671,   0xaf537d5d,   0x4040cb08,   0x4eb4e2cc,
                                0x34d2466a,   0x0115af84,   0xe1b00428,   0x95983a1d,   0x06b89fb4,   0xce6ea048,
                                0x6f3f3b82,   0x3520ab82,   0x011a1d4b,   0x277227f8,   0x611560b1,   0xe7933fdc,
                                0xbb3a792b,   0x344525bd,   0xa08839e1,   0x51ce794b,   0x2f32c9b7,   0xa01fbac9,
                                0xe01cc87e,   0xbcc7d1f6,   0xcf0111c3,   0xa1e8aac7,   0x1a908749,   0xd44fbd9a,
                                0xd0dadecb,   0xd50ada38,   0x0339c32a,   0xc6913667,   0x8df9317c,   0xe0b12b4f,
                                0x0f91fc71,   0x9b941525,   0xfae59361,   0xceb69ceb,   0xc2a86459,   0x12baa8d1,
                                0xb6c1075e,   0xe3056a0c,   0x10d25065,   0xcb03a442,   0xe0ec6e0e,   0x1698db3b,
                                0x4c98a0be,   0x3278e964,   0x9f1f9532,   0xe0d392df,   0xd3a0342b,   0x8971f21e,
                                0x1b0a7441,   0x4ba3348c,   0xc5be7120,   0xc37632d8,   0xdf359f8d,   0x9b992f2e,
                                0xe60b6f47,   0x0fe3f11d,   0xe54cda54,   0x1edad891,   0xce6279cf,   0xcd3e7e6f,
                                0x1618b166,   0xfd2c1d05,   0x848fd2c5,   0xf6fb2299,   0xf523f357,   0xa6327623,
                                0x93a83531,   0x56cccd02,   0xacf08162,   0x5a75ebb5,   0x6e163697,   0x88d273cc,
                                0xde966292,   0x81b949d0,   0x4c50901b,   0x71c65614,   0xe6c6c7bd,   0x327a140a,
                                0x45e1d006,   0xc3f27b9a,   0xc9aa53fd,   0x62a80f00,   0xbb25bfe2,   0x35bdd2f6,
                                0x71126905,   0xb2040222,   0xb6cbcf7c,   0xcd769c2b,   0x53113ec0,   0x1640e3d3,
                                0x38abbd60,   0x2547adf0,   0xba38209c,   0xf746ce76,   0x77afa1c5,   0x20756060,
                                0x85cbfe4e,   0x8ae88dd8,   0x7aaaf9b0,   0x4cf9aa7e,   0x1948c25c,   0x02fb8a8c,
                                0x01c36ae4,   0xd6ebe1f9,   0x90d4f869,   0xa65cdea0,   0x3f09252d,   0xc208e69f
                            };

        /// <summary>
        /// Конструктор - дополнительный (без параметров)
        /// </summary>
        private Crypt ()
        {
        }

        private static Crypt m_this;

        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        /// <returns>Объект класса</returns>
        public static Crypt Crypting ()
        {
            if (m_this == null) {
                m_this = new Crypt ();
            } else
                ;

            return m_this;
        }

        /// <summary>
        /// Дешифровать значение
        /// </summary>
        /// <param name="src">Массив символов для дешифрации</param>
        /// <param name="count">??? Количество символов в массиве</param>
        /// <param name="msgErr">Сообщение при ошибке выполнения метода</param>
        /// <returns>Дешифрованное значение</returns>
        public StringBuilder Decrypt (char [] src, int count, out string msgErr)
        {
            msgErr = string.Empty;

            StringBuilder res = new StringBuilder (1024);
            int i = 0, j = 0, k = 3;

            uint magic;
            if (count > 0)
                while ((i + 0) < count) {
                    res.Append ((char)(src [i] ^ (char)((key [j] >> (8 * k)) & 0xFF)));
                    i++;
                    k--;
                    if (k < 0) {
                        k = 0;
                        j++;
                        if (j >= key.Length)
                            j = 0;
                        else
                            ;
                    } else
                        ;

                    if (i % magicPeriod == 0) {
                        magic = 0;
                        magic = (char)(src [i] ^ (char)((key [j] >> (8 * k)) & 0xFF));
                        k--;
                        if (k < 0) {
                            k = 0;
                            j++;
                            if (j >= key.Length)
                                j = 0;
                            else
                                ;
                        } else
                            ;
                        magic = magic << 8;
                        magic += (char)(src [i + 1] ^ (char)((key [j] >> (8 * k)) & 0xFF));
                        k--;
                        if (k < 0) {
                            k = 0;
                            j++;
                            if (j >= key.Length)
                                j = 0;
                            else
                                ;
                        } else
                            ;
                        magic = magic << 8;
                        magic += (char)(src [i + 2] ^ (char)((key [j] >> (8 * k)) & 0xFF));
                        k--;
                        if (k < 0) {
                            k = 0;
                            j++;
                            if (j >= key.Length)
                                j = 0;
                            else
                                ;
                        } else
                            ;
                        magic = magic << 8;
                        magic += (char)(src [i + 3] ^ (char)((key [j] >> (8 * k)) & 0xFF));
                        k--;
                        if (k < 0) {
                            k = 0;
                            j++;
                            if (j >= key.Length)
                                j = 0;
                            else
                                ;
                        } else
                            ;
                        i += 4;
                        if (magic != MAGIC) {
                            msgErr = "Зашифрованные данные имеют неправильный формат!\nОбратитесь к поставщику программы.";
                        } else
                            ;
                    }
                } else
                msgErr = "Нет зашифрованных данных!\nОбратитесь к поставщику программы.";

            return res;
        }

        /// <summary>
        /// Дешифровать массив байт
        /// </summary>
        /// <param name="data">Массив байт для дешифрации</param>
        /// <param name="password">1-ая (публичная) часть ключа</param>
        /// <returns>Массив байт после дешифрации</returns>
        public byte [] Decrypt (byte [] data, string password)
        {
            BinaryReader br = new BinaryReader (InternalDecrypt (data, password));

            return br.ReadBytes ((int)br.BaseStream.Length);
        }

        /// <summary>
        /// Дешифровать строку
        /// </summary>
        /// <param name="data">Строка для дешифрации</param>
        /// <param name="password">1-ая (публичная) часть ключа</param>
        /// <returns>Строка после дешифрации</returns>
        public string Decrypt (string data, string password)
        {
            String result = "";
            try {
                CryptoStream cs = InternalDecrypt (Convert.FromBase64String (data), password);
                StreamReader sr = new StreamReader (cs);
                result = sr.ReadToEnd ();
                return result;
            } catch {
                return result;
            }
        }

        /// <summary>
        /// Возвратить поток с дешифрованным значением
        /// </summary>
        /// <param name="data">Массив байт для дешифрации</param>
        /// <param name="password">1-ая (публичная) часть ключа</param>
        /// <returns>Поток с дешифрованным значением</returns>
        CryptoStream InternalDecrypt (byte [] data, string password)
        {
            SymmetricAlgorithm sa = Rijndael.Create ();
            ICryptoTransform ct = sa.CreateDecryptor (
                (new PasswordDeriveBytes (password, null)).GetBytes (16),
                new byte [16]);

            MemoryStream ms = new MemoryStream (data);

            return new CryptoStream (ms, ct, CryptoStreamMode.Read);
        }

        /// <summary>
        /// Зашифровать значение
        /// </summary>
        /// <param name="src">Строка для шифрования</param>
        /// <param name="err">Признак ошибки при выполнении метода</param>
        /// <returns>Массив символов после шифрации</returns>
        public char [] Encrypt (StringBuilder src, out int err)
        {
            err = 1;

            char [] res = new char [1024];

            int i = 0, j = 0, k = 3, t = 0;
            while (t < src.Length) {
                res [i] = (char)(src [t] ^ (char)((key [j] >> (8 * k)) & 0xFF));
                i++;
                t++;
                k--;
                if (k < 0) {
                    k = 0;
                    j++;
                    if (j >= key.Length)
                        j = 0;
                }

                if (i % magicPeriod == 0) {
                    res [i] = (char)((char)((MAGIC >> 24) & 0xFF) ^ (char)((key [j] >> (8 * k)) & 0xFF));
                    k--;
                    if (k < 0) {
                        k = 0;
                        j++;
                        if (j >= key.Length)
                            j = 0;
                    }
                    res [i + 1] = (char)((char)((MAGIC >> 16) & 0xFF) ^ (char)((key [j] >> (8 * k)) & 0xFF));
                    k--;
                    if (k < 0) {
                        k = 0;
                        j++;
                        if (j >= key.Length)
                            j = 0;
                    }
                    res [i + 2] = (char)((char)((MAGIC >> 8) & 0xFF) ^ (char)((key [j] >> (8 * k)) & 0xFF));
                    k--;
                    if (k < 0) {
                        k = 0;
                        j++;
                        if (j >= key.Length)
                            j = 0;
                    }
                    res [i + 3] = (char)((char)(MAGIC & 0xFF) ^ (char)((key [j] >> (8 * k)) & 0xFF));
                    k--;
                    if (k < 0) {
                        k = 0;
                        j++;
                        if (j >= key.Length)
                            j = 0;
                    }
                    i += 4;
                }
            }

            err = i; //Успешно

            return res;
        }

        /// <summary>
        /// Зашифровать значение
        /// </summary>
        /// <param name="data">Массив байт для шифрации</param>
        /// <param name="password">1-ая (публичная) часть ключа</param>
        /// <returns>>Массив байт после шифрации</returns>
        public byte [] Encrypt (byte [] data, string password)
        {
            SymmetricAlgorithm sa = Rijndael.Create ();
            ICryptoTransform ct = sa.CreateEncryptor (
                (new PasswordDeriveBytes (password, null)).GetBytes (16),
                new byte [16]);

            MemoryStream ms = new MemoryStream ();
            CryptoStream cs = new CryptoStream (ms, ct, CryptoStreamMode.Write);

            cs.Write (data, 0, data.Length);
            cs.FlushFinalBlock ();

            return ms.ToArray ();
        }

        /// <summary>
        /// Зашифровать значение
        /// </summary>
        /// <param name="data">Строка для шифрации</param>
        /// <param name="password">1-ая (публичная) часть ключа</param>
        /// <returns>Строка после шифрации</returns>
        public string Encrypt (string data, string password)
        {
            if (data.Equals (string.Empty) == false)
                return Convert.ToBase64String (Encrypt (Encoding.UTF8.GetBytes (data), password));
            else
                return string.Empty;
        }
    }
}
