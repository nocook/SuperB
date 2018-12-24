using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SuperBDevAccess
{
    public class WxBSdk
    {
        static ConcurrentDictionary<int, BSDK> _sdkList = new ConcurrentDictionary<int, BSDK>();

        public delegate Task DataEventHandle(int lUser, Sdk_Event cmd, params object[] objs);
        public static event DataEventHandle DataCallback;

        public delegate void DisconnectHandle(int lUser);
        public static event DisconnectHandle Disconnected;

        public delegate void ConnectHandle(int lUser);
        public static event ConnectHandle Connected;

        public static int SuperB_Login(string ip, int port,
            string sUserName, string sPassword,
            ref SuperB_PlateInfo lpDeviceInfo)
        {
            int userIndex = -1;
            BSDK tmp = new BSDK();
            tmp.Initial(ip, port, new User()
            {
                username = sUserName.ToCharArray(),
                password = sPassword.ToCharArray(),
                Eright = EnumRightMode.LEVEL2,
            });

            if (_sdkList.TryAdd(_sdkList.Count, tmp))
            {
                userIndex = _sdkList.Count - 1;
                tmp.SetCallBackFun(
                    (cmd, objs) => DataCallback(userIndex, cmd, objs),
                    (cmd, objs) =>
                    {
                        if (cmd == Sdk_StatusEvent.Closed)
                            Disconnected.Invoke(userIndex);
                        else Connected.Invoke(userIndex);
                    });
                tmp.StartWork();
            }

            return userIndex;
        }

        public static bool SuperB_Logout(int lUserID)
        {
            if (_sdkList.TryRemove(lUserID, out BSDK bSDK))
            {
                bSDK.StopWork();
                return true;
            }

            return false;
        }
        /// <summary>
        /// 设定心跳间隔
        /// </summary>
        /// <param name="lUserID"></param>
        /// <param name="time">单位s</param>
        public void SetHearbeatInterval(int lUserID, int time)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetHearbeatInterval(time);
            }
        }
        /// <summary>
        /// 请求时间同步
        /// </summary>
        public void SetTimeSync(int lUserID)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetTimeSync();
            }
        }
        /// <summary>
        /// 设定请求实时数据
        /// </summary>
        /// <param name="GroupID">不知道作用，开放出来，暂时固定给0</param>
        /// <param name="enumAccessMode">实时数据的上报方式</param>
        /// <param name="PollingTime">定时方式时，上报的时间间隔</param>
        /// <param name="id">需要请求的ID，这个ID可以是具体采集点ID，或者 设备ID（但是设备ID的DDD字段全部置0，表示请求该设备下的所有采集点）</param>
        public void SetRealTimeDataMode(int lUserID, int GroupID, EnumAccessMode enumAccessMode, int PollingTime, List<int> id)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetRealTimeDataMode(GroupID, enumAccessMode, PollingTime, id);
            }
        }
        /// <summary>
        /// 设定告警上报模式
        /// </summary>
        /// <param name="GroupID">不知道作用，开放出来，暂时固定给0</param>
        /// <param name="enumAlarmMode">选择等级上报告警</param>
        /// <param name="id">需要上报告警的ID，若要全部数据上报告警，取全1</param>
        public void SetAlarmDataMode(int lUserID, int GroupID, EnumAlarmMode enumAlarmMode, List<UInt32> id)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetAlarmDataMode(GroupID, enumAlarmMode, id);
            }
        }

        /// <summary>
        /// 确认告警
        /// </summary>
        /// <param name="ID">需要确认告警的采集点的ID</param>
        /// <param name="alarmtime">该告警产生的时间</param>
        public void SetAlarmConfirm(int lUserID, int id, string stocken, DateTime dt)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetAlarmConfirm(id, dt, stocken);
            }
        }
        /// <summary>
        /// 清除告警
        /// </summary>
        /// <param name="ID">需要清除告警的采集点的ID</param>
        /// <param name="alarmtime">该告警产生的时间</param>
        public void SetAlarmClear(int lUserID, int ID, string stocken, DateTime dt)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetAlarmClear(ID, dt, stocken);
            }
        }

        /// <summary>
        /// 请求控制指令
        /// </summary>
        /// <param name="bStruBase">必要值，type id value,有length传length</param>
        public void SetControl(int lUserID, BStruBase bStruBase, string stocken)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetControl(bStruBase, stocken);
            }
        }
        /// <summary>
        /// 请求修改配置属性
        /// </summary>
        public void SetAttribute(int lUserID, BStruBase bStruBase, string stocken)
        {
            if (_sdkList.TryGetValue(lUserID, out BSDK bSDK))
            {
                bSDK.SetAttribute(bStruBase, stocken);
            }
        }
        /// <summary>
        /// 控制命令 返回结构体
        /// </summary>
        public class AckResult
        {
            public string Tocken { get; set; }
            public EnumResult enumResult { get; set; }

        }
        /// <summary>
        /// 告警数据结构
        /// </summary>
        public class AlarmData
        {
            public int ID { get; set; }
            public DateTime Time { get; set; }
            public string Value { get; set; }
            /// <summary>
            /// 开始，结束，清除，确认
            /// </summary>
            public string AlarmStatus { get; set; }

            public EnumAlarmLevel AlarmLevel { get; set; }
        }
        /// <summary>
        /// 告警回复结构体
        /// </summary>
        public class AlarmResTD
        {
            public EnumType enumType;
            public int ID;
            public byte value;
            public EnumState enumState;
        }
        /// <summary>
        /// 实时数据结构
        /// </summary>
        public class RealData
        {
            public int ID { get; set; }
            public DateTime Time { get; set; }
            public string Value { get; set; }
            public AlarmData Data { get; set; }
        }

        public class TAIC : BStruBase
        {
            //attribute
            public string MaxVal { get; set; }
            public EnumAlarmLevel AlarmLevel { get; set; }
            public EnumEnable AlarmEnable { get; set; }
            public string MinVal { get; set; }
            public string HiLimit1 { get; set; }
            public string LoLimit1 { get; set; }
            public string HiLimit2 { get; set; }
            public string LoLimit2 { get; set; }
            public string HiLimit3 { get; set; }
            public string LoLimit3 { get; set; }
            public string Stander { get; set; }
            public string Percision { get; set; }
            public EnumEnable Saved { get; set; }
            public string Unit { get; set; }

            //value
            public EnumState Status { get; set; }
            public float Value { get; set; }
        }
        public class TAOC : BStruBase
        {
            //attribute
            public string MaxVal { get; set; }
            public string MinVal { get; set; }
            public EnumEnable ControlEnable { get; set; }
            public string Stander { get; set; }
            public string Percision { get; set; }
            public EnumEnable Saved { get; set; }
            public string Unit { get; set; }

            //value
            public EnumState Status { get; set; }
            public float Value { get; set; }

        }

        public class TDIC : BStruBase
        {
            //attribute
            public EnumEnable AlarmThresbhold { get; set; }
            public EnumAlarmLevel AlarmLevel { get; set; }
            public EnumEnable AlarmEnable { get; set; }
            public string Desc0 { get; set; }
            public string Desc1 { get; set; }
            public EnumEnable Saved { get; set; }


            //value
            public EnumState Status { get; set; }
            public byte Value { get; set; }
        }

        public class TDOC : BStruBase
        {
            //attribute
            public EnumEnable ControlEnable { get; set; }
            public string Desc0 { get; set; }
            public string Desc1 { get; set; }
            public EnumEnable Saved { get; set; }



            //value
            public EnumState Status { get; set; }
            public byte Value { get; set; }
        }
        public class TDSC : BStruBase
        {
            //attribute
            public EnumEnable AlarmEnable { get; set; }
            public EnumEnable Saved { get; set; }


            //value
            public Int32 Length { get; set; }
            public string Value { get; set; }
        }
        public class TStation : BStruBase
        {
            public string Longitude { get; set; }
            public string Latitude { get; set; }
        }
        public class TDevice : BStruBase
        {
            public EnumDeviceType DeviceType { get; set; }
            public string Productor { get; set; }
            public string Version { get; set; }
            public TTime BeginRunTime { get; set; }
        }
        public class BStruBase
        {
            public EnumType Type { get; set; }
            public int ID { get; set; }
            public int ParentID { get; set; }
            public string Name { get; set; }
            public string Des { get; set; }
        }
        public class BDataStruBase
        {
            public EnumType Type { get; set; }
            public int ID { get; set; }
        }

        public class User
        {
            public char[] username { get; set; }
            public char[] password { get; set; }
            public EnumRightMode Eright { get; set; }
        }

        public class TNodes
        {
            public int NodeID { get; set; }
            public int ParentID { get; set; }
        }

        public class TTime
        {
            public Int16 Years { get; set; }
            public byte Month { get; set; }
            public byte Day { get; set; }
            public byte Hour { get; set; }
            public byte Minute { get; set; }
            public byte Second { get; set; }
        }

        public enum Sdk_Event
        {
            Login = 0,//登录事件
            Logout,//登出事件
            Attribute,//获取节点属性事件
            RealtimeData,//实时数据事件
            AlarmData,//告警数据事件
            ConfirmAlarmAck,//告警确认反馈事件
            ClearAlarmAck,//告警清除反馈事件
            SetPointAck,//控制命令反馈 
            SetAttributeAck,//修改配置反馈
        }

        public enum Sdk_StatusEvent
        {
            Closed = 0,//断线事件
            Connected//链接事件
        }

        public enum CmdDef
        {
            // B接口命令
            LOGIN = 101,
            LOGIN_ACK,
            LOGOUT,
            LOGOUT_ACK,
            GET_NODES = 201,
            SET_NODES,
            GET_SUBSTRUCT,
            SET_SUBSTRUCT,
            GET_PROPERTY = 301,
            SET_PROPERTY,
            SET_DYN_ACCESS_MODE = 401,
            DYN_ACCESS_MODE_ACK,
            SET_ALARM_MODE = 501,
            ALARM_MODE_ACK,
            SEND_ALARM,
            SEND_ALARM_ACK,
            GET_ACTIVE_ALARM,
            SET_ACTIVE_ALARM,
            GET_DATA_HISTORY = 601,
            SET_DATA_HISTORY,
            GET_LOG_HISTORY = 701,
            SET_LOG_HISTORY,
            GET_OPERATION_HISTORY = 801,
            SET_OPERATION_HISTORY,
            GET_ALARM_HISTORY = 901,
            SET_ALARM_HISTORY,
            SET_POINT = 1001,
            SET_POINT_ACK,
            REQ_MODIFY_PASSWORD = 1101,
            MODIFY_PASSWORD_ACK,
            HEART_BEAT = 1201,
            HEART_BEAT_ACK,
            TIME_CHECK = 1301,
            TIME_CHECK_ACK,
            REQ_SET_PROPERTY = 1401,
            REQ_SET_PROPERTY_ACK,
            NOTIFY_PROPERTY_MODIFY = 1501,
            PROPERTY_MODIFY_ACK,
            REQ_ACK_ALARM = 1601,
            REQ_ACK_ALARM_ACK,
            REQ_CANCEL_STATE = 1701,
            REQ_CANCEL_STATE_ACK,

            //inner 自定义命令
            InitAllDbData = 100000,
            KeepHeartBeat = 100001,
        }

        public enum EnumRightMode
        {
            INVALID = 0,
            LEVEL1 = 1,
            LEVEL2 = 2,
        }

        public enum EnumResult
        {
            FAILURE = 0,
            SUCCESS = 1,
        }

        public enum EnumType
        {
            STATION = 0,
            DEVICE = 1,
            DI = 2,
            AI = 3,
            DO = 4,
            AO = 5,
            STRIN = 6,
        }

        public enum EnumAlarmLevel
        {
            NOALARM = 0,//没有告警
            FATAL = 1,//紧急
            MAIN = 2,//重要
            NORMAL = 3,//一般
        }

        public enum EnumEnable
        {
            DISABLE = 0,
            ENABLE = 1,
        }

        public enum EnumDeviceType
        {
            HI_DISTRIBUTER = 0,
            LO_DISTRIBUTER,
            DIESEL_GENERATOR,
            GAS_GENERATOR,
            UPS,
            DC_AC,
            RECTIFIER,
            SOLAR,
            DC_DC,
            WIND_GENERATOR,
            BATTERY,
            LOCAL_AIRCONDITION1,
            LOCAL_AIRCONDITION2,
            DOOR_FORCE,
            ENVIORMENT,
            LIGHTNINGPROOF
        }

        public enum EnumAccessMode
        {
            ASK_ANSWER = 0,
            CHANGE_TRIGGER,
            TIME_TRIGGER,
            STOP,
        }

        public enum EnumState
        {
            NOALARM = 0,
            FATAL,
            MAIN,
            NORMAL,
        }

        public enum EnumAlarmMode
        {
            NOALARM = 0,
            CRITICAL,
            MAJOR,
            MINOR,
        }

        public enum EnumAlarmflagDes
        {
            START = 0,
            OVER,
            CLEAR,
            CONFIRM,
        }

        public struct SuperB_PlateInfo
        {
        }
    }
}
