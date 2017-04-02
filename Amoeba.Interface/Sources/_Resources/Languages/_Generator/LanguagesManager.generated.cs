using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    partial class LanguagesManager
    {
        public string Global_FontFamily { get { return this.Translate("Global_FontFamily"); } }
        public string Global_DateTime_StringFormat { get { return this.Translate("Global_DateTime_StringFormat"); } }
        public string Languages_English { get { return this.Translate("Languages_English"); } }
        public string Languages_Japanese { get { return this.Translate("Languages_Japanese"); } }
        public string MainWindow_Info { get { return this.Translate("MainWindow_Info"); } }
        public string MainWindow_Store { get { return this.Translate("MainWindow_Store"); } }
        public string OptionsWindow_Connection { get { return this.Translate("OptionsWindow_Connection"); } }
        public string OptionsWindow_TcpConnection { get { return this.Translate("OptionsWindow_TcpConnection"); } }
        public string OptionsWindow_I2pConnection { get { return this.Translate("OptionsWindow_I2pConnection"); } }
        public string InfoControl_Type { get { return this.Translate("InfoControl_Type"); } }
    }
}
