using Spjk.DeviceAccessFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Spjk.WXThread;
using System.Threading;
using System.Timers;
using System.IO;
using static SuperBDevAccess.WxBSdk;

namespace SuperBDevAccess
{
    internal delegate void DataCallBack(Sdk_Event cmd, params object[] objs);
    internal delegate void StatusCallBack(Sdk_StatusEvent cmd, params object[] objs);
    internal class BSDK : WXPublicThread
    {
        private CmdBuild m_CmdBuild;
        private DataCallBack m_DataBack;
        private StatusCallBack m_StatusBack;
        private NetMsg m_NetMsg;
        private object m_listLock;
        private List<BaseMsg> m_msgList;
        private System.Timers.Timer Timer_TimeSync;
        private EnumAlarmMode m_EnumAlarmMode;
        private EnumAccessMode m_EnumAccessMode;
        private int m_RealDataPolling;
        private long m_LatestTicket;//记录上一次检查心跳时间

        private List<BStruBase> L_bStruBases;//记录所有采集点位 及信息
        private Dictionary<int, string> m_DicIDSerialNo;//报文序号+采集点-->tocken
        private object m_dicObj;
        public BSDK()
        {

            m_msgList = new List<BaseMsg>();
            m_listLock = new object();
            m_NetMsg = new NetMsg();
            m_CmdBuild = new CmdBuild();
            m_NetMsg.SetFunc(PushMessage);//设置回调函数
            m_LatestTicket = 0;
            m_EnumAlarmMode = EnumAlarmMode.MINOR;//默认一般告警及以上都上报
            m_EnumAccessMode = EnumAccessMode.TIME_TRIGGER;//默认定时发送数据
            m_RealDataPolling = 20;//默认3s上传一次实时数据
            L_bStruBases = new List<BStruBase>();
            Timer_TimeSync = new System.Timers.Timer();
            Timer_TimeSync.Interval = 12 * 60 * 60 * 1000;//12小时同步一次时间
            Timer_TimeSync.Enabled = false;
            Timer_TimeSync.Elapsed += new ElapsedEventHandler(Timer_TSync);

            m_DicIDSerialNo = new Dictionary<int, string>();
            m_dicObj = new object();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="ip">下级服务端的IP</param>
        /// <param name="port">下级服务端的端口</param>
        /// <param name="user">向下级服务端注册请求的账号密码</param>
        public void Initial(string ip, int port, User user)
        {
            m_NetMsg.m_user = user;
            m_NetMsg.m_ClientObj.ip = ip != "" ? ip : "127.0.0.1";
            m_NetMsg.m_ClientObj.port = port > 0 ? port : 12345;
        }
        /// <summary>
        /// 启动工作线程
        /// </summary>
        /// <returns></returns>
        public int StartWork()
        {
            Start();
            return m_NetMsg.StartWork();
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
                m_NetMsg.StopWork();
                Timer_TimeSync.Enabled = false;
                Timer_TimeSync.Dispose();
                Console.WriteLine("工作线程结束");
            }
            catch (Exception) { }
            return 0;
        }
        /// <summary>
        /// 设定心跳间隔
        /// </summary>
        /// <param name="time"></param>
        public void SetHearbeatInterval(int time)
        {
            m_NetMsg.m_ClientObj.iInertval = time > 15 ? time : 15;
        }
        /// <summary>
        /// 注册 断线/链接 回调事件，以及节点属性，实时数据，告警信息 回调
        /// </summary>
        /// <param name="dataCallBack">节点属性，实时数据，告警信息 事件</param>
        /// <param name="statusCallBack">链接，断开 事件</param>
        public void SetCallBackFun(DataCallBack dataCallBack, StatusCallBack statusCallBack)
        {
            m_DataBack = dataCallBack;
            m_StatusBack = statusCallBack;
        }
        /// <summary>
        /// 请求时间同步
        /// </summary>
        public void SetTimeSync()
        {
            HandleTimeCheck();
        }
        /// <summary>
        /// 设定请求实时数据
        /// </summary>
        /// <param name="GroupID">不知道作用，开放出来，暂时固定给0</param>
        /// <param name="enumAccessMode">实时数据的上报方式</param>
        /// <param name="PollingTime">定时方式时，上报的时间间隔</param>
        /// <param name="id">需要请求的ID，这个ID可以是具体采集点ID，或者 设备ID（但是设备ID的DDD字段全部置0，表示请求该设备下的所有采集点）</param>
        public void SetRealTimeDataMode(int GroupID, EnumAccessMode enumAccessMode, int PollingTime, List<int> id)
        {
            try
            {
                PollingTime = PollingTime < 1 ? 1 : PollingTime;
                if (id.Count < 0) return;
                byte[] bsend = m_CmdBuild.SetDYN_AccessMode(GroupID, enumAccessMode, PollingTime, id.Count, id, m_CmdBuild.GetSerialNo());
                LogHelper.Trace($"请求实时数据报文={BitConverter.ToString(bsend)}");
                m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
            }
            catch (Exception) { }
        }
        /// <summary>
        /// 设定告警上报模式
        /// </summary>
        /// <param name="GroupID">不知道作用，开放出来，暂时固定给0</param>
        /// <param name="enumAlarmMode">选择等级上报告警</param>
        /// <param name="id">需要上报告警的ID，若要全部数据上报告警，取全1</param>
        public void SetAlarmDataMode(int GroupID, EnumAlarmMode enumAlarmMode, List<UInt32> id)
        {
            try
            {
                byte[] bsend = m_CmdBuild.SetAlarmMode(GroupID, enumAlarmMode, 0, id, m_CmdBuild.GetSerialNo());//参数3设为0，参阅铁标17页
                if (id.Count < 0) return;
                m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
            }
            catch (Exception)
            { }
        }
        /// <summary>
        /// 确认告警
        /// </summary>
        /// <param name="ID">需要确认告警的采集点的ID</param>
        /// <param name="alarmtime">该告警产生的时间</param>
        public void SetAlarmConfirm(int ID, DateTime dt, string tocken)
        {
            List<byte> Lcontent = new List<byte>();
            int seriNo = 0;
            try
            {
                Lcontent.Clear();
                seriNo = m_CmdBuild.GetSerialNo();
                Lcontent.AddRange(m_CmdBuild.Name(new string(m_NetMsg.m_user.username)));
                Lcontent.AddRange(m_CmdBuild.LittleToBig(ID));
                Lcontent.AddRange(m_CmdBuild.PackTTime(dt));
                Lcontent.AddRange(m_CmdBuild.PackTTime(DateTime.Now));
                byte[] bsend = m_CmdBuild.ReqAckAlarm(Lcontent.ToArray(), seriNo);
                if (bsend != null)
                {
                    m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
                    AddDic(seriNo, tocken);
                    LogHelper.Info($"下发外部接口确认告警历史消息");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"请求 告警确认 报错：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 清除告警
        /// </summary>
        /// <param name="ID">需要清除告警的采集点的ID</param>
        /// <param name="alarmtime">该告警产生的时间</param>
        public void SetAlarmClear(int ID, DateTime dt, string tocken)
        {
            List<byte> Lcontent = new List<byte>();
            int seriNo = 0;
            try
            {
                Lcontent.Clear();
                seriNo = m_CmdBuild.GetSerialNo();
                Lcontent.AddRange(m_CmdBuild.Name(new string(m_NetMsg.m_user.username)));
                Lcontent.AddRange(m_CmdBuild.LittleToBig(ID));
                Lcontent.AddRange(m_CmdBuild.PackTTime(dt));
                Lcontent.AddRange(m_CmdBuild.PackTTime(DateTime.Now));
                byte[] bsend = m_CmdBuild.ReqCanclAlarm(Lcontent.ToArray(), seriNo);
                if (bsend != null)
                {
                    m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
                    AddDic(seriNo, tocken);
                    LogHelper.Info($"下发外部接口清除告警历史消息");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"请求 告警清除 报错：{ex.ToString()}");
            }
        }
        /// <summary>
        /// 请求修改配置属性
        /// </summary>
        public void SetAttribute(BStruBase bStruBase, string tocken)
        {
            List<byte> Lcontent = new List<byte>();
            byte[] bsend = null;
            int seriNo = 0;
            try
            {
                seriNo = m_CmdBuild.GetSerialNo();
                switch (bStruBase.Type)
                {
                    case EnumType.AO:
                        break;
                    case EnumType.DO:

                        break;
                    case EnumType.STRIN:

                        break;
                    default:
                        break;
                }
                bsend = m_CmdBuild.SetPropertyModify(Lcontent.ToArray(), seriNo);
                if (bsend != null)
                {
                    m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
                    AddDic(seriNo, tocken);
                    LogHelper.Info($"下发外部接口修改配置");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"请求 设置属性 报错：{ex.ToString()}");
            }
        }
        /// <summary>
        /// 请求控制指令
        /// </summary>
        /// <param name="bStruBase">必要值，type id value length</param>
        public void SetControl(BStruBase bStruBase, string tocken)
        {
            List<byte> Lcontent = new List<byte>();
            byte[] bsend = null;
            int seriNo = 0;
            EnumState enumState = EnumState.NOALARM;//不知道这个状态作用，默认给正常
            try
            {
                switch (bStruBase.Type)
                {
                    case EnumType.AO:
                        TAOC tAOC = (TAOC)bStruBase;
                        Lcontent.AddRange(m_CmdBuild.LittleToBig((int)tAOC.Type));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig(tAOC.ID));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig(tAOC.Value));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig((int)enumState));
                        seriNo = m_CmdBuild.GetSerialNo();
                        bsend = m_CmdBuild.SetPoint(Lcontent.ToArray(), seriNo);
                        break;
                    case EnumType.DO:
                        TDOC tDOC = (TDOC)bStruBase;
                        Lcontent.AddRange(m_CmdBuild.LittleToBig((int)tDOC.Type));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig(tDOC.ID));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig(tDOC.Value));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig((int)enumState));
                        seriNo = m_CmdBuild.GetSerialNo();
                        bsend = m_CmdBuild.SetPoint(Lcontent.ToArray(), seriNo);
                        break;
                    case EnumType.STRIN:
                        TDSC tDSC = (TDSC)bStruBase;
                        Lcontent.AddRange(m_CmdBuild.LittleToBig((int)tDSC.Type));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig(tDSC.ID));
                        Lcontent.AddRange(m_CmdBuild.LittleToBig(tDSC.Length));
                        Lcontent.AddRange(System.Text.Encoding.Default.GetBytes(m_CmdBuild.StringLengthConfirm(tDSC.Value, tDSC.Length)));
                        seriNo = m_CmdBuild.GetSerialNo();
                        bsend = m_CmdBuild.SetPoint(Lcontent.ToArray(), seriNo);
                        break;
                    default:
                        break;
                }
                if (bsend != null)
                {
                    m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
                    AddDic(seriNo, tocken);
                    LogHelper.Info($"下发控制消息");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"请求控制失败：{ex.ToString()}");
            }
        }












        /// <summary>
        /// 处理队列中的消息
        /// </summary>
        /// <returns></returns>
        protected override int Run()
        {
            try
            {
                BaseMsg msg = PullMessage();
                if (msg != null)
                {
                    switch (msg.Type)
                    {
                        case MSGType.msgRequest:
                            HandleNetMsg(msg);
                            break;
                        case MSGType.msgNetConnect:
                            m_StatusBack(Sdk_StatusEvent.Connected, msg.FromIp, msg.FromPort);
                            break;
                        case MSGType.msgNetClose:
                            m_StatusBack(Sdk_StatusEvent.Closed, msg.FromIp, msg.FromPort);
                            break;
                        default:
                            break;
                    }
                }

                //判断是否心跳断开
                DateTime dtnow = System.DateTime.Now;
                if (dtnow.Ticks - m_LatestTicket < 5 * 1000L * 10000L || !m_NetMsg.m_ClientObj.bLogin)
                {
                    return 1;
                }
                m_LatestTicket = dtnow.Ticks;
                if (dtnow.Ticks - m_NetMsg.m_ClientObj.lLatestTicket > m_NetMsg.m_ClientObj.iInertval * 1000L * 10000L)
                {
                    //判断 为断开
                    //m_NetMsg.m_ClientObj.bConnected = false;
                }
            }
            catch (Exception) { }
            return 1;
        }
        /// <summary>
        /// 处理下级传来的各类消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleNetMsg(BaseMsg msg)
        {
            try
            {
                CmdDef cmdtyp = (CmdDef)(Convert.ToUInt32(msg.cmd));
                switch (cmdtyp)
                {
                    case CmdDef.LOGIN_ACK:
                        Console.WriteLine("Step 1 收到登录反馈");
                        m_NetMsg.m_ClientObj.bLogin = true;
                        m_NetMsg.m_ClientObj.lLatestTicket = DateTime.Now.Ticks;
                        HandleCuRegisterResponse(msg);
                        Timer_TimeSync.Enabled = true;
                        break;
                    case CmdDef.HEART_BEAT_ACK:
                        Console.WriteLine("收到心跳反馈");
                        HandleHeartBeatAck();
                        break;
                    case CmdDef.SET_NODES:
                        Console.WriteLine("Step2 收到系统结构回复");
                        HandleGetNodesResponse(msg);
                        break;
                    case CmdDef.SET_PROPERTY:
                        //收到请求ID属性的回复
                        Console.WriteLine("Step3 收到属性回复");
                        HandleGetAttributeResponse(msg);
                        break;
                    case CmdDef.LOGOUT_ACK:
                        //HandleLogoutAck(msg);
                        break;
                    case CmdDef.ALARM_MODE_ACK:
                        Console.WriteLine("Step4 收到设定告警回复");
                        HandleAlarmModeAck(msg);
                        break;
                    case CmdDef.DYN_ACCESS_MODE_ACK:
                        Console.WriteLine("Step5 收到设定实时数据回复");
                        HandleDYNAccessModeAck(msg);
                        break;
                    case CmdDef.SEND_ALARM:
                        //收到告警信息
                        HandleSendAlarm(msg);
                        break;
                    case CmdDef.REQ_ACK_ALARM_ACK:
                        //告警确认反馈
                        HandleAlarmConfirmAck(msg);
                        break;
                    case CmdDef.REQ_CANCEL_STATE_ACK:
                        //告警清除反馈
                        HandleAlarmClearAck(msg);
                        break;
                    case CmdDef.SET_POINT_ACK:
                        //控制点位反馈
                        HandleSetPointAck(msg);
                        break;
                    case CmdDef.REQ_SET_PROPERTY_ACK:
                        //修改配置反馈
                        HandleSetAttributeAck(msg);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理网络收到的消息异常：{ex.ToString()}");
            }
            return 1;
        }
        /// <summary>
        /// 从队列取出消息
        /// </summary>
        /// <returns></returns>
        private BaseMsg PullMessage()
        {
            try
            {
                BaseMsg msg = null;
                if (0 == m_msgList.Count)
                {
                    return null;
                }
                lock (m_listLock)
                {
                    msg = m_msgList[0];
                    m_msgList.RemoveAt(0);
                    return msg;
                }
            }
            catch (Exception) { }
            return null;
        }
        /// <summary>
        /// 添加消息到队列
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private void PushMessage(BaseMsg msg)
        {
            try
            {
                lock (m_listLock)
                {
                    m_msgList.Add(msg);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 处理心跳返回
        /// </summary>
        /// <returns></returns>
        public int HandleHeartBeatAck()
        {
            long LatestTicket = System.DateTime.Now.Ticks;
            m_NetMsg.m_ClientObj.lLatestTicket = LatestTicket;
            return 1;
        }
        /// <summary>
        /// 处理注册回复
        /// </summary>
        /// <returns>0</returns>
        private int HandleCuRegisterResponse(BaseMsg msg)
        {
            try
            {
                byte[] brec = msg.xmlbyte;
                EnumRightMode enumRightMode = (EnumRightMode)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                Console.WriteLine("收到注册反馈，权限=" + enumRightMode);
                m_DataBack(Sdk_Event.Login, (Object)enumRightMode);

                string rootId = "00000000";
                byte[] bsend = m_CmdBuild.GetNodes(rootId, m_CmdBuild.GetSerialNo());
                m_NetMsg.m_SendData(bsend, msg.FromIp, msg.FromPort);
                Console.WriteLine("Step2 请求系统结构:RootID=" + rootId);
            }
            catch (Exception) { }
            return 1;
        }
        /// <summary>
        /// 处理节点请求回复
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleGetNodesResponse(BaseMsg msg)
        {
            byte[] brec = msg.xmlbyte;
            List<int> l_requestID = new List<int>();//将需要请求的ID属性的ID加入list
            try
            {
                LogHelper.Trace($"Receive tree={BitConverter.ToString(brec)}");
                int Cnt = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                int CntReuest = 0;
                if (Cnt > 0)
                {
                    List<TNodes> l_TNodes = new List<TNodes>();
                    for (int i = 4; i < brec.Length; i += 4)
                    {
                        TNodes nodes = new TNodes();
                        nodes.NodeID = m_CmdBuild.GetNetworkToHostOrder(brec, i); i += 4;
                        LogHelper.Trace($"ID={nodes.NodeID}");
                        nodes.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec, i);
                        LogHelper.Trace($"ParentID={nodes.ParentID}");
                        l_TNodes.Add(nodes);
                        if (!l_requestID.Contains(nodes.NodeID) && m_CmdBuild.GetIDLevel(nodes.NodeID) != 1) { l_requestID.Add(nodes.NodeID); CntReuest++; }
                        if (!l_requestID.Contains(nodes.ParentID) && m_CmdBuild.GetIDLevel(nodes.ParentID) != 1) { l_requestID.Add(nodes.ParentID); CntReuest++; }
                    }
                    for (int i = 0; i < l_requestID.Count; i++)
                    {
                        LogHelper.Trace($"请求属性ID{i}={l_requestID[i]}");
                    }
                    byte[] bsend = m_CmdBuild.GetProperty(CntReuest, l_requestID, m_CmdBuild.GetSerialNo());
                    //Console.WriteLine("请求属性：cmd=" + BitConverter.ToString(bsend));
                    Console.WriteLine("Step3 请求属性");
                    LogHelper.Trace($"请求属性：cmd={BitConverter.ToString(bsend)}");
                    m_NetMsg.m_SendData(bsend, msg.FromIp, msg.FromPort);
                    //Console.WriteLine("发送请求系统属性命令:");
                }
            }
            catch (Exception) { }
            return 1;
        }

        /// <summary>
        /// 处理来自客户端的ID 属性返回
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleGetAttributeResponse(BaseMsg msg)
        {
            try
            {
                byte[] brec = msg.xmlbyte;
                //debug
                //LogHelper.Trace($"属性返回={BitConverter.ToString(brec)}");
                L_bStruBases.Clear();
                AnalysAttributeResponse(brec, ref L_bStruBases);
                if (L_bStruBases.Count > 0)
                {
                    m_DataBack(Sdk_Event.Attribute, L_bStruBases);
                    m_NetMsg.m_ClientObj.bLogin = true;
                }
                //设定告警模式
                Console.WriteLine("Step4 请求设定告警");
                List<UInt32> id = new List<UInt32>();
                id.Add(0xFFFFFFFF);//默认全部取1 表示请求所有点位的告警
                SetAlarmDataMode(0, m_EnumAlarmMode, id);
            }
            catch (Exception)
            {
                Console.WriteLine("处理来自客户端的ID 属性返回报错");
            }
            return 1;
        }
        /// <summary>
        /// 解析服务端返回的节点属性
        /// </summary>
        /// <param name="brec"></param>
        /// <param name="lattribute"></param>
        /// <returns></returns>
        private int AnalysAttributeResponse(byte[] brec, ref List<BStruBase> lattribute)
        {
            int ret = 0;
            try
            {
                int Cnt = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                if (Cnt > 0)
                {
                    for (int i = 4; i < brec.Length; i++)
                    {
                        EnumType enumType = (EnumType)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray());
                        i += 4;
                        switch (enumType)
                        {
                            case EnumType.AI:
                                TAIC taic = new TAIC();
                                taic.Type = EnumType.AI;
                                taic.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                LogHelper.Trace($"attribute taic:{taic.ID}");
                                taic.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                taic.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                taic.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                taic.MaxVal = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.AlarmLevel = (EnumAlarmLevel)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                taic.AlarmEnable = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                taic.HiLimit1 = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.LoLimit1 = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.HiLimit2 = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.LoLimit2 = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.HiLimit3 = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.LoLimit3 = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.Stander = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.Percision = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taic.Saved = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                taic.Unit = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.UNIT_LENGTH); i += CmdBuild.UNIT_LENGTH;
                                i--;
                                lattribute.Add(taic);
                                break;
                            case EnumType.AO:
                                TAOC taoc = new TAOC();
                                taoc.Type = EnumType.AO;
                                taoc.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                taoc.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                taoc.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                taoc.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                taoc.MaxVal = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taoc.MinVal = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taoc.ControlEnable = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                taoc.Stander = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taoc.Percision = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                taoc.Saved = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                taoc.Unit = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.UNIT_LENGTH); i += CmdBuild.UNIT_LENGTH;
                                i--;
                                lattribute.Add(taoc);
                                break;
                            case EnumType.DEVICE:
                                TDevice st_device = new TDevice();
                                st_device.Type = EnumType.DEVICE;
                                st_device.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                LogHelper.Trace($"attribute st_device:{st_device.ID}");
                                st_device.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                st_device.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                st_device.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                st_device.DeviceType = (EnumDeviceType)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                st_device.Productor = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                st_device.Version = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.VER_LENGTH); i += CmdBuild.VER_LENGTH;
                                TTime time = new TTime();
                                time.Years = m_CmdBuild.GetNetworkToHostOrderInt16(brec, i); i += 2;
                                time.Month = brec[i]; i++;
                                time.Day = brec[i]; i++;
                                time.Hour = brec[i]; i++;
                                time.Minute = brec[i]; i++;
                                time.Second = brec[i]; i++;
                                st_device.BeginRunTime = time;
                                i--;
                                lattribute.Add(st_device);
                                break;

                            case EnumType.DI:
                                TDIC tdic = new TDIC();
                                tdic.Type = EnumType.DI;
                                tdic.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                tdic.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                tdic.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                tdic.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdic.AlarmThresbhold = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                tdic.AlarmLevel = (EnumAlarmLevel)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                tdic.AlarmEnable = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                tdic.Desc0 = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdic.Desc1 = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdic.Saved = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                i--;
                                lattribute.Add(tdic);
                                break;
                            case EnumType.DO:
                                TDOC tdoc = new TDOC();
                                tdoc.Type = EnumType.DO;
                                tdoc.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                tdoc.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                tdoc.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                tdoc.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdoc.ControlEnable = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                tdoc.Desc0 = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdoc.Desc1 = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdoc.Saved = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                i--;
                                lattribute.Add(tdoc);
                                break;
                            case EnumType.STATION:
                                TStation s_station = new TStation();
                                s_station.Type = EnumType.STATION;
                                s_station.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                LogHelper.Trace($"attribute s_station:{s_station.ID}");
                                s_station.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                s_station.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                s_station.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                s_station.Longitude = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                s_station.Latitude = m_CmdBuild.GetNetworkToHostOrderf(brec, i).ToString(); i += 4;
                                i--;
                                lattribute.Add(s_station);
                                break;
                            case EnumType.STRIN:
                                TDSC tdsc = new TDSC();
                                tdsc.Type = EnumType.STRIN;
                                tdsc.ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                tdsc.ParentID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                tdsc.Name = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.NAMELENGTH); i += CmdBuild.NAMELENGTH;
                                tdsc.Des = System.Text.Encoding.Default.GetString(brec, i, CmdBuild.DES_LENGTH); i += CmdBuild.DES_LENGTH;
                                tdsc.AlarmEnable = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                tdsc.Saved = (EnumEnable)(m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray())); i += 4;
                                i--;
                                lattribute.Add(tdsc);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"解析属性错误：{ex}");
            }
            return ret;
        }
        /// <summary>
        /// 处理告警设定反馈
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public int HandleAlarmModeAck(BaseMsg msg)
        {
            try
            {
                byte[] brec = msg.xmlbyte;
                int GroupID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                EnumResult enumResult = (EnumResult)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(4).ToArray());
                if (enumResult == EnumResult.SUCCESS)
                {
                    Console.WriteLine("Step5 请求设定实时数据");
                    List<int> IDRequest = new List<int>();
                    //请求实时数据
                    for (int i = 0; i < L_bStruBases.Count; i++)
                    {
                        if (L_bStruBases[i].Type != EnumType.DEVICE && L_bStruBases[i].Type != EnumType.STATION)
                        {
                            if (!IDRequest.Contains(L_bStruBases[i].ID))
                                IDRequest.Add(L_bStruBases[i].ID);
                        }
                    }
                    LogHelper.Trace($"请求实时数据ID数={IDRequest.Count}");
                    SetRealTimeDataMode(0, m_EnumAccessMode, m_RealDataPolling, IDRequest);
                }
                else
                {
                    //再次请求设定告警
                    List<UInt32> id = new List<UInt32>();
                    id.Add(0xFFFFFFFF);//默认全部取1 表示请求所有点位的告警
                    SetAlarmDataMode(0, m_EnumAlarmMode, id);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理来自客户端的告警设定反馈报错:Error={ex}");
            }
            return 1;
        }
        /// <summary>
        /// 请求时钟同步
        /// </summary>
        /// <returns></returns>
        private int HandleTimeCheck()
        {
            int ret = -1;
            try
            {
                byte[] time = m_CmdBuild.PackTTime(DateTime.Now);
                if (time != null)
                {
                    byte[] bsend = m_CmdBuild.TimeCheck(time, m_CmdBuild.GetSerialNo());
                    m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
                    ret = 1;
                }
            }
            catch (Exception)
            { }
            return ret;
        }
        /// <summary>
        /// 定时向下级请求同步时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_TSync(object sender, ElapsedEventArgs e)
        {
            Timer_TimeSync.Enabled = false;
            try
            {
                HandleTimeCheck();
            }
            catch (Exception) { }

            Timer_TimeSync.Enabled = true;
        }
        /// <summary>
        /// 处理实时消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleDYNAccessModeAck(BaseMsg msg)
        {
            List<RealData> l_realDatas = new List<RealData>();
            string ip = msg.FromIp;
            byte[] brec = msg.xmlbyte;
            int id = 0x00000000;
            string svalue = "";
            float dvalue = 0;
            byte bvalue = 0;
            EnumState enumState = EnumState.NORMAL;
            RealData realData;
            try
            {
                //LogHelper.Trace($"实时数据：{BitConverter.ToString(brec)}");
                int groupid = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                EnumResult enumResult = (EnumResult)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(4).ToArray());
                int Cnt = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(8).ToArray());
                if (enumResult == EnumResult.SUCCESS)
                {
                    for (int i = 12; i < brec.Length; i++)
                    {
                        EnumType enumType = (EnumType)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray());
                        i += 4;
                        realData = new RealData();
                        switch (enumType)
                        {
                            case EnumType.AI:

                                id = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                dvalue = m_CmdBuild.GetNetworkToHostOrderf(brec, i); i += 4;
                                enumState = (EnumState)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                i--;

                                realData.ID = id;
                                realData.Value = dvalue.ToString();
                                realData.Time = DateTime.Now;
                                l_realDatas.Add(realData);
                                break;
                            case EnumType.AO:
                                id = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                dvalue = m_CmdBuild.GetNetworkToHostOrderf(brec, i); i += 4;
                                enumState = (EnumState)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                i--;
                                realData.ID = id;
                                realData.Value = dvalue.ToString();
                                realData.Time = DateTime.Now;
                                l_realDatas.Add(realData);
                                break;
                            case EnumType.DI:
                                id = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                bvalue = brec[i]; i += 1;
                                enumState = (EnumState)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                i--;
                                realData.ID = id;
                                realData.Value = bvalue.ToString();
                                realData.Time = DateTime.Now;
                                l_realDatas.Add(realData);
                                break;
                            case EnumType.DO:
                                id = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                bvalue = brec[i]; i += 1;
                                enumState = (EnumState)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                i--;
                                realData.ID = id;
                                realData.Value = bvalue.ToString();
                                realData.Time = DateTime.Now;
                                l_realDatas.Add(realData);
                                break;
                            case EnumType.STRIN:
                                id = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                int length = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(i).ToArray()); i += 4;
                                svalue = System.Text.Encoding.Default.GetString(brec, i, length); i += length;
                                i--;
                                realData.ID = id;
                                realData.Value = svalue;
                                realData.Time = DateTime.Now;
                                l_realDatas.Add(realData);
                                break;
                            default:
                                break;
                        }
                    }
                    if (l_realDatas.Count > 0)//
                    {
                        m_DataBack(Sdk_Event.RealtimeData, l_realDatas);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理来下级的实时数据内容解析报错:Error={ex}");
            }
            return 1;
        }

        /// <summary>
        /// 收到告警数据
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleSendAlarm(BaseMsg msg)
        {
            byte bfalVal = 1;//接受告警失败返回1，接受告警成功返回0
            List<AlarmData> l_alarmDatas = new List<AlarmData>();
            List<AlarmResTD> l_alarmResTDs = new List<AlarmResTD>();//记录 回馈下级
            AlarmData alarmData;
            AlarmResTD alarmResTD;
            byte[] brec = msg.xmlbyte;
            int cnt = 0;
            int tap = 1;
            string alarmStarttime = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
            try
            {
                cnt = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                if (cnt == 0 || brec.Length < 172) return 1;
                for (int i = 4; i < brec.Length; i += 168)//一个TAlarm=4+4+160
                {
                    alarmData = new AlarmData();
                    alarmResTD = new AlarmResTD();
                    alarmResTD.value = 0;//默认告警接受成功
                    alarmResTD.enumType = EnumType.DEVICE;//随便填写的
                    int k = i;
                    int ID = m_CmdBuild.GetNetworkToHostOrder(brec.Skip(k).ToArray()); k += 4;
                    alarmResTD.ID = ID;
                    EnumState enumState = (EnumState)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(k).ToArray()); k += 4;
                    alarmResTD.enumState = enumState;
                    k += 1; //1为[
                    string alarmSerialnumb = System.Text.Encoding.Default.GetString(brec, k, 6); k += 6 + tap;
                    string alarmName = System.Text.Encoding.Default.GetString(brec, k, 42); k += 42 + tap;
                    alarmStarttime = System.Text.Encoding.Default.GetString(brec, k, 19); k += 19 + tap;
                    string IDdes = System.Text.Encoding.Default.GetString(brec, k, 13); k += 13 + tap;
                    string alarmLevel = System.Text.Encoding.Default.GetString(brec, k, 6); k += 6 + tap;
                    string alarmNumb = System.Text.Encoding.Default.GetString(brec, k, 6); k += 6 + tap;
                    string alarmFlagdes = System.Text.Encoding.Default.GetString(brec, k, 6); k += 6 + tap;
                    string alaemFlagtime = System.Text.Encoding.Default.GetString(brec, k, 19); k += 19 + tap;
                    string alalmText = System.Text.Encoding.Default.GetString(brec, k, 32);
                    EnumAlarmLevel enumAlarmLevel;
                    if (alarmLevel == "紧急")
                    {
                        enumAlarmLevel = EnumAlarmLevel.FATAL;
                    }
                    else if (alarmLevel == "重要")
                    {
                        enumAlarmLevel = EnumAlarmLevel.MAIN;
                    }
                    else
                    {
                        enumAlarmLevel = EnumAlarmLevel.NORMAL; ;
                    }
                    DateTime dt = DateTime.Now;
                    alarmData.ID = ID;
                    if (!DateTime.TryParse(alaemFlagtime, out dt))
                    {
                        alarmResTD.value = bfalVal;
                        l_alarmResTDs.Add(alarmResTD);
                        LogHelper.Error($"解析出错，告警时间格式不对,ID={ID},rec or={BitConverter.ToString(brec)}");
                        continue;
                    }
                    alarmData.Time = dt;
                    string stemp = alalmText.Substring(alalmText.IndexOf('=') + 1).Trim();
                    //stemp = stemp.Remove(stemp.IndexOf(']')).Trim();
                    alarmData.Value = stemp;
                    alarmData.AlarmStatus = alarmFlagdes;
                    alarmData.AlarmLevel = enumAlarmLevel;
                    l_alarmDatas.Add(alarmData);
                    l_alarmResTDs.Add(alarmResTD);
                }
                if (l_alarmDatas.Count > 0)
                {
                    m_DataBack(Sdk_Event.AlarmData, l_alarmDatas);
                    LogHelper.Trace("上传告警数据");
                }
                if (l_alarmResTDs.Count > 0)
                {
                    //send alarm ack
                    SendAlarmAck(l_alarmResTDs);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理来下级的告警数据内容解析报错:Error={ex}");
            }
            return 1;
        }
        /// <summary>
        /// 回复下级（服务端），接受到告警
        /// </summary>
        /// <param name="alarmDatas"></param>
        /// <returns></returns>
        private int SendAlarmAck(List<AlarmResTD> alarmResTDs)
        {
            List<byte> Lcontent = new List<byte>();
            int Cnt = alarmResTDs.Count;
            try
            {
                for(int i=0;i<alarmResTDs.Count;i++)
                {
                    Lcontent.AddRange(m_CmdBuild.LittleToBig((int)alarmResTDs[i].enumType));
                    Lcontent.AddRange(m_CmdBuild.LittleToBig(alarmResTDs[i].ID));
                    Lcontent.Add(alarmResTDs[i].value);
                    Lcontent.AddRange(m_CmdBuild.LittleToBig((int)alarmResTDs[i].enumState));
                }
                byte[]bsend = m_CmdBuild.SendAlarmAck(Cnt, Lcontent.ToArray(),m_CmdBuild.GetSerialNo());
                m_NetMsg.m_SendData(bsend, m_NetMsg.m_ClientObj.ip, m_NetMsg.m_ClientObj.port);
            }
            catch (Exception ex)
            { LogHelper.Error($"回复下级收到告警 ERROR：{ex.ToString()}"); }
            return 1;
        }
        /// <summary>
        /// 处理告警确认反馈
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleAlarmConfirmAck(BaseMsg msg)
        {
            byte[] brec = msg.xmlbyte;
            int seriNo = msg.Seq;
            string stocken = "";
            try
            {
                AckResult ackResult = new AckResult();
                EnumResult enumResult = (EnumResult)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                if (DelDic(seriNo, ref stocken))
                {
                    ackResult.Tocken = stocken;
                    ackResult.enumResult = enumResult;
                    m_DataBack(Sdk_Event.ConfirmAlarmAck, ackResult);
                    LogHelper.Trace("上传告警确认响应");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理确认告警反馈异常：{ex.ToString()}");
            }
            return 1;
        }

        /// <summary>
        /// 处理告警清除反馈
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleAlarmClearAck(BaseMsg msg)
        {
            byte[] brec = msg.xmlbyte;
            int seriNo = msg.Seq;
            string stocken = "";
            try
            {
                AckResult ackResult = new AckResult();
                EnumResult enumResult = (EnumResult)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                if (DelDic(seriNo, ref stocken))
                {
                    ackResult.Tocken = stocken;
                    ackResult.enumResult = enumResult;
                    m_DataBack(Sdk_Event.ClearAlarmAck, ackResult);
                    LogHelper.Trace("上传告警清除响应");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理清除告警反馈异常：{ex.ToString()}");
            }
            return 1;
        }
        /// <summary>
        /// 处理控制命令返回值
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleSetPointAck(BaseMsg msg)
        {
            byte[] brec = msg.xmlbyte;
            int seriNo = msg.Seq;
            string stocken = "";
            try
            {
                AckResult ackResult = new AckResult();
                EnumResult enumResult = (EnumResult)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                if (DelDic(seriNo, ref stocken))
                {
                    ackResult.Tocken = stocken;
                    ackResult.enumResult = enumResult;
                    m_DataBack(Sdk_Event.ClearAlarmAck, ackResult);
                    LogHelper.Trace("上传控制命令响应");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理控制命令反馈异常：{ex.ToString()}");
            }
            return 1;
        }
        /// <summary>
        /// 处理修改配置反馈
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private int HandleSetAttributeAck(BaseMsg msg)
        {
            byte[] brec = msg.xmlbyte;
            int seriNo = msg.Seq;
            string stocken = "";
            try
            {
                AckResult ackResult = new AckResult();
                EnumResult enumResult = (EnumResult)m_CmdBuild.GetNetworkToHostOrder(brec.Skip(0).ToArray());
                if (DelDic(seriNo, ref stocken))
                {
                    ackResult.Tocken = stocken;
                    ackResult.enumResult = enumResult;
                    m_DataBack(Sdk_Event.SetAttributeAck, ackResult);
                    LogHelper.Trace("上传修改配置响应");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理修改配置反馈异常：{ex.ToString()}");
            }
            return 1;
        }
        private bool AddDic(int serino, string sidt)
        {
            bool ret = false;
            try
            {
                lock (m_dicObj)
                {
                    ret = m_DicIDSerialNo.TryAdd(serino, sidt);
                }
            }
            catch (Exception)
            { }
            return ret;
        }
        private bool DelDic(int serino, ref string sidt)
        {
            bool ret = false;
            try
            {
                ret = m_DicIDSerialNo.TryGetValue(serino, out sidt);
                lock (m_dicObj)
                {
                    m_DicIDSerialNo.Remove(serino);
                }
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
    }
}