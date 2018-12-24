using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using cfg = System.Configuration.ConfigurationManager;

namespace SuperBMgr.models
{
    class SuperBContext : DbContext
    {
        public DbSet<SqlSubPlateformRes> subplateformres { get; set; }
        public DbSet<SqlTStation> tStation { get; set; }
        public DbSet<SqlTDevice> tDevice { get; set; }
        public DbSet<SqlTaic> taic { get; set; }
        public DbSet<SqlTaoc> taoc { get; set; }
        public DbSet<SqlTdic> tdic { get; set; }
        public DbSet<SqlTdoc> tdoc { get; set; }
        public DbSet<SqlTdsc> tdsc { get; set; }
        public DbSet<SqlTagMap> tagmap { get; set; }
        public DbSet<SqlDeviceMap> devicemap { get; set; }
        public DbSet<SqlAlarmMap> alarmmap { get; set; }
        public DbSet<SqlNodeMap> nodemap { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var str = $"server={cfg.AppSettings["mysqlUri"]};userid={cfg.AppSettings["mysqlUser"]}" +
                $";pwd={cfg.AppSettings["mysqlPasswd"]};port={cfg.AppSettings["mysqlPort"]};" +
                $"database={cfg.AppSettings["superBDbName"]};SslMode=None;Charset=utf8";
            optionsBuilder.UseMySQL(str);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SqlNodeMap>()
                .HasKey(n => new { n.nodeIsid, n.objId });
        }
    }


    class SqlSubPlateformRes
    {
        [Key]
        public int id { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public int rightMode { get; set; }
    }

    class SqlTStation
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        public string longitude  { get; set; }
        public string latitude { get; set; }
    }

    class SqlTDevice
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        public int devTypeId { get; set; }
        public string productor { get; set; }
        public string version { get; set; }
        public DateTime? beginRunTime { get; set; }
    }

    class SqlTaic
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        //attribute
        public string maxVal { get; set; }
        public int? alarmLevel { get; set; }
        public int? alarmEnable { get; set; }
        public string minVal { get; set; }
        public string hiLimit1 { get; set; }
        public string loLimit1 { get; set; }
        public string hiLimit2 { get; set; }
        public string loLimit2 { get; set; }
        public string hiLimit3 { get; set; }
        public string loLimit3 { get; set; }
        public string stander { get; set; }
        public string percision { get; set; }
        public int? saved { get; set; }
        public string unit { get; set; }
    }

    class SqlTaoc
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        //attribute
        public string maxVal { get; set; }
        public string minVal { get; set; }
        public int? controlEnable { get; set; }
        public string stander { get; set; }
        public string percision { get; set; }
        public int? saved { get; set; }
        public string unit { get; set; }
    }

    class SqlTdic
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        //attribute
        public int? alarmThresbhold { get; set; }
        public int? alarmLevel { get; set; }
        public int? alarmEnable { get; set; }
        public string desc0 { get; set; }
        public string desc1 { get; set; }
        public int? saved { get; set; }
    }

    class SqlTdoc
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        //attribute
        public int? controlEnable { get; set; }
        public string desc0 { get; set; }
        public string desc1 { get; set; }
        public int? saved { get; set; }
    }

    class SqlTdsc
    {
        [Key]
        public int objId { get; set; }
        public int objTypeId { get; set; }
        public int? parentId { get; set; }
        public string objName { get; set; }
        public string objDesc { get; set; }

        //attribute
        public int? alarmEnable { get; set; }
        public int? saved { get; set; }
    }

    class SqlTagMap
    {
        [Key]
        public int objId { get; set; }
        public string tagIsid { get; set; }
        public int objTypeId { get; set; }
    }

    class SqlDeviceMap
    {
        [Key]
        public int objId { get; set; }
        public string devIsid { get; set; }
        public string groupIsid { get; set; }
    }

    class SqlAlarmMap
    {
        [Key]
        public string alarmIsid { get; set; }
        public int objId { get; set; }
        public int alarmTypeCode{ get; set; }
    }

    class SqlNodeMap
    {
        public string nodeIsid { get; set; }
        public int objId { get; set; }
    }
}
