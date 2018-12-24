using MqttCommon;
using MqttCommon.Setup;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr
{
    delegate Task<bool> SetupChangedHandle(SetupModel setup);
    class SetupMgr
    {
        public static SetupMgr Inst = new SetupMgr();

        private ConcurrentDictionary<string, TagRes> _tagsMap;

        public event SetupChangedHandle SetupChangedEvent;

        private SetupMgr()
        {
            _tagsMap = new ConcurrentDictionary<string, TagRes>();
        }

        public async Task Init()
        {
            await MqttMgr.Inst.SetSubscribe($"inIot/{cfg.AppSettings["ProjectName"]}" +
                $"/{cfg.AppSettings["MqttClientId"]}/setup", TagSetupReceived);
        }

        /// <summary>
        /// 收集所有的page之后，再response
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        private async void TagSetupReceived(string topic, byte[] payload)
        {
            string strPayload = Encoding.UTF8.GetString(payload);
            SetupModel setup = JsonSrialize.Desrialize<SetupModel>(strPayload);
            LogHelper.Debug("TagSetupReceived: " + strPayload);
            if (!(PageModelBase<GroupRes>.GatherAllPages(setup, 5000, null) is SetupModel pages)) return;

            var task = Setup2Proxys(pages);
            await task;

            if (task.Result != null)
            {
                LogHelper.Debug("task.Result.code = " + task.Result.code);
                await MqttMgr.Inst.PublishAsync(topic + "/response", JsonSrialize.Srialize(task.Result));
            }
        }

        private async Task<SetupResModel> Setup2Proxys(SetupModel setup)
        {
            SetupResModel res = new SetupResModel()
            {
                token = setup.token,
                code = 0,
                msg = "success",
            };

            if (setup.changeType == ChangeType.Delete)
            {
                res.code = 3;
                res.msg = "B接口没有删除接口";
                return res;
            }
            // transProtocolId为key对group分类
            // var proxyGroups = Util.RoutePageing(20, setup, g => g.transProtocolId);
            List<string> failedGroups = new List<string>();
            var msgs = string.Empty;

            if (setup.objects.Length == 0) return res;

            bool isSuccess = await SetupChangedEvent(setup);

            if (!isSuccess)
            {
                res.code = 1;
                res.msg += $"\r\n{cfg.AppSettings["MqttClientId"]}:failed," + msgs;
            }

            return res;
        }

        private void Compare2Repo(TagRes tag)
        {
            var tagMap = SuperBRepertory.Inst.tagmap.Find(m => m.tagIsid == tag.tagIsid);
            if (tagMap == null) return;

           // SuperBRepertory.Inst.taic.Find()
        }

    }
}
