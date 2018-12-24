using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr
{
    class RedisHelper
    {
        public static RedisHelper Inst = new RedisHelper();

        private readonly int _overTime = int.Parse(cfg.AppSettings["RedisOverTime"]);

        /// <summary>
        /// 锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 连接对象
        /// </summary>
        private volatile IConnectionMultiplexer _connection;

        /// <summary>
        /// 数据库
        /// </summary>
        private IDatabase _db;

        /// <summary>
        /// 发布/订阅
        /// </summary>
        private ISubscriber _sub;

        private RedisHelper()
        {

            ConfigurationOptions options = new ConfigurationOptions()
            {
                AbortOnConnectFail = false,
                EndPoints = { cfg.AppSettings["RedisUrl"] },
            };
            _connection = ConnectionMultiplexer.Connect(options);
            _db = GetDatabase();
            _sub = GetSubscriber();
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        /// <returns></returns>
        protected IConnectionMultiplexer GetConnection()
        {
            if (_connection != null && _connection.IsConnected)
            {
                return _connection;
            }
            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected)
                {
                    return _connection;
                }

                if (_connection != null)
                {
                    _connection.Dispose();
                }

                ConfigurationOptions options = new ConfigurationOptions()
                {
                    AbortOnConnectFail = false,
                    EndPoints = { cfg.AppSettings["RedisUrl"] },
                };
                _connection = ConnectionMultiplexer.Connect(options);
            }

            return _connection;
        }

        /// <summary>
        /// 获取数据库
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        protected IDatabase GetDatabase(int? db = null)
        {
            return GetConnection().GetDatabase(db ?? -1);
        }

        /// <summary>
        /// 获取数据库
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        protected ISubscriber GetSubscriber()
        {
            return GetConnection().GetSubscriber();
        }


        public bool SetJson2Bytes(string key, object data)
        {
            if (data == null)
            {
                LogHelper.Error("redis Set2Bytes error => data is null");
                return false;
            }
            var entryBytes = Serialize(data);

            try
            {
                return _db.StringSet(key, entryBytes, TimeSpan.FromSeconds(_overTime));
            }
            catch (Exception ex)
            {
                LogHelper.Error("SetJson2Bytes failde\r\n" + ex);
                return false;
            }
        }

        public IBatch GetBatch()
        {
            return _db.CreateBatch();
        }

        public bool SetJson2String(string key, object data)
        {
            if (data == null)
            {
                LogHelper.Error("redis Set2String error => data is null");
                return false;
            }
            var entryBytes = JsonConvert.SerializeObject(data);

            try
            {
                return _db.StringSet(key, entryBytes, TimeSpan.FromSeconds(_overTime));
            }
            catch (Exception ex)
            {
                LogHelper.Error("SetJson2String failde\r\n" + ex);
                return false;
            }
        }


        public bool SetString(string key, string str)
        {
            if (str == null)
            {
                LogHelper.Error("redis SetString error => str is null");
                return false;
            }
            try
            {
                return _db.StringSet(key, str);
            }
            catch (Exception ex)
            {
                LogHelper.Error("SetString failde\r\n" + ex);
                return false;
            }
        }

        public bool KeyDelete(string key)
        {
            return _db.KeyDelete(key);
        }

        public string GetString(string key)
        {
            return _db.StringGet(key);
        }

        public async Task SubscribeAsync(string channel, Func<string, string, Task> handler)
        {
            var ch = new RedisChannel(channel, RedisChannel.PatternMode.Literal);
            try
            {
                var tsk = _sub.SubscribeAsync(ch, (chn, vl) => handler(chn, vl));
                await tsk;
                if (!tsk.IsCompletedSuccessfully)
                {
                    LogHelper.Error($"SubscribeAsync failed => channel={channel}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("SubscribeAsync failde\r\n" + ex);
            }
        }

        public async Task PublishAsync(string channel, string value)
        {
            var ch = new RedisChannel(channel, RedisChannel.PatternMode.Literal);
            try
            {
                var tsk = _sub.PublishAsync(ch, value);
                await tsk;
                if (!tsk.IsCompletedSuccessfully)
                {
                    LogHelper.Error($"PublishAsync failed => channel={channel}, value={value}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("PublishAsync failde\r\n" + ex);
            }
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="data"></param>
        /// <returns>byte[]</returns>
        private byte[] Serialize(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
