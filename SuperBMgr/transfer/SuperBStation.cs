using System;
using System.Collections.Generic;
using System.Text;
using SuperBMgr.models;
using static SuperBDevAccess.WxBSdk;
using cfg = System.Configuration.ConfigurationManager;
using MqttCommon.Setup;

namespace SuperBMgr.transfer
{
    class SuperBStation
    {
        public string NodeIsid { get; private set; }
        public TStation OrgiData { get; private set; }
        public SqlTStation DbData { get; private set; }

        public SuperBStation(TStation data, string nodeIsid)
        {
            OrgiData = data;
            NodeIsid = nodeIsid != null? nodeIsid:Uuid.Create16Token();
            HandleStation(data);
        }

        private void HandleStation(TStation data)
        {
            DbData = new SqlTStation()
            {
                latitude = data.Latitude,
                longitude = data.Longitude,
                objDesc = data.Des,
                objId = data.ID,
                objName = data.Name,
                objTypeId = (int)data.Type,
                parentId = data.ParentID,
            };
        }

        public bool Update(TStation data)
        {
            if (data == null) return false;

            if (OrgiData == null)
            {
                OrgiData = data;
                HandleStation(data);
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

            if (OrgiData.Latitude != data.Latitude)
            {
                OrgiData.Latitude = data.Latitude;
                DbData.latitude = data.Latitude;
                isUpdated = true;
            }

            if (OrgiData.Longitude != data.Longitude)
            {
                OrgiData.Longitude = data.Longitude;
                DbData.longitude = data.Longitude;
                isUpdated = true;
            }

            return isUpdated;
        }
    }
}
