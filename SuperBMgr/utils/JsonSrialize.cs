using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SuperBMgr
{
    public static class JsonSrialize
    {
        public static string Srialize(object obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            if (str.Length > 3
                && str[0] == 0xef
                && str[1] == 0xbb
                && str[2] == 0xbf)
            {
                //去掉BOM
                str = str.Substring(3, str.Length - 3);
            }
            return JsonConvert.SerializeObject(obj);
        }

        public static T Desrialize<T>(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return default(T);
            }
        }

        /// <summary>
        /// 将对象转换为byte数组
        /// </summary>
        /// <param name="obj">被转换对象</param>
        /// <returns>转换后byte数组</returns>
        public static byte[] Object2Bytes(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] serializedResult = System.Text.Encoding.UTF8.GetBytes(json);
            return serializedResult;
        }

        /// <summary>
        /// 将byte数组转换成对象
        /// </summary>
        /// <param name="buff">被转换byte数组</param>
        /// <returns>转换完成后的对象</returns>
        public static object Bytes2Object(byte[] buff)
        {
            if (buff == null) return null;
            string json = System.Text.Encoding.UTF8.GetString(buff);
            return JsonConvert.DeserializeObject<object>(json);
        }

        /// <summary>
        /// 将byte数组转换成对象
        /// </summary>
        /// <param name="buff">被转换byte数组</param>
        /// <returns>转换完成后的对象</returns>
        public static T Bytes2Object<T>(byte[] buff)
        {
            if (buff == null) return default(T);
            string json = System.Text.Encoding.UTF8.GetString(buff);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// 将byte数组转换成对象
        /// </summary>
        /// <param name="buff">被转换byte数组</param>
        /// <returns>转换完成后的对象</returns>
        public static T Bytes2Object<T>(byte[] buff, int offset, int count)
        {
            if (buff == null) return default(T);
            string json = System.Text.Encoding.UTF8.GetString(buff, offset, count);
            return JsonConvert.DeserializeObject<T>(json);
        }


        public static string FormatJson(this string str)
        {
            StringBuilder newStr = new StringBuilder(str);
            newStr = newStr.Replace("{", "\r\n{\r\n")
                .Replace("}", "\r\n}\r\n")
                .Replace(",", ",\r\n");
            int cenNum = 0;
            for (int i = 0; i < newStr.Length; i++)
            {
                if (newStr[i] == '{') cenNum++;
                if (newStr[i] == '}') cenNum--;
                if (cenNum > 0 && (i - 1) > 0 && newStr[i - 1] == '\n')
                {
                    newStr.Insert(i, "  ", cenNum);
                    i += cenNum;
                }
            }

            return newStr.ToString();
        }
    }
}
