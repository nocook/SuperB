using Microsoft.EntityFrameworkCore;
using MqttCommon.Setup;
using SuperBDevAccess;
using SuperBMgr.models;
using SuperBMgr.transfer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static SuperBDevAccess.WxBSdk;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr
{
    class SdkMgr
    {
        public static SdkMgr Inst = new SdkMgr();

        ConcurrentDictionary<int, SqlSubPlateformRes> _subPlates = new ConcurrentDictionary<int, SqlSubPlateformRes>();

        public async Task Init()
        {
            // 订阅各种事件
            WxBSdk.DataCallback += WxBSdk_DataCallback;
            WxBSdk.Disconnected += WxBSdk_Disconnected;
            WxBSdk.Connected += WxBSdk_Connected;

            // 从数据库获取当前所有下级平台
            using (var db = new SuperBContext())
            {
                var query = db.subplateformres.ToListAsync();
                await query;

                if (query.Result != null)
                {
                    foreach (var plate in query.Result)
                    {
                        var tmp = new SuperB_PlateInfo();
                        // 初始化sdk
                        var userId = SuperB_Login(plate.ip, plate.port, plate.userName, plate.password, ref tmp);
                        if (userId < 0 || !_subPlates.TryAdd(userId, plate))
                        {
                            LogHelper.Error($"login sdk error {plate.ip} !");
                            continue;
                        }
                    }
                }
            }
        }

        private void WxBSdk_Connected(int lUser)
        {
            throw new NotImplementedException();
        }

        private void WxBSdk_Disconnected(int lUser)
        {
            throw new NotImplementedException();
        }

        private async Task WxBSdk_DataCallback(int lUser, Sdk_Event cmd, params object[] objs)
        {
            if (!_subPlates.TryGetValue(lUser, out SqlSubPlateformRes plate))
            {
                LogHelper.Error($"WxBSdk_DataCallback cannot find userid = {lUser}");
                return;
            }

            switch (cmd)
            {
                case Sdk_Event.Attribute:
                    {
                        if (!(objs[0] is List<BStruBase> atts)) return;
                        await HandleAttribute(atts);
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task HandleAttribute(List<BStruBase> atts)
        {
            PlateformDevs pl = new PlateformDevs();
            ConcurrentDictionary<string, List<TagRes>> groupBuffer = new ConcurrentDictionary<string, List<TagRes>>();

            foreach (var att in atts)
            {
                switch (att.Type)
                {
                    case EnumType.STATION:
                        {
                            if (!(att is TStation st)) continue;
                            await SuperBRepertory.Inst.UpdateOrAddStation(st);
                        }
                        break;
                    case EnumType.DEVICE:
                        {
                            if (!(att is TDevice dev)) continue;
                            await SuperBRepertory.Inst.UpdateOrAddDevice(dev);
                        }
                        break;
                    case EnumType.AI:
                    case EnumType.AO:
                    case EnumType.DI:
                    case EnumType.DO:
                    case EnumType.STRIN:
                        {
                            await SuperBRepertory.Inst.BeginUpdateOrAddTag(att,  async (tag, groupIsid, change) =>
                            {
                                if (change == MqttCommon.ChangeType.Add)
                                {
                                    groupBuffer.AddOrUpdate(groupIsid, new List<TagRes>(),
                                        (oKey, oVal) => { oVal.Add(tag.Tag); return oVal; });
                                }
                                else if (change == MqttCommon.ChangeType.Update)
                                {
                                    return true;
                                }

                                return false;
                            });
                        }
                        break;
                    default:
                        break;
                }

                foreach (var pair in groupBuffer)
                {
                    var req = await HttpUtil.GetAsync<GroupResResponse>(
                        $"http://{cfg.AppSettings["serverUrl"]}/inIoT/devServer/tagGroup/{pair.Key}",
                       SuperBCenter.ServerToken);
                    if (req == null) continue;
                    if (req.codeid != 0)
                    {
                        var devMap = SuperBRepertory.Inst.devicemap.Find(d => d.groupIsid == pair.Key);
                        if (devMap == null)
                        {
                            LogHelper.Error($"GroupRes {pair.Key} cannot find map to device");
                            continue;
                        }
                       var device = SuperBRepertory.Inst.DevList.Find(d => d.Device.dvIsid == devMap.devIsid);
                        if (device == null)
                        {
                            LogHelper.Error($"GroupRes {pair.Key} cannot find device");
                            continue;
                        }
                        var res = await HttpUtil.PostAsync($"http://{cfg.AppSettings["serverUrl"]}/inIoT/devServer/tagGroup",
                            JsonSrialize.Srialize(new GroupRes()
                            {
                                groupIsid = pair.Key,
                                collectPeriod = 2,
                                dvIsid = device.Device.dvIsid,
                                nodeIsid = device.NodeIsid,
                                overTime = 2,
                                relinkCount = 2,
                                relinkPeriod = 2,
                                transProtocolId = "1",
                            }),
                            SuperBCenter.ServerToken);

                        if (res == null || res.codeid != 0)
                        {
                            LogHelper.Error($"GroupRes {pair.Key} cannot add");
                            continue;
                        }
                    }

                    
                }
            }
        }
        
        private void ClearSdks()
        {
           /* foreach (var sdk in _sdkList)
            {
               // sdk.StopWork();
            }*/

        }


        private void Disconnected()
        {

        }

        private void Connected()
        {

        }


        private void DeviceRessUpdated()
        {

        }

        private void TagRessUpdated()
        {

        }

        private void AlarmRessUpdated()
        {

        }

    }
}
 