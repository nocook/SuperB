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
    class SuperB_Tdsc : SuperBTagBase
    {
        public SqlTdsc DbData { get; set; }

        public SuperB_Tdsc(TDSC data, string tagIsid, SqlAlarmMap[] alarmMaps)
            : base(data, tagIsid, alarmMaps)
        {
            TagType = EnumType.STRIN;
        }

        public override bool Update(BStruBase tagbase)
        {
            if (!(tagbase is TDSC tdsc)) return false;
            if (!(OrgiData is TDSC orgTdsc)) return false;

            if (orgTdsc == null)
            {
                OrgiData = tdsc;
                HandleTag(tdsc, null);
                return true;
            }

            if (tdsc.ID != orgTdsc.ID) return false;

            bool isUpdated = false;
            if (orgTdsc.Name != tdsc.Name)
            {
                orgTdsc.Name = tdsc.Name;
                DbData.objName = tdsc.Name;
                isUpdated = true;
            }

            if (orgTdsc.ParentID != tdsc.ParentID)
            {
                orgTdsc.ParentID = tdsc.ParentID;
                DbData.parentId = tdsc.ParentID;
                isUpdated = true;
            }

            if (orgTdsc.Des != tdsc.Des)
            {
                orgTdsc.Des = tdsc.Des;
                DbData.objDesc = tdsc.Des;
                isUpdated = true;
            }

            if (orgTdsc.Type != tdsc.Type)
            {
                orgTdsc.Type = tdsc.Type;
                DbData.objTypeId = (int)tdsc.Type;
                isUpdated = true;
            }

            if (orgTdsc.AlarmEnable != tdsc.AlarmEnable)
            {
                orgTdsc.AlarmEnable = tdsc.AlarmEnable;
                DbData.alarmEnable = (int)tdsc.AlarmEnable;
                isUpdated = true;
            }

            if (orgTdsc.Saved != tdsc.Saved)
            {
                orgTdsc.Saved = tdsc.Saved;
                DbData.saved = (int)tdsc.Saved;
                isUpdated = true;
            }

            return isUpdated;
        }

        protected override void HandleTag(BStruBase data, string tagIsid)
        {
            if (!(data is TDSC tdsc)) return;

            TagRes tag = new TagRes()
            {
                tagIsid = tagIsid ?? Uuid.Create16Token(),
                tagName = tdsc.Name,
                tagTypeCode = "21", //B接口读写
                dataType = "5", //5-float
                ioType = "1", //'读写类型(0不可读写/1只读/2只写/3可读可写)'
                addition = 0,// bias
                multiplier = 1, // 倍数
                ruleIsid = cfg.AppSettings["ruleIsid"],  // saved
                tagAddr = "1",   // 1
            };

            DbData = new SqlTdsc()
            {
                alarmEnable = (int)tdsc.AlarmEnable,
                objDesc = tdsc.Des,
                objId = tdsc.ID,
                objName = tdsc.Name,
                objTypeId = (int)tdsc.Type,
                parentId = tdsc.ParentID,
                saved = (int)tdsc.Saved,
            };
        }

        protected override void HandleAlarms(BStruBase bData, SqlAlarmMap[] alarmMaps)
        {
            TDSC data = bData as TDSC;
            if (data == null && Tag == null) return;

            SqlAlarmRes valueAlarm = StruAlarm(data, alarmMaps.FirstOrDefault(a => a.alarmTypeCode == 0));
            valueAlarm.alarmType = 5;
            AlarmRess.Add(valueAlarm);
        }

        private SqlAlarmRes StruAlarm(TDSC data, SqlAlarmMap armMap)
        {
            SqlAlarmRes alarm = new SqlAlarmRes()
            {
                tagIsid = Tag.tagIsid,
                alarmEnable = data.AlarmEnable == EnumEnable.ENABLE ? 1 : 0,
                alarmIsid = armMap.alarmIsid ?? Uuid.Create16Token(),
                alarmType = 1,
            };
            alarm.alarmName = alarm.alarmIsid;
            return alarm;
        }
    }
}
