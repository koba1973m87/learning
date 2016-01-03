using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Web;

namespace Tks.Log.Config
{
    public sealed class Configurator
    {

        // コンストラクタ
        private Configurator()
        {
        }

        // ログファイル名
        public static string LogFilename { get; set; }

        static public void Configure(FileInfo configFile)
        {

            try
            {

                if (configFile != null)
                {
                    // 存在した場合
                    var fs = configFile.Open(FileMode.Open, FileAccess.Read);
                    XmlDocument doc = new XmlDocument();

                    XmlValidatingReader xmlReader = new XmlValidatingReader(new XmlTextReader(fs));
                    xmlReader.ValidationType = ValidationType.None;
                    xmlReader.EntityHandling = EntityHandling.ExpandEntities;

                    // Xmlファイル読み込み
                    doc.Load(xmlReader);

                    XmlNodeList nodelist = doc.SelectNodes("/root/log/filename");

                    LogFilename = nodelist[0].InnerText;

                    fs.Close();
                    fs.Dispose();

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

        }

        // 呼び出し元のアセンブリから取得
        static public void Configure(Assembly assembly)
        {
            // 存在しない場合は属性から取得する
            object[] configures = Attribute.GetCustomAttributes(assembly, typeof(ConfiguratorAttribute), false);
            if (configures == null || configures.Length == 0)
            {
                // 属性が指定されていない場合
            }
            else
            {
                // 属性が指定されていた場合は１つ目だけが適用される
                ConfiguratorAttribute attr = configures[0] as ConfiguratorAttribute;
                HttpContext context = HttpContext.Current;
                var filePath = "";
                if (context == null)
                {
                    filePath = attr.ConfigFile;
                }
                else
                {
                    HttpServerUtility server = context.Server;
                    filePath = server.MapPath(attr.ConfigFile);
                }

                var fi = new FileInfo(filePath);
                Configure(fi);
            }
        }

    }
}
