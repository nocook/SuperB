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
    class SuperB_Tdoc : SuperBTagBase
    {
        public SqlTdoc DbData { get; set; }

        public SuperB_Tdoc(TDOC data, string tagIsid, SqlAlarmMap[] alarmMaps)
            : base(data, tagIsid, alarmMaps)
        {
            TagType = EnumType.DO;
        }

        public override bool Update(BStruBase tagbase)
        {
            if (!(tagbase is TDOC data)) return false;
            if (!(OrgiData is TDOC orgData)) return false;

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
            if (orgData.ControlEnable != data.ControlEnable)
            {
                orgData.ControlEnable = data.ControlEnable;
                DbData.controlEnable = (int)data.ControlEnable;
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
            if (!(data is TDOC tdoc)) return;

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

            DbData = new SqlTdoc()
            {
                objDesc = tdoc.Des,
                objId = tdoc.ID,
                objName = tdoc.Name,
                objTypeId = (int)tdoc.Type,
                parentId = tdoc.ParentID,
                saved = (int)tdoc.Saved,
                controlEnable = (int)tdoc.ControlEnable,
                desc0 = tdoc.Desc0,
                desc1 = tdoc.Desc1,
            };
        }

        protected override void HandleAlarms(BStruBase bData, SqlAlarmMap[] alarmMaps)
        {

        }
    }
}
