using Spjk.ProtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spjk.WXThread;
using System.Threading;
using Spjk.DeviceAccessFramework;
using System.Net;
using static SuperBDevAccess.WxBSdk;

namespace SuperBDevAccess
{
    public delegate void PushMsgCallBack(BaseMsg msg);
    class NetMsg : WXPublicThread
    {
        public ClientObj m_ClientObj;
        public User m_user;//登录账户

        private long m_LatestTicket;//记录上一次发送心跳时间
        private CTcpProtocolNet m_tcpNet;
        private CmdBuild m_CmdBuild;
        private List<byte> m_listdata;//暂存网络接受到的字节，用于分割
        private PushMsgCallBack m_PushMsgCallBack;
        public NetMsg()
        {
            m_CmdBuild = new CmdBuild();
            m_listdata = new List<byte>();
            m_user = new User();
            m_ClientObj = new ClientObj();
            m_ClientObj.iInertval = 20;//default 20s
            m_ClientObj.bConnected = false;
            m_ClientObj.bLogin = false;
            m_tcpNet = new CTcpProtocolNet();
            m_tcpNet.Init();
            m_tcpNet.SetCallBack(DataReceiveCallBack);

            m_LatestTicket = 0;
        }
        public void SetFunc(PushMsgCallBack cb)
        {
            m_PushMsgCallBack = cb;
        }
        /// <summary>
        /// 启动工作线程
        /// </summary>
        /// <returns></returns>
        public int StartWork()
        {
            return Start();
        }

        /// <summary>
        /// 结束工作线程
        /// </summary>
        /// <returns></returns>
        public int StopWork()
        {
            try
            {
                Stop();
                m_tcpNet.CloseClient(m_ClientObj.ip, m_ClientObj.port);
                Console.WriteLine("工作线程结束");
            }
            catch (Exception) { }
            return 0;
        }
        /// <summary>
        /// 负责连接socket，登录注册
        /// </summary>
        /// <returns></returns>
        protected override int Run()
        {
            try
            {
                if (!m_ClientObj.bConnected)
                {
                    m_ClientObj.bLogin = false;
                    m_tcpNet.CloseClient(m_ClientObj.ip, m_ClientObj.port);
                    m_ClientObj.bConnected = m_tcpNet.TryConnect(m_ClientObj.ip, m_ClientObj.port, 3000) == 0 ? true : false;
                }
                else if (!m_ClientObj.bLogin)
                {
                    //登录
                    byte[] bsend = m_CmdBuild.Login(m_user, m_CmdBuild.GetSerialNo());
                    m_SendData(bsend, m_ClientObj.ip, m_ClientObj.port);
                    Console.WriteLine("Step 1 登录");
                }
                if (m_ClientObj.bLogin)
                {
                    //定时心跳请求
                    DateTime dtnow = System.DateTime.Now;
                    if (dtnow.Ticks - m_LatestTicket < m_ClientObj.iInertval * 1000L * 10000L)
                    {
                        return 1;
                    }
                    m_LatestTicket = dtnow.Ticks;
                    int a = m_CmdBuild.GetSerialNo();
                    Console.WriteLine("Time=" + DateTime.Now.ToString("yy-MM-dd-hh-mm-ss") + " Net Numb=" + a);
                    byte[] bsend = m_CmdBuild.KeepHeartBeat("", a);
                    m_SendData(bsend, m_ClientObj.ip, m_ClientObj.port);
                    Console.WriteLine("set  heart request");
                }
                Thread.Sleep(2000);
            }
            catch (Exception)
            { }
            return 1;
        }
        private int DataReceiveCallBack(NumNetType nType, string strIp, int nPort, byte[] buffer, int isize)
        {
            try
            {
                switch (nType)
                {
                    case NumNetType.Data:
                        {
                            List<byte[]> listRec = adddata(buffer, strIp, nPort);
                            foreach (var va in listRec)
                            {
                                int len = 0; int serialn = 0; int pktype = 0; string re = ""; byte[] bres = null;
                                m_CmdBuild.MessageAnalysis(va, ref len, ref serialn, ref pktype, ref re, ref bres);
                                BaseMsg msg = new BaseMsg();
                                msg.FromIp = strIp;
                                msg.FromPort = nPort;
                                msg.cmd = pktype.ToString();
                                msg.Type = MSGType.msgRequest;
                                msg.xml = re;
                                msg.xmlbyte = bres;
                                msg.Seq = serialn;
                                //LogHelper.Trace($"Net Receive:{BitConverter.ToString(va)}:pktype={pktype};fromip:{strIp};fromport{nPort}");
                                m_PushMsgCallBack(msg);
                            }
                        }
                        break;
                    case NumNetType.ClientConnect:
                        {
                            //Console.WriteLine("ClientConnect ip ={0},port ={1}", strIp, nPort);
                            //BaseMsg msg = new BaseMsg();
                            //msg.FromIp = strIp;
                            //msg.FromPort = nPort;
                            //msg.Type = MSGType.msgNetConnect;
                            //m_PushMsgCallBack(msg);
                        }
                        break;
                    case NumNetType.ClientClose:
                        {
                            BaseMsg msg = new BaseMsg();
                            msg.FromIp = strIp;
                            msg.FromPort = nPort;
                            msg.Type = MSGType.msgNetClose;
                            m_PushMsgCallBack(msg);
                            m_ClientObj.bConnected = false;
                        }
                        break;
                }
            }
            catch (Exception) { }
            return 0;
        }
        /// <summary>
        /// 筛查网络接受的字节
        /// </summary>
        /// <param name="bd"></param>
        /// <param name="strIp"></param>
        /// <param name="iPort"></param>
        /// <returns></returns>
        private List<byte[]> adddata(byte[] bd, string strIp, int iPort)
        {
            string strKey = strIp + ":" + iPort.ToString();
            List<byte[]> listb = new List<byte[]>();
            m_listdata.AddRange(bd);
            if (m_listdata.Count < 12)
            {
                return listb;
            }
            else
            {
                while (true && m_listdata.Count >= 10)
                {
                    byte[] bhead = new byte[10];
                    m_listdata.CopyTo(0, bhead, 0, 10);
                    string strhead = Encoding.ASCII.GetString(bhead);
                    if ("#dlhjtxxy#" == strhead)
                    {
                        if (m_listdata.Count < 10 + 12 + 2)
                        {
                            return listb;
                        }
                        else
                        {
                            int iLenth = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(m_listdata.Skip(10).ToArray(), 0));
                            if (iLenth <= 0)
                            {
                                m_listdata.RemoveAt(0);//排除脏数据
                                continue;
                            }
                            if (m_listdata.Count < 12 + iLenth)
                            {
                                return listb;
                            }
                            else
                            {
                                byte[] bt = new byte[iLenth + 12];
                                m_listdata.CopyTo(0, bt, 0, iLenth + 12);
                                m_listdata.RemoveRange(0, iLenth + 12);
                                listb.Add(bt);
                            }
                        }
                    }
                    else
                    {
                        m_listdata.RemoveAt(0);//排除脏数据
                    }
                }
            }
            return listb;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="send"></param>
        /// <param name="strIp"></param>
        /// <param name="iPort"></param>
        /// <returns></returns>
        public int m_SendData(byte[] send, string strIp, int iPort)
        {
            return m_tcpNet.SendData(send, send.Length, strIp, iPort);
        }
    }
}