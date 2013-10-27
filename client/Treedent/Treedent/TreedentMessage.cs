using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Treedent
{
    [DataContract]
    public class TreedentMessage
    {
        // Valid codes:
        // 1 - register successful
        // 2 - login successful
        // 3 - session expired
        // 4 - logout successful
        // 5 - upload successful
        // 6 - got next photo
        // 7 - end of photos
        // 8 - guess found
        // 9 - no guess
        // 10 - guess submitted
        // 999 - error
        [DataMember]
        public int Code;

        [DataMember]
        public Dictionary<string, string> DataMap;

        public TreedentMessage()
        {

        }
    }
}
