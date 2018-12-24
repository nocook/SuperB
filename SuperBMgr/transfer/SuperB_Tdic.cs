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
    class SuperB_Tdic : SuperBTagBase
    {
        public SqlTdic DbData { get; set; }

        public SuperB_Tdic(TDIC data, string tagIsid, SqlAlarmMap[] alarmMaps)
            : base(data, tagIsid, alarmMaps)
        {
            TagType = EnumType.DI;
        }

        public override bool Update(BStruBase tagbase)
        {
            if (!(tagbase is TDIC data)) return false;
            if (!(OrgiData is TDIC orgData)) return false;

            if (orgData == null)
            {
                OrgiData = data;
                HandleTag(data, null);
                return true;
            }

            if (data.ID != orgData.ID) return false;

            bool isUpdated = false;
            if (orgData.Name != data.Name)
            {
                orgData.Name = data.Name;
                DbData.objName = data.Name;
                isUpdated = true;
            }
            if (orgData.ParentID != data.ParentID)
            {
                orgData.ParentID = data.ParentID;
                DbData.parentId = data.ParentID;
                isUpdated = true;
            }
            if (orgData.Des != data.Des)
            {
                orgData.Des = data.Des;
                DbData.objDesc = data.Des;
                isUpdated = true;
            }
            if (orgData.Type != data.Type)
            {
                orgData.Type = data.Type;
                DbData.objTypeId = (int)data.Type;
                isUpdated = true;
            }
            if (orgData.Saved != data.Saved)
            {
                orgData.Saved = data.Saved;
                DbData.saved = (int)data.Saved;
                isUpdated = true;
            }
            if (orgData.AlarmEnable != data.AlarmEnable)
            {
                orgData.AlarmEnable = data.AlarmEnable;
                DbData.alarmEnable = (int)data.AlarmEnable;
                isUpdated = true;
            }
            if (orgData.AlarmLevel != data.AlarmLevel)
            {
                orgData.AlarmLevel = data.AlarmLevel;
                DbData.alarmLevel = (int)data.AlarmLevel;
                isUpdated = true;
            }
            if (orgData.AlarmThresbhold != data.AlarmThresbhold)
            {
                orgData.AlarmThresbhold = data.AlarmThresbhold;
                DbData.alarmThresbhold = (int)data.AlarmThresbhold;
                isUpdated = true;
            }
            if (orgData.Desc0 != data.Desc0)
            {
                orgData.Desc0 = data.Desc0;
                DbData.desc0 = data.Desc0;
                isUpdated = true;
            }
            if (orgData.Desc1 != data.Desc1)
            {
                orgData.Desc1 = data.Desc1;
                DbData.desc1 = data.Desc1;
                isUpdated = true;
            }

            return isUpdated;
        }

        protected override void HandleTag(BStruBase data, string tagIsid)
        {
            if (!(data is TDIC tdic)) return;

            TagRes tag = new TagRes()
            {
                tagIsid = tagIsid ?? Uuid.Create16Token(),
                tagName = data.Name,
                tagTypeCode = "21", //B接口读写
                dataType = "5", //5-float
                ioType = "1", //'读写类型(0不可读写/1只读/2只写/3可读可写)'
                addition = 0,// bias
                multiplier = 1, // 倍数
                ruleIsid = cfg.AppSettings["ruleIsid"],  // saved
                tagAddr = "1",   // 1
            };

            DbData = new SqlTdic()
            {
                objDesc = tdic.Des,
                objId = tdic.ID,
                objName = tdic.Name,
                objTypeId = (int)tdic.Type,
                parentId = tdic.ParentID,
                saved = (int)tdic.Saved,
                desc0 = tdic.Desc0,
                desc1 = tdic.Desc1,
                alarmEnable = (int)tdic.AlarmEnable,
                alarmLevel = (int)tdic.AlarmLevel,
                alarmThresbhold = (int)tdic.AlarmThresbhold,
            };
        }

        protected override void HandleAlarms(BStruBase bData, SqlAlarmMap[] alarmMaps)
        {
            TDIC data = bData as TDIC;
            if (data == null && Tag == null || data.AlarmLevel == EnumAlarmLevel.NOALARM) return;

            if (data.AlarmThresbhold ==  EnumEnable.ENABLE)
            {
                SqlAlarmRes valueAlarm = StruAlarm(data, alarmMaps.FirstOrDefault(a => a.alarmTypeCode == 0));
                valueAlarm.alarmType = 5;
                AlarmRess.Add(valueAlarm);
            }

        }

        private SqlAlarmRes StruAlarm(TDIC data, SqlAlarmMap armMap)
        {
            SqlAlarmRes alarm = new SqlAlarmRes()
            {
                tagIsid = Tag.tagIsid,
                alarmEnable = data.AlarmEnable == EnumEnable.ENABLE ? 1 : 0,
                alarmIsid = armMap.alarmIsid ?? Uuid.Create16Token(),
                alarmLevel = (int)data.AlarmLevel,
                alarmType = 1,
            };
            alarm.alarmName = alarm.alarmIsid;
            return alarm;
        }
    }
}
