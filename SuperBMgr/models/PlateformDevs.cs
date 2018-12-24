using System;
using System.Collections.Generic;
using System.Text;
using SuperBDevAccess;
using SuperBMgr.transfer;
using static SuperBDevAccess.WxBSdk;

namespace SuperBMgr.models
{
    class PlateformDevs
    {
        public TStation Station { get; set; }
        public List<TDevice> DevList { get; set; }

        public IReadOnlyList<TAIC> AiList { get { return _aiList; } }
        private List<TAIC> _aiList;

        public PlateformDevs()
        {
            DevList = new List<TDevice>();
            _aiList = new List<TAIC>();
        }

        public void SetTaic(TAIC ai)
        {
          /*  SuperB_Taic bStru = new SuperB_Taic(ai);
        //    ai.AlarmEnable
            _aiList.Add(ai);*/
        }

        public void SetTaoc(TAOC ao)
        {

        }

        public void SetTdic(TDIC di)
        {
        }

        public void SetTdoc(TDOC doc)
        {
        }

        public void SetTdsc(TDSC dsc)
        {
        }
    }


}
