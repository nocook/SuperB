using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperBMgr
{
    public static class Uuid
    {
        static int _newId = 1;
        public static string NewId { get { return (_newId++).ToString(); } }

        // like Ivj6eZRx40MTx2ZvnG8nA  
        public static string CreateToken()
        {
            Guid g = Guid.NewGuid();
            var token = Convert.ToBase64String(g.ToByteArray()).Replace("=", "").Replace("+", "").Replace("/", "");
            return token;
        }

        /// <summary>  
        /// 根据GUID获取16位的唯一字符串  
        /// </summary>  
        /// <param name="guid"></param>  
        /// <returns></returns>  
        public static string Create16Token()
        {
            var i = Guid.NewGuid().ToByteArray().Aggregate<byte, long>(1, (current, b) => current * (b + 1));
            return $"{i - DateTime.Now.Ticks:x}";
        }

    }
}
