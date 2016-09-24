using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TrainScheduling.Helper
{
    public class HasInitialized
    {
        private String TrueMsg;
        private String FalseMsg;

        public Boolean Done { get; set; }
        public String Msg
        {
            get
            {
                if (Done)
                {
                    return TrueMsg;
                }
                else
                    return FalseMsg;
            }
        }
        public HasInitialized(String trueMsg, String falseMsg)
        {
            Done = false;
            TrueMsg = trueMsg;
            FalseMsg = falseMsg;
        }
    }

    public class CDisplayData
    {
        public string Name { get; set; }
        public string Outputdata { get; set; }
    }
    public class CDisplayTrainData
    {
        public string ID { get; set; }
        public string speed { get; set; }
        public string delayTime { get; set; }
        public string energy { get; set; }
    }

    public class CmyColor
    {
        public static SolidColorBrush RequiredColor(string colorname)
        {
            var SWcolor = new System.Windows.Media.Color();//               
            var SDcolor = System.Drawing.Color.FromName(colorname);
            SWcolor.R = SDcolor.R;
            SWcolor.G = SDcolor.G;
            SWcolor.B = SDcolor.B;
            SWcolor.A = SDcolor.A;
            return new SolidColorBrush(SWcolor);
        }
    }
}
