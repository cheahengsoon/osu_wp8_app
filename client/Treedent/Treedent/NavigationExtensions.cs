using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Treedent
{
    // Credit goes to stack overflow user zik.
    // http://stackoverflow.com/questions/13654379/how-do-i-pass-non-string-parameters-between-pages-in-windows-phone-8
    public static class NavigationExtensions
    {
        private static object _navigationData = null;

        public static void Navigate(this NavigationService service, string page, object data)
        {
            _navigationData = data;
            service.Navigate(new Uri(page, UriKind.Relative));
        }

        public static object GetLastNavigationData(this NavigationService service)
        {
            object data = _navigationData;
            _navigationData = null;
            return data;
        }

        public static void Navigate(this NavigationService service, string page)
        {
            service.Navigate(new Uri(page, UriKind.Relative));
        }

    }
}
