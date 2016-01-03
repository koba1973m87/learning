using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tks.Log.Config
{
    [AttributeUsage(AttributeTargets.Assembly)]
    [Serializable]
    public class ConfiguratorAttribute : Attribute
    {

        string _configFile;

        public string ConfigFile
        {
            get { return this._configFile; }
            set { this._configFile = value; }
        }
    }
}
