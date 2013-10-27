using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Treedent
{
    public interface ITreedentPage
    {
        void DoShowLogin();
        void DoHideLogin();
        void DoUpdateTop();
        void DoNavigateProfile();
        void DoNavigateRegister();
        void DoNavigateMain();
        void DoHandleTap(object that);
    }
}
