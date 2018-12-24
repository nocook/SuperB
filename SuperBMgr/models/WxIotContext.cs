using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr.models
{

    class WxIotContext : DbContext
    {
        public DbSet<SqlProxyRes> proxyress { get; set; }

        public DbSet<SqlTagGroup> taggroup { get; set; }

        public DbSet<SqlDeviceRes> deviceres { get; set; }

        public DbSet<SqlTagConfigRes> tagconfigres { get; set; }

        public DbSet<SqlRealtimeData> realtimedata { get; set; }

        public DbSet<SqlAddrres> addrres { get; set; }

        public DbSet<SqlNodeJoinTag> nodeTags { get; set; }

        public DbSet<SqlAlarmRes> alarmres { get; set; }

        public DbSet<SqlAlarmData> alarmhistorydata { get; set; }

        public DbSet<SqlLinkRes> linkRes { get; set; }

        public DbSet<SqlLinkActionRes> linkActionRes { get; set; }

        public DbSet<SqlLinkTriggerRes> linkTriggerRes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var str =$"server={cfg.AppSettings["mysqlUri"]};userid={cfg.AppSettings["mysqlUser"]}" +
                $";pwd={cfg.AppSettings["mysqlPasswd"]};port={cfg.AppSettings["mysqlPort"]};" +
                $"database={cfg.AppSettings["iotDbName"]};SslMode=None;Charset=utf8";
            optionsBuilder.UseMySQL(str);
        }

    }

    class SqlAlarmRes
    {
        [Key]
        public int id { get; set; }
        public string alarmIsid { get; set; }
        public string alarmName { get; set; }
        public string tagIsid { get; set; }
        public int alarmType { get; set; }
        public string upperLimit { get; set; }
        public string lowLimit { get; set; }
        public string alarmDesc { get; set; }
        public int? alarmLevel { get; set; }
        public int? alarmEnable { get; set; }
    }

    class SqlAlarmData
    {
        [Key]
        public int id { get; set; }
        public string alarmIsid { get; set; }
        public string tagIsid { get; set; }
        public string codeValue { get; set; }
        public string alarmContent { get; set; }
        public DateTime? alarmTime { get; set; }
        public DateTime? recoverTime { get; set; }
        public string recoverValue { get; set; }
        public int? alarmConfirm { get; set; }
        public string confirmUserName { get; set; }
        public DateTime? confirmTime { get; set; }
        public string confirmDescription { get; set; }
        public string alarmCleaner { get; set; }
        public DateTime? cleanTime { get; set; }
        public string alarmStatus { get; set; }
        public string tagName { get; set; }
    }

    class SqlNodeJoinTag
    {
        [Key]
        public string tagIsid { get; set; }
        public string nodeIsid { get; set; }
    }

    class SqlDeviceRes
    {
        [Key]
        public int id { get; set; }
        public string dvIsid { get; set; }
        public string nodeIsid { get; set; }
        public string dvName { get; set; }
        public string dvTypeCode { get; set; }
        public string dvBrandid { get; set; }
        public string managerIsid { get; set; }
        public int ratedPower { get; set; }
        public string createTime { get; set; }
    }

    class SqlProxyRes
    {
        [Key]
        public int id { get; set; }
        public string proxyId { get; set; }
        public string proxyName { get; set; }
        public string proxyPath { get; set; }
        public int maxTagNum { get; set; }
        public string transProtocolId { get; set; }
        public string managerIsid
        {
            get; set;
        }
    }

    class SqlTagGroup
    {
        [Key]
        public int id { get; set; }
        public string groupIsid { get; set; }
        public string transProtocolId { get; set; }
        public int? collectPeriod { get; set; }
        public int? overTime { get; set; }
        public int? relinkPeriod { get; set; }
        public int? relinkCount { get; set; }
        public string dvIsid { get; set; }
        public string nodeIsid { get; set; }
    }

    class SqlTagConfigRes
    {
        [Key]
        public int id { get; set; }
        public string tagIsid { get; set; }
        public string tagName { get; set; }
        public string groupIsid { get; set; }
        public string tagAddr { get; set; }
        public string dataType { get; set; }
        public string ruleIsid { get; set; }
        public string ioType { get; set; }
        public float multiplier { get; set; }
        public float addition { get; set; }
        public string tagTypeCode { get; set; }
    }

    class SqlRealtimeData
    {
        [Key]
        public string tagIsid { get; set; }
        public string codeValue { get; set; }
        public string tagName { get; set; }
        public string tagTypeCode { get; set; }
        public DateTime? updateTime { get; set; }
    }

    class SqlAddrres
    {
        [Key]
        public int id { get; set; }
        public string dvIsid { get; set; }
        public int communicationType { get; set; }
        public string dvAddr1 { get; set; }
        public string dvAddr2 { get; set; }
        public string dvAddr3 { get; set; }
        public string connectParam1 { get; set; }
        public string connectParam2 { get; set; }
        public string connectParam3 { get; set; }
        public string connectParam4 { get; set; }
    }

    public class SqlLinkRes
    {
        [Key]
        public int id { get; set; }
        public string linkIsid { get; set; }
        public string linkName { get; set; }
        public string triggerExpress { get; set; }
        public string actionExpress { get; set; }
        public int enable { get; set; }
        public int count { get; set; }
    }

    public class SqlLinkActionRes
    {
        [Key]
        public int id { get; set; }
        public string actionIsid { get; set; }
        public int notifyUser { get; set; }
        public string targetType { get; set; }
        public string targetIsid { get; set; }
        public string targetValue { get; set; }
    }

    public class SqlLinkTriggerRes
    {
        [Key]
        public int id { get; set; }
        public string triggerIsid { get; set; }
        public string triggerType { get; set; }
        public string triggerSourceIsid { get; set; }
        public string triggerValue { get; set; }
        public string triggerLoggic { get; set; }
    }
}
