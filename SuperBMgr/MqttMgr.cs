using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using cfg = System.Configuration.ConfigurationManager;
using topicFilter = MQTTnet.Server.MqttTopicFilterComparer;

namespace SuperBMgr
{
    delegate void ReceiveMsgHandle(string topic, byte[] payload);

    class MqttMgr
    {
        ConcurrentDictionary<string, ReceiveMsgHandle> _topicCallbacks;
        IMqttClient _mqttClient;

        public static MqttMgr Inst = new MqttMgr();

        private MqttMgr()
        {
            _topicCallbacks = new ConcurrentDictionary<string, ReceiveMsgHandle>();
        }

        public async Task Init()
        {
            await ConnectMqttServerAsync();

            try
            {
                // 订阅其他模块订阅的主题
                foreach (var cbb in _topicCallbacks)
                {
                    _mqttClient.SubscribeAsync(cbb.Key);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"subscribe failed! => {ex.Message}");
            }
        }

        private async Task ConnectMqttServerAsync()
        {
            if (_mqttClient == null)
            {
                _mqttClient = (new MqttFactory()).CreateMqttClient();
                _mqttClient.ApplicationMessageReceived += _mqttClient_ApplicationMessageReceived;
                _mqttClient.Connected += _mqttClient_Connected;
                _mqttClient.Disconnected += _mqttClient_Disconnected;
            }

            try
            {
                var keep = int.Parse(cfg.AppSettings["EmqKeepAlive"]);
                var options = new MqttClientOptions()
                {
                    ClientId = cfg.AppSettings["MqttClientId"],
                    ProtocolVersion = MQTTnet.Serializer.MqttProtocolVersion.V311,
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = cfg.AppSettings["EmqIp"],
                        Port = int.Parse(cfg.AppSettings["EmqPort"]),
                    },
                    KeepAlivePeriod = new TimeSpan(0, 0, keep * 2),
                    KeepAliveSendInterval = new TimeSpan(0, 0, keep),
                };

                await _mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"connect mqtt failed！=> {ex.Message}");
            }
        }

        /// <summary>
        /// 与emq断掉以后，重新开始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            LogHelper.Error("disconnected mqtt server！");
            new Task(() =>
            {
                Task.Delay(5 * 1000).Wait();
                Init().Wait();
            }).Start();
        }

        private void _mqttClient_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            LogHelper.Info("connected mqtt server！");
        }

        /// <summary>
        /// 将收到的消息分发到各个订阅的模块
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            //             LogHelper.Debug($"received mqtt msg => topic: {e.ApplicationMessage.Topic}\r\n\t" +
            //                 $"payload");

            if (GetTopicCallback(e.ApplicationMessage.Topic, out ReceiveMsgHandle act))
            {
                act(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload);
            }
        }

        /// <summary>
        /// 提供其他模块订阅用，当收到匹配主题以后发给传递进来的委托
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public async Task<bool> SetSubscribe(string topic, ReceiveMsgHandle callBack)
        {
            if (!_topicCallbacks.TryAdd(topic, callBack)) return false;

            await _mqttClient.SubscribeAsync(topic);

            return true;
        }

        public async Task PublishAsync(string topic, string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce)
            => await _mqttClient.PublishAsync(topic, payload, qualityOfServiceLevel);


        public async Task PublishAsync(string topic, byte[] data)
        {
            try
            {
                if (_mqttClient.IsConnected)
                {
                    var appMsg = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(data)
                        .WithAtMostOnceQoS()
                        .WithRetainFlag(false)
                        .Build();
                    await _mqttClient.PublishAsync(appMsg);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private bool GetTopicCallback(string topic, out ReceiveMsgHandle act)
        {
            act = null;
            var orgTpStrs = topic.Split('/');
            foreach (var pair in _topicCallbacks)
            {
                if (topicFilter.IsMatch(topic, pair.Key))
                {
                    act = pair.Value;
                    return true;
                }
            }

            return false;
        }

    }
}
