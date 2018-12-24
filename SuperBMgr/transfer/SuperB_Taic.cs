using MqttCommon.Setup;
using SuperBDevAccess;
using SuperBMgr.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SuperBDevAccess.WxBSdk;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr.transfer
{

    class SuperB_Taic : SuperBTagBase
    {
        public SqlTaic DbData { get; set; }

        public SuperB_Taic(TAIC data, string tagIsid, SqlAlarmMap[] alarmMaps)
            : base(data, tagIsid, alarmMaps)
        {
            TagType = EnumType.AI;
        }

        protected override void HandleTag(BStruBase data, string tagIsid)
        {
            if (!(data is TAIC taic)) return;

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

            DbData = new SqlTaic()
            {
                alarmEnable = (int)taic.AlarmEnable,
                alarmLevel = (int)taic.AlarmLevel,
                hiLimit1 = taic.HiLimit1,
                hiLimit2 = taic.HiLimit2,
                hiLimit3 = taic.HiLimit3,
                loLimit1 = taic.LoLimit1,
                loLimit2 = taic.LoLimit2,
                loLimit3 = taic.LoLimit3,
                maxVal = taic.MaxVal,
                minVal = taic.MinVal,
                objDesc = taic.Des,
                objId = taic.ID,
                objName = taic.Name,
                objTypeId = (int)taic.Type,
                parentId = taic.ParentID,
                percision = taic.Percision,
                saved = (int)taic.Saved,
                stander = taic.Stander,
                unit = taic.Unit,
            };
        }

        public override bool Update(BStruBase tagbase)
        {
            if (!(tagbase is TAIC data)) return false;
            if (!(OrgiData is TAIC orgData)) return false;

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
            if (orgData.AlarmEnable != data.AlarmEnable)
            {
                orgData.AlarmEnable = data.AlarmEnable;
                DbData.alarmEnable = (int)data.AlarmEnable;
                isUpdated = true;
            }
            if (orgData.Saved != data.Saved)
            {
                orgData.Saved = data.Saved;
                DbData.saved = (int)data.Saved;
                isUpdated = true;
            }
            if (orgData.AlarmLevel != data.AlarmLevel)
            {
                orgData.AlarmLevel = data.AlarmLevel;
                DbData.alarmLevel = (int)data.AlarmLevel;
                isUpdated = true;
            }
            if (orgData.HiLimit1 != data.HiLimit1)
            {
                orgData.HiLimit1 = data.HiLimit1;
                DbData.hiLimit1 = data.HiLimit1;
                isUpdated = true;
            }
            if (orgData.HiLimit2 != data.HiLimit2)
            {
                orgData.HiLimit2 = data.HiLimit2;
                DbData.hiLimit2 = data.HiLimit2;
                isUpdated = true;
            }
            if (orgData.HiLimit3 != data.HiLimit3)
            {
                orgData.HiLimit3 = data.HiLimit3;
                DbData.hiLimit3 = data.HiLimit3;
                isUpdated = true;
            }
            if (orgData.LoLimit1 != data.LoLimit1)
            {
                orgData.LoLimit1 = data.LoLimit1;
                DbData.loLimit1 = data.LoLimit1;
                isUpdated = true;
            }
            if (orgData.LoLimit2 != data.LoLimit2)
            {
                orgData.LoLimit2 = data.LoLimit2;
                DbData.loLimit2 = data.LoLimit2;
                isUpdated = true;
            }
            if (orgData.LoLimit3 != data.LoLimit3)
            {
                orgData.LoLimit3 = data.LoLimit3;
                DbData.loLimit3 = data.LoLimit3;
                isUpdated = true;
            }
            if (orgData.MaxVal != data.MaxVal)
            {
                orgData.MaxVal = data.MaxVal;
                DbData.maxVal = data.MaxVal;
                isUpdated = true;
            }
            if (orgData.MinVal != data.MinVal)
            {
                orgData.MinVal = data.MinVal;
                DbData.minVal = data.MinVal;
                isUpdated = true;
            }
            if (orgData.Percision != data.Percision)
            {
                orgData.Percision = data.Percision;
                DbData.percision = data.Percision;
                isUpdated = true;
            }
            if (orgData.Stander != data.Stander)
            {
                orgData.Stander = data.Stander;
                DbData.stander = data.Stander;
                isUpdated = true;
            }
            
            return isUpdated;
        }

        protected override void HandleAlarms(BStruBase bData, SqlAlarmMap[] alarmMaps)
        {
            TAIC data = bData as TAIC;
            if (data == null && Tag == null || data.AlarmLevel == EnumAlarmLevel.NOALARM) return;

            if (!string.IsNullOrEmpty(data.LoLimit1) || !string.IsNullOrEmpty(data.HiLimit1))
            {
                SqlAlarmRes alarm = StruAlarm(data, alarmMaps.FirstOrDefault(a => a.alarmTypeCode == 1));
                alarm.lowLimit = data.LoLimit1;
                alarm.upperLimit = data.HiLimit1;
                AlarmRess.Add(alarm);
            }
            if (!string.IsNullOrEmpty(data.LoLimit2) || !string.IsNullOrEmpty(data.HiLimit2))
            {
                SqlAlarmRes alarm = StruAlarm(data, alarmMaps.FirstOrDefault(a => a.alarmTypeCode == 2));
                alarm.lowLimit = data.LoLimit2;
                alarm.upperLimit = data.HiLimit2;
                AlarmRess.Add(alarm);
            }
            if (!string.IsNullOrEmpty(data.LoLimit3) || !string.IsNullOrEmpty(data.HiLimit3))
            {
                SqlAlarmRes alarm = StruAlarm(data, alarmMaps.FirstOrDefault(a => a.alarmTypeCode == 3));
                alarm.lowLimit = data.LoLimit3;
                alarm.upperLimit = data.HiLimit3;
                AlarmRess.Add(alarm);
            }

            SqlAlarmRes valueAlarm = StruAlarm(data, alarmMaps.FirstOrDefault(a => a.alarmTypeCode == 0));
            valueAlarm.alarmType = 5;
            AlarmRess.Add(valueAlarm);
        }

        private SqlAlarmRes StruAlarm(TAIC data, SqlAlarmMap armMap)
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
