using System;
using System.Collections.Generic;
using System.Text;
using SuperBMgr.models;
using static SuperBDevAccess.WxBSdk;
using cfg = System.Configuration.ConfigurationManager;
using MqttCommon.Setup;

namespace SuperBMgr.transfer
{
    class SuperBDevice
    {
        public string NodeIsid { get; private set; }
        public DevModel Device { get; private set; }
        public TDevice OrgiData { get; private set; }
        public SqlTDevice DbData { get; private set; }

        public SuperBDevice(TDevice data, string nodeIsid)
        {
            NodeIsid = nodeIsid;
            OrgiData = data;
            HandleDevice(data);
        }

        private void HandleDevice(TDevice data)
        {
            Device = new DevModel()
            {
                addrRes = new AddResource(),
                dvBrandid = "",
                dvIsid = Uuid.Create16Token(),
                managerIsid = cfg.AppSettings["managerIsid"],
                nodeIsid = NodeIsid,
                dvTypeCode = "37",
                dvName = data.Name,
            };

            DbData = new SqlTDevice()
            {
                beginRunTime = SuperBTagBase.Transfer(data.BeginRunTime),
                devTypeId = (int)data.DeviceType,
                objDesc = data.Des,
                objId = data.ID,
                parentId = data.ParentID,
                objName = data.Name,
                objTypeId = (int)data.Type,
                productor = data.Productor,
                version = data.Version,
            };
        }

        public bool Update(TDevice data)
        {
            if (data == null) return false;

            if (OrgiData == null)
            {
                OrgiData = data;
                HandleDevice(data);
                return true;
            }

            if (data.ID != OrgiData.ID) return false;

            bool isUpdated = false;
            if (OrgiData.Name != data.Name)
            {
                OrgiData.Name = data.Name;
                DbData.objName = data.Name;
                isUpdated = true;
            }

            if (OrgiData.ParentID != data.ParentID)
            {
                OrgiData.ParentID = data.ParentID;
                DbData.parentId = data.ParentID;
                isUpdated = true;
            }

            if (OrgiData.Des != data.Des)
            {
                OrgiData.Des = data.Des;
                DbData.objDesc = data.Des;
                isUpdated = true;
            }

            if (OrgiData.Type != data.Type)
            {
                OrgiData.Type = data.Type;
                DbData.objTypeId = (int)data.Type;
                isUpdated = true;
            }

            if (OrgiData.Productor != data.Productor)
            {
                OrgiData.Productor = data.Productor;
                DbData.productor = data.Productor;
                isUpdated = true;
            }

            if (OrgiData.DeviceType != data.DeviceType)
            {
                OrgiData.DeviceType = data.DeviceType;
                DbData.devTypeId = (int)data.DeviceType;
                isUpdated = true;
            }
            
            if (OrgiData.Version != data.Version)
            {
                OrgiData.Version = data.Version;
                DbData.version = data.Version;
                isUpdated = true;
            }

            if (OrgiData.BeginRunTime != data.BeginRunTime)
            {
                OrgiData.BeginRunTime = data.BeginRunTime;
                DbData.beginRunTime = SuperBTagBase.Transfer(data.BeginRunTime);
                isUpdated = true;
            }

            return isUpdated;

        }

    }
}
