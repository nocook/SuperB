using MqttCommon.Setup;
using SuperBMgr.models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr
{
    public class SuperBCenter
    {
        public static SuperBCenter Inst = new SuperBCenter();
        public static string ServerToken { get; set; }

        public void Init()
        {
            var token = HttpUtil.LoginAsync($"http://{cfg.AppSettings["serverUrl"]}/inIoT/userServer/tologin",
                new LoginUser()
                {
                    username = "root",
                    pwd = "12345"
                });
            token.Wait();

            if (string.IsNullOrEmpty(token.Result))
            {
                LogHelper.Error($"LoginAsync {cfg.AppSettings["serverUrl"]} error");
                return;
            }
            ServerToken = JsonSrialize.Desrialize<LoginResponse>(token.Result).data.token;

            var req1 = HttpUtil.GetAsync<GroupResResponse>($"http://{cfg.AppSettings["serverUrl"]}/inIoT/devServer/tagGroup/G433635714868056",
                ServerToken);

            req1.Wait();
            var req =  HttpUtil.PostAsync($"http://{cfg.AppSettings["serverUrl"]}/inIoT/devServer/deviceRes",
                   JsonSrialize.Srialize(new DevModel()
                   {
                       dvIsid = "dfsdf",
                       dvName = "ssssssf",
                       dvTypeCode = "15",
                       managerIsid = cfg.AppSettings["managerIsid"],
                       nodeIsid = "100114",
                       addrRes = new MqttCommon.Setup.AddResource()
                       {
                           communicationType = 1,
                           dvAddr1 = "1.1.1.1",
                           dvAddr2 = "2222"
                       }
                   }), SuperBCenter.ServerToken);


            SdkMgr.Inst.Init().Wait();
            //   SuperBRepertory.Inst.SynchronizeDb().Wait();

            //    MqttMgr.Inst.Init().Wait();

            //   SetupMgr.Inst.Init().Wait();
            return;


            string ss = JsonSrialize.Srialize(new DevModel[]
            {
              new DevModel() {  addrRes = new AddResource()
                {
                    communicationType = 0,
                    connectParam1 = "9600",
                    connectParam2 = "8",
                    connectParam3 = "1",
                    connectParam4 = "1",
                    dvAddr1 = "3",
                    dvAddr2 = "1",
                },
                dvName = "sdf",
                dvTypeCode = "10",
                managerIsid = "mgrTest2",
                nodeIsid = "zhc-yq",
                dvIsid = Uuid.Create16Token(), }
            });
            HttpUtil.PostAsync("http://10.10.12.164:8080/inIoT/devServer/deviceRes/list", ss, token.Result);
        }

    }
}
