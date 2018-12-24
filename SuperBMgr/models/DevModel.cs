using MqttCommon.Setup;
using System;
using System.Collections.Generic;
using System.Text;

namespace SuperBMgr.models
{
    class DevModel
    {
        public string dvIsid { get; set; }
        public string nodeIsid { get; set; }
        public string dvName { get; set; }
        public string dvTypeCode { get; set; }
        public string dvBrandid { get; set; }
        public string managerIsid { get; set; }
        public int ratePower { get; set; }
        public AddResource addrRes { get; set; }
    }


    class WebList<T>
    {
        public WebData<T> data;
    }

    class WebData<T>
    {
        public int total { get; set; }
        public List<T> data { get; set; }
    }

    /*  class AddResource
      {
          public int communicationType { get; set; }
          public string connectParam1 { get; set; }
          public string connectParam2 { get; set; }
          public string connectParam3 { get; set; }
          public string connectParam4 { get; set; }
          public string dvAddr1 { get; set; }
          public string dvAddr2 { get; set; }
      }


       class GroupRes
      {
          public string managerIsid { get; set; }
          public string nodeIsid { get; set; }
          public string dvIsid { get; set; }

          public string groupIsid { get; set; }
          public AddResource addRess { get; set; }
          public string transProtocolId { get; set; }
          public int? collectPeriod { get; set; }
          public int? overTime { get; set; }
          public int? relinkPeriod { get; set; }
          public int? relinkCount { get; set; }
          public TagRes[] tagRes { get; set; }

          /// <summary>
          /// 拷贝给其他的Group，除了Tag点位集合和groupIsid
          /// </summary>
          /// <param name="groupRes"></param>
          public void CopyToNoTags(GroupRes groupRes)
          {
              // groupRes.groupIsid = groupIsid;
              groupRes.managerIsid = managerIsid;
              groupRes.nodeIsid = nodeIsid;
              groupRes.dvIsid = dvIsid;
              groupRes.addRess = addRess;
              groupRes.transProtocolId = transProtocolId;
              groupRes.collectPeriod = collectPeriod;
              groupRes.overTime = overTime;
              groupRes.relinkPeriod = relinkPeriod;
              groupRes.relinkCount = relinkCount;
          }

          public void CopyToIncludTags(GroupRes groupRes)
          {
              CopyToNoTags(groupRes);
              groupRes.tagRes = tagRes;
          }
      }*/
}
