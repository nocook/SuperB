using System;
using System.Collections.Generic;
using System.Text;

namespace SuperBDevAccess
{
    public class ClientObj
    {
        public string ip { get; set; }
        public int port { get; set; }
        /// <summary>
        /// socket是否连接
        /// </summary>
        public bool bConnected { get; set; }
        /// <summary>
        /// 上一次心跳更新时间
        /// </summary>
        public long lLatestTicket { get; set; }
        /// <summary>
        /// 心跳间隔
        /// </summary>
        public int iInertval { get; set; }
        /// <summary>
        ///记录是否注册到下级
        /// </summary>
        public bool bLogin { get; set; }
    }


}
