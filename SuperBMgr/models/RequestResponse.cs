using System;
using System.Collections.Generic;
using System.Text;
using MqttCommon.Setup;

namespace SuperBMgr.models
{
    public class RequestResponse
    {
        public int codeid { get; set; }
        public string msg { get; set; }
    }

    public class LoginResponse
    {
        public int codeid { get; set; }
        public string msg { get; set; }
        public LoginPaper data { get; set; }
        
    }

    public class GroupResResponse
    {
        public int codeid { get; set; }
        public string msg { get; set; }
        public GroupRes data { get; set; }

    }

    public class LoginPaper
    {
        public string token { get; set; }
    }
}
