using MqttCommon.Setup;
using SuperBMgr.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SuperBDevAccess.WxBSdk;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr.transfer
{
    abstract class SuperBTagBase 
    {
        public List<SqlAlarmRes> AlarmRess { get; protected set; }
        public TagRes Tag { get; protected set; }
        public BStruBase OrgiData { get; protected set; }
        public string DeviceIsid { get; set; }
        public EnumType TagType { get; set; }

        public SuperBTagBase(BStruBase data, string devIsid, SqlAlarmMap[] alarmMaps)
        {
            AlarmRess = new List<SqlAlarmRes>();
            OrgiData = data;
            DeviceIsid = devIsid;

            HandleTag(data, null);
            HandleAlarms(data, alarmMaps);
        }

        public abstract bool Update(BStruBase tagbase);

        public virtual bool UpdateTag(TagRes tag)
        {
            if (Tag.tagIsid != tag.tagIsid) return false;

            Tag = tag;
            if (tag.tagName != OrgiData.Name)
            {
                OrgiData.Name = tag.tagName;
                return true;
            }
            return false;
        }

        public bool IsBTag(int objId) => OrgiData.ID == objId;
        public bool IsTag(string tagIsid) => Tag.tagIsid == tagIsid;

        protected abstract void HandleTag(BStruBase data, string tagIsid);
        protected abstract void HandleAlarms(BStruBase data, SqlAlarmMap[] alarmMaps);

        public static TTime Transfer(DateTime dt)
        {
            return new TTime()
            {
                Day = (byte)dt.Day,
                Hour = (byte)dt.Hour,
                Month = (byte)dt.Month,
                Years = (short)dt.Year,
                Minute = (byte)dt.Minute,
                Second = (byte)dt.Second,
            };
        }

        public static DateTime Transfer(TTime ttime)
        {
            return new DateTime(ttime.Years, ttime.Month, ttime.Day, ttime.Hour, ttime.Minute, ttime.Second);
        }
    }

}
