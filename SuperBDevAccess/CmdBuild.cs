using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static SuperBDevAccess.WxBSdk;

namespace SuperBDevAccess
{
    /// <summary>
    /// 生成铁标协议命令格式
    /// 帧头+长度+报文序号+命令字+内容+CRC（长度+报文序号+命令字+内容的所有字节的CRC）
    /// </summary>
    public class CmdBuild
    {
        public const int NAMELENGTH = 40;
        public const int PASSWORDLEN = 20;
        public const int EVENT_LENGTH = 160;
        public const int DES_LENGTH = 160;
        public const int UNIT_LENGTH = 8;
        public const int VER_LENGTH = 40;
        //帧头
        private byte[] m_bHead = System.Text.Encoding.ASCII.GetBytes("#dlhjtxxy#");
        //长度
        //private int m_iLenth;
        //报文序号
        //private int m_iSerialsNo;
        //报文命令字
        private int m_iPkType;
        //CRC校验
        //private byte[] m_bCRC;
        //实体消息byte
        //private byte[] m_byte;
        //总的字节数
        //private byte[] m_bTotal;
        private static int seq = 0;//报文序号
        /// <summary>
        /// 生成登录命令
        /// </summary>
        /// <param name="user"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] Login(User user, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                byte[] content = new byte[NAMELENGTH + PASSWORDLEN];
                byte[] tpbname = System.Text.Encoding.Default.GetBytes(new string(user.username));
                byte[] tpbpassword = System.Text.Encoding.Default.GetBytes(new string(user.password));
                if (tpbname.Length < NAMELENGTH)
                {
                    Array.Copy(tpbname, content, tpbname.Length);
                    for (int i = tpbname.Length; i < NAMELENGTH; i++)
                    {
                        content[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbname, content, NAMELENGTH);
                }
                if (tpbpassword.Length < PASSWORDLEN)
                {
                    Array.Copy(tpbpassword, 0, content, NAMELENGTH, tpbpassword.Length);
                    for (int i = NAMELENGTH + tpbpassword.Length; i < NAMELENGTH + PASSWORDLEN; i++)
                    {
                        content[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbpassword, 0, content, NAMELENGTH, PASSWORDLEN);
                }
                bres = FinalSend((int)CmdDef.LOGIN, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }

        /// <summary>
        /// 生成登录响应命令
        /// </summary>
        /// <param name="user"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] LoginAck(User user, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.LOGIN_ACK, LittleToBig((int)user.Eright), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 生成登出命令
        /// </summary>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] Logout(int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                string content = "";
                bres = FinalSend((int)CmdDef.LOGOUT, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 生成登出响应命令
        /// </summary>
        /// <param name="Eresult"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] LogoutAck(string content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.LOGOUT_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 生成结构请求信息命令
        /// </summary>
        /// <param name="RootID"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] GetNodes(string RootID, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.GET_NODES, IDConvertTbyte(RootID), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 生成结构请求相应信息命令
        /// </summary>
        /// <param name="Cnt"></param>下属节点数量，可以返回0，返回-1表示无相应的ID号
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetNodes(int Cnt, List<TNodes> Tnodes, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                byte[] bcnt = LittleToBig(Cnt);
                Lcontent.AddRange(bcnt);
                for (int i = 0; i < Tnodes.Count; i++)
                {
                    byte[] temp = LittleToBig(Tnodes[i].NodeID);
                    Lcontent.AddRange(temp);
                    temp = LittleToBig(Tnodes[i].ParentID);
                    Lcontent.AddRange(temp);
                    /*byte[] temp = IDConvertTbyte(Tnodes[i].NodeID);
                    if (temp != null) Lcontent.AddRange(temp);
                    temp = IDConvertTbyte(Tnodes[i].ParentID);
                    if (temp != null) Lcontent.AddRange(temp);*/
                }
                byte[] Bcontent = Lcontent.ToArray();
                bres = FinalSend((int)CmdDef.SET_NODES, Bcontent, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        ///心跳请求
        /// </summary>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] KeepHeartBeat(string content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.HEART_BEAT, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 心跳response
        /// </summary>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] KeepHeartBeatAck(string content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.HEART_BEAT_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 请求数据属性
        /// </summary>
        /// <param name="cnt"></param>
        /// <param name="ID"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] GetProperty(int Cnt, List<int> ID, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                byte[] bcnt = LittleToBig(Cnt);
                Lcontent.AddRange(bcnt);
                for (int i = 0; i < ID.Count; i++)
                {
                    byte[] temp = LittleToBig(ID[i]);
                    if (temp != null) Lcontent.AddRange(temp);
                }
                bres = FinalSend((int)CmdDef.GET_PROPERTY, Lcontent.ToArray(), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 反馈数据属性
        /// </summary>
        /// <param name="Cnt"></param>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetProperty(int Cnt, byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                byte[] bcnt = LittleToBig(Cnt);
                byte[] bcontent = new byte[bcnt.Length + content.Length];
                Array.Copy(bcnt, 0, bcontent, 0, bcnt.Length);
                Array.Copy(content, 0, bcontent, bcnt.Length, content.Length);
                bres = FinalSend((int)CmdDef.SET_PROPERTY, bcontent, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 请求实时数据
        /// </summary>
        /// <param name="GroupID"></param>
        /// <param name="Mode"></param>
        /// <param name="PollingTime"></param>
        /// <param name="Cnt"></param>
        /// <param name="Ids"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetDYN_AccessMode(int GroupID, EnumAccessMode Mode, int PollingTime, int Cnt, List<int> Ids, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                Lcontent.AddRange(LittleToBig(GroupID));
                Lcontent.AddRange(LittleToBig((int)Mode));
                Lcontent.AddRange(LittleToBig(PollingTime));
                Lcontent.AddRange(LittleToBig(Cnt));

                for (int i = 0; i < Ids.Count; i++)
                {
                    byte[] temp = LittleToBig(Ids[i]);
                    if (temp != null) Lcontent.AddRange(temp);
                }
                bres = FinalSend((int)CmdDef.SET_DYN_ACCESS_MODE, Lcontent.ToArray(), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 请求实时数据响应
        /// </summary>
        /// <param name="GroupID"></param>
        /// <param name="Mode"></param>
        /// <param name="Cnt"></param>
        /// <param name="Ids"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] DYNAccessModeAck(int GroupID, EnumResult result, int Cnt, byte[] brec, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                Lcontent.AddRange(LittleToBig(GroupID));
                Lcontent.AddRange(LittleToBig((int)result));
                Lcontent.AddRange(LittleToBig(Cnt));
                byte[] content = new byte[Lcontent.ToArray().Length + brec.Length];
                Array.Copy(Lcontent.ToArray(), content, Lcontent.ToArray().Length);
                if (brec.Length > 0) Array.Copy(brec, 0, content, Lcontent.ToArray().Length, brec.Length);
                bres = FinalSend((int)CmdDef.DYN_ACCESS_MODE_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 请求告警信息
        /// </summary>
        /// <param name="GroupID"></param>
        /// <param name="Mode"></param>
        /// <param name="Cnt"></param>
        /// <param name="Ids"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetAlarmMode(int GroupID, EnumAlarmMode Mode, int Cnt, List<UInt32> Ids, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                Lcontent.AddRange(LittleToBig(GroupID));
                Lcontent.AddRange(LittleToBig((int)Mode));
                Lcontent.AddRange(LittleToBig(Cnt));

                for (int i = 0; i < Ids.Count; i++)
                {
                    byte[] temp = LittleToBig(Ids[i]);
                    if (temp != null) Lcontent.AddRange(temp);
                }
                bres = FinalSend((int)CmdDef.SET_ALARM_MODE, Lcontent.ToArray(), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 请求告警响应
        /// </summary>
        /// <param name="GroupID"></param>
        /// <param name="enumResult"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] AlarmModelAck(int GroupID, EnumResult enumResult, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                Lcontent.AddRange(LittleToBig(GroupID));
                Lcontent.AddRange(LittleToBig((int)enumResult));
                bres = FinalSend((int)CmdDef.ALARM_MODE_ACK, Lcontent.ToArray(), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 发送告警
        /// </summary>
        /// <param name="cnt"></param>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SendAlarm(int cnt, byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                Lcontent.AddRange(LittleToBig(cnt));
                Lcontent.AddRange(content);
                bres = FinalSend((int)CmdDef.SEND_ALARM, Lcontent.ToArray(), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 发送告警反馈
        /// </summary>
        /// <param name="cnt"></param>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SendAlarmAck(int cnt, byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                List<byte> Lcontent = new List<byte>();
                Lcontent.AddRange(LittleToBig(cnt));
                Lcontent.AddRange(content);
                bres = FinalSend((int)CmdDef.SEND_ALARM_ACK, Lcontent.ToArray(), SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 写数据值请求
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetPoint(byte[] content,int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.SET_POINT, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 写数据值请求反馈
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetPointAck(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.SET_POINT_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 时钟同步请求
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] TimeCheck(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.TIME_CHECK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 时钟同步反馈
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] TimeCheckAck(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.TIME_CHECK_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 修改配置请求
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] SetPropertyModify(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.NOTIFY_PROPERTY_MODIFY, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }

        /// <summary>
        /// 请求清除历史告警
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] ReqCanclAlarm(byte[]content,int SerialsNoRec )
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.REQ_CANCEL_STATE, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 清除历史告警反馈
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] ReqCanclAlarmAck(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.REQ_CANCEL_STATE_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }



        /// <summary>
        /// 请求确认告警
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] ReqAckAlarm(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.REQ_ACK_ALARM, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 确认告警反馈
        /// </summary>
        /// <param name="content"></param>
        /// <param name="SerialsNoRec"></param>
        /// <returns></returns>
        public byte[] ReqAckAlarmAck(byte[] content, int SerialsNoRec)
        {
            byte[] bres = null;
            try
            {
                bres = FinalSend((int)CmdDef.REQ_ACK_ALARM_ACK, content, SerialsNoRec);
            }
            catch (Exception) { }
            return bres;
        }
        public int PK_Type
        {
            get
            {
                return m_iPkType;
            }
            set
            {
                m_iPkType = value;
            }
        }
        public byte[] bPK_Type
        {
            get
            {
                return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(m_iPkType));
            }
        }
        /// <summary>
        /// 将16进制字符串ID转成4字节byte
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public byte[] IDConvertTbyte(string id)
        {
            byte[] bres = new byte[4];
            if (id.Length != 8) return null;
            try
            {
                for (int i = 0; i < bres.Length; i++)
                {
                    bres[i] = (byte)Convert.ToInt32(id.Substring(i * 2, 2), 16);
                }
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 将4字节byte的ID转成16进制的字符串
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string IDConvertTstring(byte[] id, int startindex, int length)
        {
            string res = null;
            try
            {
                for (int i = startindex; i < startindex + length; i++)
                {
                    res += (Convert.ToString(id[i], 16)).PadLeft(2, '0');
                }
                res = res.ToUpper();
            }
            catch (Exception) { }
            return res;
        }





        /// <summary>
        /// 生成最终命令：帧头+长度+报文序号+命令字+内容+CRC（长度+报文序号+命令字+内容的所有字节的CRC）
        /// </summary>
        /// <param name="pktype"></param>
        /// <param name="contant"></param>
        /// <returns></returns>
        private byte[] FinalSend(int pktype, string contant, int serialno)
        {
            byte[] bres = null;
            try
            {
                byte[] bSerialNo = LittleToBig(serialno);
                byte[] bPKType = LittleToBig(pktype);
                byte[] bContent = System.Text.Encoding.Default.GetBytes(contant);
                int len = bSerialNo.Length + bPKType.Length + bContent.Length + 4;//4是长度自己
                byte[] bLen = LittleToBig(len);
                bres = new byte[10 + len + 2];//10=帧头；2=CRC
                Array.Copy(bLen, 0, bres, 10, bLen.Length);
                Array.Copy(bSerialNo, 0, bres, 10 + bLen.Length, bSerialNo.Length);
                Array.Copy(bPKType, 0, bres, 10 + bLen.Length + bSerialNo.Length, bPKType.Length);
                Array.Copy(bContent, 0, bres, 10 + bLen.Length + bSerialNo.Length + bPKType.Length, bContent.Length);
                byte bh; byte bl;
                Check.CalculateCrc16(bres, 10, bres.Length - 12, out bh, out bl);
                Array.Copy(m_bHead, 0, bres, 0, m_bHead.Length);
                bres[bres.Length - 2] = bh;
                bres[bres.Length - 1] = bl;
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 生成最终命令：帧头+长度+报文序号+命令字+内容+CRC（长度+报文序号+命令字+内容的所有字节的CRC）
        /// </summary>
        /// <param name="pktype"></param>
        /// <param name="contant"></param>
        /// <returns></returns>
        private byte[] FinalSend(int pktype, byte[] bContent, int serialno)
        {
            byte[] bres = null;
            try
            {
                byte[] bSerialNo = LittleToBig(serialno);
                byte[] bPKType = LittleToBig(pktype);
                //byte[] bContent = strToToHexByte(string.Format("{0:X8}", IPAddress.NetworkToHostOrder(contant)));
                int len = bSerialNo.Length + bPKType.Length + bContent.Length + 4;
                byte[] bLen = LittleToBig(len);
                bres = new byte[10 + len + 2];//10=帧头；2=CRC
                Array.Copy(bLen, 0, bres, 10, bLen.Length);
                Array.Copy(bSerialNo, 0, bres, 10 + bLen.Length, bSerialNo.Length);
                Array.Copy(bPKType, 0, bres, 10 + bLen.Length + bSerialNo.Length, bPKType.Length);
                Array.Copy(bContent, 0, bres, 10 + bLen.Length + bSerialNo.Length + bPKType.Length, bContent.Length);
                byte bh; byte bl;
                Check.CalculateCrc16(bres, 10, bres.Length - 12, out bh, out bl);
                Array.Copy(m_bHead, 0, bres, 0, m_bHead.Length);
                bres[bres.Length - 2] = bh;
                bres[bres.Length - 1] = bl;
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 字符串转16进制字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
        /// <summary>
        /// 将主机字节序转成网络字节序,小端转大端
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] LittleToBig(int value)
        {
            byte[] b = null;
            try
            {
                value = IPAddress.HostToNetworkOrder(value);
                b = BitConverter.GetBytes(value);
            }
            catch (Exception)
            { }
            return b;
        }
        /// <summary>
        /// 将主机字节序转成网络字节序,小端转大端
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] LittleToBig(float value)
        {
            byte[] b = new byte[4];
            try
            {
                byte[] btemp = BitConverter.GetBytes(value);
                for (int i = 0; i < btemp.Length; i++)
                {
                    b[i] = btemp[3 - i];
                }
            }
            catch (Exception)
            { b = null; }
            return b;
        }
        /// <summary>
        /// 将主机字节序转成网络字节序,小端转大端
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] LittleToBig(Int16 value)
        {
            byte[] b = new byte[2];
            try
            {
                byte[] btemp = BitConverter.GetBytes(value);
                for (int i = 0; i < 2; i++)
                {
                    b[i] = btemp[1 - i];
                }
            }
            catch (Exception)
            { b = null; }
            return b;
        }
        private List<byte> m_listdata = new List<byte>();
        public int MessageCut(byte[] bd)
        {
            int ret = -1;
            try
            {
                m_listdata.AddRange(bd);
                if (m_listdata.Count < 12)
                {
                    return -1;
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
                                return -1;
                            }
                            else
                            {
                                int lenth = GetNetworkToHostOrder(m_listdata.Skip(10).ToArray());
                                if (m_listdata.Count < 12 + lenth)
                                {
                                    return -1;
                                }
                                else
                                {
                                    byte[] bt = new byte[lenth + 12];
                                    m_listdata.CopyTo(0, bt, 0, lenth + 12);
                                    m_listdata.RemoveRange(0, lenth + 12);

                                }
                            }

                        }
                    }
                }
            }
            catch (Exception) { }
            return ret;
        }
        /// <summary>
        /// 解析接受到的铁标规则字节
        /// </summary>
        /// <param name="bd"></param>
        /// <param name="lenth"></param>
        /// <param name="serialno"></param>
        /// <param name="pktype"></param>
        /// <param name="content"></param>
        /// <param name="bcontent"></param>
        /// <returns></returns>
        public int MessageAnalysis(byte[] bd, ref int lenth, ref int serialno, ref int pktype, ref string content, ref byte[] bcontent)
        {
            int ret = -1;
            try
            {
                //check crc
                if (bd.Length < 10 + 12 + 2)
                {
                    return -1;
                }
                else
                {
                    lenth = GetNetworkToHostOrder(bd.Skip(10).ToArray());
                    serialno = GetNetworkToHostOrder(bd.Skip(14).ToArray());
                    pktype = GetNetworkToHostOrder(bd.Skip(18).ToArray());

                    byte bh; byte bl;
                    Check.CalculateCrc16(bd, 10, lenth, out bh, out bl);
                    if (bd[bd.Length - 2] == bh && bd[bd.Length - 1] == bl)
                    {
                        bcontent = new byte[lenth - 12];
                        Array.Copy(bd, 10 + 12, bcontent, 0, lenth - 12);
                        content = System.Text.Encoding.Default.GetString(bd, 10 + 12, lenth - 12);
                        ret = 1;
                    }
                }

            }
            catch (Exception) { }
            return ret;
        }
        /// <summary>
        /// 将网络字节序转成主机字节序,大端转小端 整数4字节
        /// </summary>
        /// <param name="bvalue"></param>
        /// <returns></returns>
        public int GetNetworkToHostOrder(byte[] bvalue)
        {
            int ret = -1;
            try
            {
                ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bvalue, 0));
            }
            catch (Exception)
            {

            }
            return ret;
        }
        /// <summary>
        /// 将网络字节序转成主机字节序,大端转小端 整数4字节
        /// </summary>
        /// <param name="bvalue"></param>
        /// <returns></returns>
        public int GetNetworkToHostOrder(byte[] bvalue, int startindex)
        {
            int ret = -1;
            try
            {
                ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bvalue, startindex));
            }
            catch (Exception)
            {

            }
            return ret;
        }
        /// <summary>
        /// 将网络字节序转成主机字节序,大端转小端 整数2字节
        /// </summary>
        /// <param name="bvalue"></param>
        /// <returns></returns>
        public Int16 GetNetworkToHostOrderInt16(byte[] bvalue, int startindex)
        {
            Int16 ret = -1;
            try
            {
                byte[] bres = new byte[2];
                for (int i = 0; i < 2; i++)
                {
                    bres[i] = bvalue[startindex + 1 - i];
                }
                ret = BitConverter.ToInt16(bres, 0);
            }
            catch (Exception)
            {

            }
            return ret;
        }
        /// <summary>
        /// 将网络字节序转成主机字节序,大端转小端 浮点数
        /// </summary>
        /// <param name="bvalue"></param>
        /// <returns></returns>
        public float GetNetworkToHostOrderf(byte[] bvalue, int startindex)
        {
            float ret = -1;
            try
            {
                byte[] btemp = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    btemp[i] = bvalue[3 + startindex - i];
                }
                ret = BitConverter.ToSingle(btemp, 0);
            }
            catch (Exception)
            {

            }
            return ret;
        }
        /// <summary>
        /// 将名字按照定长要求补全，并转成对应的字节数组
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] Name(string name)
        {
            byte[] bres = null;
            try
            {
                /*if (name.Length > NAMELENGTH)
                {
                    name = name.Substring(0, NAMELENGTH);
                }
                else { name = name.PadRight(NAMELENGTH); }
                bres = System.Text.Encoding.Default.GetBytes(name);*/
                bres = new byte[NAMELENGTH];
                byte[] tpbres = System.Text.Encoding.Default.GetBytes(name);
                if (tpbres.Length < NAMELENGTH)
                {
                    Array.Copy(tpbres, bres, tpbres.Length);
                    for (int i = tpbres.Length; i < NAMELENGTH; i++)
                    {
                        bres[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbres, bres, NAMELENGTH);
                }
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 将描述按照定长要求补全，并转成对应的字节数组
        /// </summary>
        /// <param name="des"></param>
        /// <returns></returns>
        public byte[] Des(string des)
        {
            byte[] bres = null;
            try
            {
                /*if (des.Length > DES_LENGTH)
                {
                    des = des.Substring(0, DES_LENGTH);
                }
                else { des = des.PadRight(DES_LENGTH); }
                bres = System.Text.Encoding.Default.GetBytes(des);*/
                bres = new byte[DES_LENGTH];
                byte[] tpbres = System.Text.Encoding.Default.GetBytes(des);
                if (tpbres.Length < DES_LENGTH)
                {
                    Array.Copy(tpbres, bres, tpbres.Length);
                    for (int i = tpbres.Length; i < DES_LENGTH; i++)
                    {
                        bres[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbres, bres, DES_LENGTH);
                }
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 将版本按照定长要求补全，并转成对应的字节数组
        /// </summary>
        /// <param name="ver"></param>
        /// <returns></returns>
        public byte[] Ver(string ver)
        {
            byte[] bres = null;
            try
            {
                /*if (des.Length > DES_LENGTH)
                {
                    des = des.Substring(0, DES_LENGTH);
                }
                else { des = des.PadRight(DES_LENGTH); }
                bres = System.Text.Encoding.Default.GetBytes(des);*/
                bres = new byte[VER_LENGTH];
                byte[] tpbres = System.Text.Encoding.Default.GetBytes(ver);
                if (tpbres.Length < VER_LENGTH)
                {
                    Array.Copy(tpbres, bres, tpbres.Length);
                    for (int i = tpbres.Length; i < VER_LENGTH; i++)
                    {
                        bres[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbres, bres, VER_LENGTH);
                }
            }
            catch (Exception) { }

            return bres;
        }
        /// <summary>
        /// 将字符串修改成相应要求长度的字节,再转出对应的字符串
        /// </summary>
        /// <param name="tobeconvert"></param>
        /// <returns></returns>
        public string StringLengthConfirm(string tobeconvert, int lentdemand)
        {
            string sres = "";
            byte[] bres = null;
            try
            {
                bres = new byte[lentdemand];
                byte[] tpbres = System.Text.Encoding.Default.GetBytes(tobeconvert);
                if (tpbres.Length < lentdemand)
                {
                    Array.Copy(tpbres, bres, tpbres.Length);
                    for (int i = tpbres.Length; i < lentdemand; i++)
                    {
                        bres[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbres, bres, lentdemand);
                }
                sres = System.Text.Encoding.Default.GetString(bres);
            }
            catch (Exception) { }
            return sres;
        }
        /// <summary>
        /// 将Unit(单位)按照定长要求补全，并转成对应的字节数组
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public byte[] Unit(string unit)
        {
            byte[] bres = null;
            try
            {
                /*if (unit.Length > UNIT_LENGTH)
                {
                    unit = unit.Substring(0, UNIT_LENGTH);
                }
                else { unit = unit.PadRight(UNIT_LENGTH); }
                bres = System.Text.Encoding.Default.GetBytes(unit);*/
                bres = new byte[UNIT_LENGTH];
                byte[] tpbres = System.Text.Encoding.Default.GetBytes(unit);
                if (tpbres.Length < UNIT_LENGTH)
                {
                    Array.Copy(tpbres, bres, tpbres.Length);
                    for (int i = tpbres.Length; i < UNIT_LENGTH; i++)
                    {
                        bres[i] = 0x20;
                    }
                }
                else
                {
                    Array.Copy(tpbres, bres, UNIT_LENGTH);
                }
            }
            catch (Exception) { }
            return bres;
        }
        /// <summary>
        /// 获取站号，即ID的前5位
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetStationID(int id)
        {
            int ret = 0x00000000;
            try
            {
                string sid = Convert.ToString(id, 2);
                sid = sid.PadLeft(32, '0');
                string newid = (sid.Substring(0, 5)).PadRight(32, '0');
                ret = Convert.ToInt32(newid, 2);
            }
            catch (Exception) { }
            return ret;
        }
        /// <summary>
        /// 判断该ID的DDD字段是否符合标准；
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ID_DDDconfirm(int id, string sDDD)
        {
            bool b = false;
            try
            {
                string sid = Convert.ToString(id, 2);
                sid = sid.PadLeft(32, '0');
                if (sid.Substring(21) == sDDD)
                {
                    b = true;
                }
            }
            catch (Exception) { }
            return b;
        }
        /// <summary>
        /// 将ID的DDD位置0
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetAABBBCC(int id)
        {
            int ret = 0x00000000;
            try
            {
                string sid = Convert.ToString(id, 2);
                sid = sid.PadLeft(32, '0');
                string newid = (sid.Substring(0, 21)).PadRight(32, '0');
                ret = Convert.ToInt32(newid, 2);
            }
            catch (Exception) { }
            return ret;
        }
        /// <summary>
        /// 将ID转成告警上报用的格式即AA.BBB.CC.DDD
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetAlarmID(int id)
        {
            string ret = "00.000.00.000";
            try
            {
                string sid = Convert.ToString(id, 2);
                sid = sid.PadLeft(32, '0');
                string newid = (sid.Substring(0, 21)).PadRight(32, '0');
                string AA = Convert.ToInt32(sid.Substring(0, 5), 2).ToString().PadLeft(2, '0');
                //AA = AA.Length == 2 ? AA : AA.Substring(AA.Length - 2, 2);
                string BBB = Convert.ToInt32(sid.Substring(5, 10), 2).ToString().PadLeft(3, '0');
                // BBB = BBB.Length == 3 ? BBB : BBB.Substring(BBB.Length - 3, 3);
                string CC = Convert.ToInt32(sid.Substring(15, 6), 2).ToString().PadLeft(2, '0');
                //CC = CC.Length == 2 ? CC : CC.Substring(CC.Length - 2, 2);
                string DDD = Convert.ToInt32(sid.Substring(21, 11), 2).ToString().PadLeft(3, '0');
                ret = AA + "." + BBB + "." + CC + "." + DDD;
                ret = AA + BBB + CC + DDD;
            }
            catch (Exception) { }
            return ret;
        }
        /// <summary>
        /// 生成告警描述部分
        /// </summary>
        /// <param name="alarmSerialNum"></param>
        /// <param name="Name"></param>
        /// <param name="dtAlarmtimeStart"></param>
        /// <param name="ID"></param>
        /// <param name="enumalarmlevel"></param>
        /// <param name="arlarmNum"></param>
        /// <param name="alarmflagDes"></param>
        /// <param name="dtalarmflagtime"></param>
        /// <param name="alarmDes"></param>
        /// <returns></returns>
        public bool AlarmDesGen(int alarmSerialNum, string Name, DateTime dtAlarmtimeStart, int ID, EnumAlarmLevel enumalarmlevel, int arlarmNum, EnumAlarmflagDes alarmflagDes, DateTime dtalarmflagtime, string alarmDes, ref byte[] bres)
        {
            string nouse = " ";
            string tap = "  ";
            bool bflag = false;
            //if (bres.Length != EVENT_LENGTH) return bflag;
            bres = new byte[EVENT_LENGTH];
            try
            {
                string salarmSerialNum = alarmSerialNum.ToString();
                if (salarmSerialNum.Length > 6) return bflag;
                salarmSerialNum = salarmSerialNum.PadLeft(6, '0');
                string sName = StringLengthConfirm(Name, 42);
                string sAlarmtimeStart = dtAlarmtimeStart.ToString("yyyy-MM-dd HH:mm:ss");
                string sid = GetAlarmID(ID);
                string sAlarmlevel = "";
                switch (enumalarmlevel)
                {
                    case EnumAlarmLevel.FATAL:
                        sAlarmlevel = "紧急";
                        break;
                    case EnumAlarmLevel.MAIN:
                        sAlarmlevel = "重要";
                        break;
                    case EnumAlarmLevel.NORMAL:
                        sAlarmlevel = "一般";
                        break;
                    default:
                        return bflag;
                }
                string sarlarmNum = arlarmNum.ToString();
                if (sarlarmNum.Length > 6) return bflag;
                sarlarmNum = sarlarmNum.PadLeft(6, '0');
                string salarmflagDes = "";
                switch (alarmflagDes)
                {
                    case EnumAlarmflagDes.START:
                        salarmflagDes = "开始";
                        break;
                    case EnumAlarmflagDes.OVER:
                        salarmflagDes = "结束";
                        break;
                    case EnumAlarmflagDes.CONFIRM:
                        salarmflagDes = "确认";
                        break;
                    case EnumAlarmflagDes.CLEAR:
                        salarmflagDes = "清除";
                        break;
                    default:
                        return bflag;
                }

                string sdtalarmflagtime = nouse;
                if (dtalarmflagtime != null)
                {
                    sdtalarmflagtime = dtalarmflagtime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                alarmDes = "值=" + alarmDes;
                string salarmDes = StringLengthConfirm(alarmDes, 32);
                string res = "[" + salarmSerialNum + tap
                    + sName + tap
                    + sAlarmtimeStart + tap
                    + sid + tap
                    + sAlarmlevel + tap
                    + sarlarmNum + tap
                    + salarmflagDes + tap
                    + sdtalarmflagtime + tap
                    + salarmDes + "]";
                res = StringLengthConfirm(res, 160);
                bres = System.Text.Encoding.Default.GetBytes(res);
                bflag = true;
            }
            catch (Exception) { }
            return bflag;
        }
    
        public int GetSerialNo()
        {
            try
            {
                seq++;
            }
            catch (Exception) { seq = 0; }
            return seq;
        }

        /// <summary>
        /// 判断ID 为 区中心（1） 还是 站（2） 还是 设备（3） 还是 采集点（4）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetIDLevel(int id)
        {
            int ret = -1;
            string def = "0";
            try
            {
                string sid = Convert.ToString(id, 2);
                sid = sid.PadLeft(32, '0');
                if(sid.Substring(5)==def.PadRight(27,'0'))
                {
                    def = "0";
                    ret = 1;
                }
                else if (sid.Substring(15) == def.PadRight(17, '0'))
                {
                    def = "0";
                    ret = 2;
                }
                else if (sid.Substring(21) == def.PadRight(11, '0'))
                {
                    def = "0";
                    ret = 3;
                }
                else
                {
                    ret = 4;
                }
            }
            catch(Exception)
            { }
            return ret;
        }

        public byte[] PackTTime(DateTime dt)
        {
            byte[] bres = null;
            List<byte> ls = new List<byte>();
            try
            {
                TTime time = new TTime();
                time.Years = (Int16)dt.Year;
                time.Month = (byte)dt.Month;
                time.Day = (byte)dt.Day;
                time.Hour = (byte)dt.Hour;
                time.Minute = (byte)dt.Minute;
                time.Second = (byte)dt.Second;
                ls.AddRange(LittleToBig(time.Years));
                ls.Add(time.Month);
                ls.Add(time.Day);
                ls.Add(time.Hour);
                ls.Add(time.Minute);
                ls.Add(time.Second);
                bres = ls.ToArray();
            }
            catch (Exception)
            { }
            return bres;
        }
    }
}
