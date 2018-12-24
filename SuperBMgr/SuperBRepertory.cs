using Microsoft.EntityFrameworkCore;
using MqttCommon;
using SuperBMgr.models;
using SuperBMgr.transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SuperBDevAccess.WxBSdk;
using cfg = System.Configuration.ConfigurationManager;


namespace SuperBMgr
{
    delegate Task<bool> TagReqFunc(SuperBTagBase tag, string groupIsid, ChangeType change);
    class SuperBRepertory
    {
        public static SuperBRepertory Inst = new SuperBRepertory();

        public List<SqlSubPlateformRes> subplateformres { get; private set; }
        public List<SqlTagMap> tagmap { get; private set; }
        public List<SqlDeviceMap> devicemap { get; private set; }
        public List<SqlAlarmMap> alarmmap { get; private set; }
        public List<SqlNodeMap> nodemap { get; private set; }

        public List<SuperBStation> NdList { get; private set; }
        public List<SuperBDevice> DevList { get; private set; }
        public List<SuperBTagBase> TagList { get; private set; }

        SuperBContext _db = new SuperBContext();
        private SuperBRepertory()
        {
            NdList = new List<SuperBStation>();
            DevList = new List<SuperBDevice>();
            TagList = new List<SuperBTagBase>();
        }

        /// <summary>
        /// add:增加nodemap和station实体
        /// update:只替换实体
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public async Task<bool> UpdateOrAddStation(TStation station)
        {
            var nd = NdList.Find(n => n.OrgiData.ID == station.ID);
            if (nd == null)
            {
                var ndMap = nodemap.Find(n => n.objId == nd.OrgiData.ID);
                if (ndMap == null) return false;
                nd = new SuperBStation(station, ndMap.nodeIsid);

                try
                {
                    await _db.tStation.AddAsync(nd.DbData);
                    await _db.SaveChangesAsync();
                    NdList.Add(nd);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                    return false;
                }
            }
            else
            {
                if (nd.Update(station))
                {
                    try
                    {
                        _db.tStation.Update(nd.DbData);
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex);
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<bool> UpdateNodeMap(SuperBStation st)
        {
            if (!nodemap.Exists(n => n.nodeIsid == st.NodeIsid && n.objId == st.OrgiData.ID))
            {
                var map = new SqlNodeMap() { nodeIsid = st.NodeIsid, objId = st.OrgiData.ID };
                try
                {
                    await _db.nodemap.AddAsync(map);
                    await _db.SaveChangesAsync();
                    nodemap.Add(map);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> UpdateOrAddDevice(TDevice data)
        {
            var node = nodemap.Find(m => m.objId == data.ParentID);
            if (node == null) return false; //没有父节点，返回

            var dev = DevList.Find(n => n.OrgiData.ID == data.ID);
            if (dev == null)    //新增设备，返回新增的设备，等web新增成功，插入数据库
            {
                dev = new SuperBDevice(data, node.nodeIsid);
                var map = new SqlDeviceMap() { devIsid = dev.Device.dvIsid, objId = data.ID, groupIsid = Uuid.Create16Token() };
                var req = await HttpUtil.PostAsync($"http://{cfg.AppSettings["serverUrl"]}/inIoT/devServer/deviceRes",
                    JsonSrialize.Srialize(new DevModel()
                    {
                        dvIsid = dev.Device.dvIsid,
                        dvName = dev.Device.dvName,
                        dvTypeCode = dev.Device.dvTypeCode,
                        managerIsid = cfg.AppSettings["managerIsid"],
                        nodeIsid = dev.NodeIsid,
                        addrRes = new MqttCommon.Setup.AddResource()
                        {
                            communicationType = 1,
                            dvAddr1 = "1.1.1.1",
                            dvAddr2 = "2222"
                        }
                    }), SuperBCenter.ServerToken);

                if (req == null || req.codeid != 0) return false;

                try
                {
                    await _db.tDevice.AddAsync(dev.DbData);
                    if (!devicemap.Exists(d => d.objId == dev.OrgiData.ID && d.devIsid == dev.NodeIsid))
                    {
                        await _db.devicemap.AddAsync(map);
                        devicemap.Add(map);
                    }
                    await _db.SaveChangesAsync();
                    DevList.Add(dev);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                    return false;
                }
            }
            else
            {
                if (!devicemap.Exists(d => d.objId == dev.OrgiData.ID && d.devIsid == dev.Device.dvIsid)) return false;
                if (dev.Update(data))
                {
                    try
                    {
                        _db.tDevice.Update(dev.DbData);
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex);
                        return false;
                    }
                }

            }

            return true;
        }

        public async Task<bool> BeginUpdateOrAddTag(BStruBase data, TagReqFunc reqFunc)
        {
            var dev = devicemap.Find(m => m.objId == data.ParentID);
            if (dev == null) return false; //没有父节点，返回

            var tag = TagList.Find(n => n.OrgiData.ID == data.ID);
            if (tag == null)    //新增设备，返回新增的设备，等web新增成功，插入数据库
            {
                if (data.Type == EnumType.AI)
                    tag = new SuperB_Taic(data as TAIC, dev.devIsid, null);
                else if (data.Type == EnumType.AO)
                    tag = new SuperB_Taoc(data as TAOC, dev.devIsid, null);
                else if (data.Type == EnumType.DI)
                    tag = new SuperB_Tdic(data as TDIC, dev.devIsid, null);
                else if (data.Type == EnumType.DO)
                    tag = new SuperB_Tdoc(data as TDOC, dev.devIsid, null);
                else if (data.Type == EnumType.STRIN)
                    tag = new SuperB_Tdsc(data as TDSC, dev.devIsid, null);

                var map = new SqlTagMap() { tagIsid = tag.Tag.tagIsid, objId = data.ID };

                TagList.Add(tag);
                var req = await reqFunc(tag, dev.groupIsid, ChangeType.Add);
                if (!req) return true;

                try
                {
                    if (data.Type == EnumType.AI)
                    {
                        await _db.taic.AddAsync((tag as SuperB_Taic).DbData);
                    }
                    else if (data.Type == EnumType.AO)
                    {
                        await _db.taoc.AddAsync((tag as SuperB_Taoc).DbData);
                    }
                    else if (data.Type == EnumType.DI)
                    {
                        await _db.tdic.AddAsync((tag as SuperB_Tdic).DbData);
                    }
                    else if (data.Type == EnumType.DO)
                    {
                        await _db.tdoc.AddAsync((tag as SuperB_Tdoc).DbData);
                    }
                    else if (data.Type == EnumType.STRIN)
                    {
                        await _db.tdsc.AddAsync((tag as SuperB_Tdsc).DbData);
                    }

                    if (!tagmap.Exists(d => d.objId == tag.OrgiData.ID && d.tagIsid == tag.Tag.tagIsid))
                    {
                        await _db.tagmap.AddAsync(map);
                        tagmap.Add(map);
                    }
                    await _db.SaveChangesAsync();
                    TagList.Add(tag);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                    return false;
                }
            }
            else
            {
                if (!tagmap.Exists(d => d.objId == tag.OrgiData.ID && d.tagIsid == tag.Tag.tagIsid)) return false;
                var tmp = tag.OrgiData;
                if (tag.Update(data))
                {
                    var req = await reqFunc(tag, dev.groupIsid, ChangeType.Update);
                    if (!req)
                    {
                        tag.Update(tmp);
                        return false;
                    }

                    try
                    {
                        if (data.Type == EnumType.AI)
                        {
                            _db.taic.Update((tag as SuperB_Taic).DbData);
                        }
                        else if (data.Type == EnumType.AO)
                        {
                            _db.taoc.Update((tag as SuperB_Taoc).DbData);
                        }
                        else if (data.Type == EnumType.DI)
                        {
                             _db.tdic.Update((tag as SuperB_Tdic).DbData);
                        }
                        else if (data.Type == EnumType.DO)
                        {
                            _db.tdoc.Update((tag as SuperB_Tdoc).DbData);
                        }
                        else if (data.Type == EnumType.STRIN)
                        {
                            _db.tdsc.Update((tag as SuperB_Tdsc).DbData);
                        }
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex);
                        return false;
                    }
                }

            }

            return true;
        }

        public async Task<bool> EndUpdateOrAddTag(List<SuperBTagBase> sussTags, List<SuperBTagBase> failedTags)
        {
            foreach (var tag in failedTags)
                TagList.Remove(tag);
            foreach (var tag in sussTags)
            {
                var map = new SqlTagMap() { tagIsid = tag.Tag.tagIsid, objId = tag.OrgiData.ID };

                try
                {
                    if (tag.TagType == EnumType.AI)
                    {
                        await _db.taic.AddAsync((tag as SuperB_Taic).DbData);
                    }
                    else if (tag.TagType == EnumType.AO)
                    {
                        await _db.taoc.AddAsync((tag as SuperB_Taoc).DbData);
                    }
                    else if (tag.TagType == EnumType.DI)
                    {
                        await _db.tdic.AddAsync((tag as SuperB_Tdic).DbData);
                    }
                    else if (tag.TagType == EnumType.DO)
                    {
                        await _db.tdoc.AddAsync((tag as SuperB_Tdoc).DbData);
                    }
                    else if (tag.TagType == EnumType.STRIN)
                    {
                        await _db.tdsc.AddAsync((tag as SuperB_Tdsc).DbData);
                    }

                    if (!tagmap.Exists(d => d.objId == tag.OrgiData.ID && d.tagIsid == tag.Tag.tagIsid))
                    {
                        await _db.tagmap.AddAsync(map);
                        tagmap.Add(map);
                    }
                    await _db.SaveChangesAsync();
                    TagList.Add(tag);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                    return false;
                }
            }

            return true;
        }

        public async Task SynchronizeDb()
        {
            var db = _db;
            subplateformres = await db.subplateformres.ToListAsync();
            var tStation = await db.tStation.ToListAsync();
            nodemap = await db.nodemap.ToListAsync();
            foreach (var st in tStation)
            {
                var ndMap = nodemap.Find(n => n.objId == st.objId);
                if (ndMap == null) continue;
                NdList.Add(new SuperBStation(new TStation()
                {
                    ID = st.objId,
                    Name = st.objName,
                    Des = st.objDesc,
                    Type = (EnumType)st.objTypeId,
                    ParentID = st.parentId ?? 0,
                    Latitude = st.latitude,
                    Longitude = st.longitude
                },
                ndMap.nodeIsid));
            }

            var tDevice = await db.tDevice.ToListAsync();
            foreach (var dev in tDevice)
            {
                var ndMap = NdList.Find(n => n.OrgiData.ID == dev.parentId);
                if (ndMap == null) continue;
                DevList.Add(new SuperBDevice(new TDevice()
                {
                    ID = dev.objId,
                    Name = dev.objName,
                    Des = dev.objDesc,
                    Type = (EnumType)dev.objTypeId,
                    ParentID = dev.parentId ?? 0,
                    BeginRunTime = dev.beginRunTime.HasValue ?
                     SuperBTagBase.Transfer(dev.beginRunTime.Value) :
                     SuperBTagBase.Transfer(DateTime.Now),
                    DeviceType = (EnumDeviceType)dev.devTypeId,
                    Productor = dev.productor,
                    Version = dev.version,

                },
                ndMap.NodeIsid));
            }

            tagmap = await db.tagmap.ToListAsync();
            devicemap = await db.devicemap.ToListAsync();
            alarmmap = await db.alarmmap.ToListAsync();

            var taic = await db.taic.ToListAsync();
            foreach (var aic in taic)
            {
                var devMap = DevList.Find(n => n.OrgiData.ID == aic.parentId);
                var tagMap = tagmap.Find(t => t.objId == aic.objId);
                if (devMap == null || tagMap == null) continue;
                TagList.Add(new SuperB_Taic(new TAIC()
                {
                    ID = aic.objId,
                    Name = aic.objName,
                    Des = aic.objDesc,
                    Type = (EnumType)aic.objTypeId,
                    ParentID = aic.parentId ?? 0,
                    AlarmEnable = (EnumEnable)aic.alarmEnable,
                    AlarmLevel = (EnumAlarmLevel)aic.alarmLevel,
                    HiLimit1 = aic.hiLimit1,
                    HiLimit2 = aic.hiLimit2,
                    HiLimit3 = aic.hiLimit3,
                    LoLimit1 = aic.loLimit1,
                    LoLimit2 = aic.loLimit2,
                    LoLimit3 = aic.loLimit3,
                },
                tagMap.tagIsid, alarmmap.ToArray()));
            }
            var taoc = await db.taoc.ToListAsync();
            foreach (var aic in taoc)
            {
                var devMap = DevList.Find(n => n.OrgiData.ID == aic.parentId);
                var tagMap = tagmap.Find(t => t.objId == aic.objId);
                if (devMap == null || tagMap == null) continue;
                TagList.Add(new SuperB_Taoc(new TAOC()
                {
                    ID = aic.objId,
                    Name = aic.objName,
                    Des = aic.objDesc,
                    Type = (EnumType)aic.objTypeId,
                    ParentID = aic.parentId ?? 0,
                    ControlEnable = (EnumEnable)aic.controlEnable,
                    MaxVal = aic.maxVal,
                    MinVal = aic.minVal,
                    Percision = aic.percision,
                    Saved = (EnumEnable)aic.saved,
                    Stander = aic.stander,
                    Unit = aic.unit,
                },
                tagMap.tagIsid, alarmmap.ToArray()));
            }
            var tdic = await db.tdic.ToListAsync();
            foreach (var aic in tdic)
            {
                var devMap = DevList.Find(n => n.OrgiData.ID == aic.parentId);
                var tagMap = tagmap.Find(t => t.objId == aic.objId);
                if (devMap == null || tagMap == null) continue;
                TagList.Add(new SuperB_Tdic(new TDIC()
                {
                    ID = aic.objId,
                    Name = aic.objName,
                    Des = aic.objDesc,
                    Type = (EnumType)aic.objTypeId,
                    ParentID = aic.parentId ?? 0,
                    Saved = (EnumEnable)aic.saved,
                    AlarmEnable = (EnumEnable)aic.alarmEnable,
                    AlarmLevel = (EnumAlarmLevel)aic.alarmLevel,
                    AlarmThresbhold = (EnumEnable)aic.alarmThresbhold,
                    Desc0 = aic.desc0,
                    Desc1 = aic.desc1,
                },
                tagMap.tagIsid, alarmmap.ToArray()));
            }
            var tdoc = await db.tdoc.ToListAsync();
            foreach (var aic in tdoc)
            {
                var devMap = DevList.Find(n => n.OrgiData.ID == aic.parentId);
                var tagMap = tagmap.Find(t => t.objId == aic.objId);
                if (devMap == null || tagMap == null) continue;
                TagList.Add(new SuperB_Tdoc(new TDOC()
                {
                    ID = aic.objId,
                    Name = aic.objName,
                    Des = aic.objDesc,
                    Type = (EnumType)aic.objTypeId,
                    ParentID = aic.parentId ?? 0,
                    ControlEnable = (EnumEnable)aic.controlEnable,
                    Saved = (EnumEnable)aic.saved,
                    Desc0 = aic.desc0,
                    Desc1 = aic.desc1,
                },
                tagMap.tagIsid, alarmmap.ToArray()));
            }
            var tdsc = await db.tdsc.ToListAsync();
            foreach (var aic in tdsc)
            {
                var devMap = DevList.Find(n => n.OrgiData.ID == aic.parentId);
                var tagMap = tagmap.Find(t => t.objId == aic.objId);
                if (devMap == null || tagMap == null) continue;
                TagList.Add(new SuperB_Tdsc(new TDSC()
                {
                    ID = aic.objId,
                    Name = aic.objName,
                    Des = aic.objDesc,
                    Type = (EnumType)aic.objTypeId,
                    ParentID = aic.parentId ?? 0,
                    Saved = (EnumEnable)aic.saved,
                    AlarmEnable = (EnumEnable)aic.alarmEnable,
                },
                tagMap.tagIsid, alarmmap.ToArray()));
            }
        }

    }
}
