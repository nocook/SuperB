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
    class SuperB_Taoc : SuperBTagBase
    {
        public SqlTaoc DbData { get; set; }

        public SuperB_Taoc(TAOC data, string tagIsid, SqlAlarmMap[] alarmMaps)
            : base(data, tagIsid, alarmMaps)
        {
            TagType = EnumType.AO;
        }

        public override bool Update(BStruBase tagbase)
        {
            if (!(tagbase is TAOC data)) return false;
            if (!(OrgiData is TAOC orgData)) return false;

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
            if (orgData.ControlEnable != data.ControlEnable)
            {
                orgData.ControlEnable = data.ControlEnable;
                DbData.controlEnable = (int)data.ControlEnable;
                isUpdated = true;
            }
            if (orgData.Unit != data.Unit)
            {
                orgData.Unit = data.Unit;
                DbData.unit = data.Unit;
                isUpdated = true;
            }

            return isUpdated;
        }

        protected override void HandleTag(BStruBase data, string tagIsid)
        {
            if (!(data is TAOC taoc)) return;

            TagRes tag = new TagRes()
            {
                tagIsid = tagIsid ?? Uuid.Create16Token(),
                tagName = data.Name,
                tagTypeCode = "21", //B接口读写
                dataType = "5", //5-float
                ioType = "3", //'读写类型(0不可读写/1只读/2只写/3可读可写)'
                addition = 0,// bias
                multiplier = 1, // 倍数
                ruleIsid = cfg.AppSettings["ruleIsid"],  // saved
                tagAddr = "1",   // 1
            };

            DbData = new SqlTaoc()
            {
                objDesc = taoc.Des,
                objId = taoc.ID,
                objName = taoc.Name,
                objTypeId = (int)taoc.Type,
                parentId = taoc.ParentID,
                saved = (int)taoc.Saved,
                controlEnable = (int)taoc.ControlEnable,
                maxVal = taoc.MaxVal,
                minVal = taoc.MinVal,
                percision = taoc.Percision,
                stander = taoc.Stander,
                unit = taoc.Unit,
            };
        }

        protected override void HandleAlarms(BStruBase bData, SqlAlarmMap[] alarmMaps)
        {

        }
    }
}
