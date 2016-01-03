using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Diagnostics;
using System.Data;

using System.Web;
using System.Reflection;
using Tks.Database;

namespace Tks.Log
{
    public class Logger
    {
        private static Logger instance = null;
        private static IDatabase db = null;
        private static Assembly callingAssembly = null;

        ~Logger()
        {
            try
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                    sw = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                    fs = null;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Logデストラクタ:" + ex.Message);
            }
                
            instance = null;
        }

        public static Logger GetDefault()
        {
            if (instance == null)
            {
                instance = new Logger();
                instance.Enc = Encoding.UTF8;
            }
            return instance;
        }

        public static Logger GetLogger(Type t)
        {

            callingAssembly = Assembly.GetCallingAssembly();
            if (instance == null)
            {
                instance = new Logger();
                instance.Enc = Encoding.UTF8;
            }
            Config.Configurator.Configure(callingAssembly);
            _WriteFilePath = Config.Configurator.LogFilename;
            return instance;
        }

        private FileStream fs =  null;
        private StreamWriter sw = null;
        public Encoding Enc { get; set; }


        // ログファイル関連
        private static string _WriteFilePath = "";
        public string WriteFilePath 
        {
            get
            {
                return _WriteFilePath;
            }
            set
            {
                _WriteFilePath = value;
            }
        }

        private static bool _WriteFile = true;
        public bool WriteFile 
        {
            get
            {
                return _WriteFile;
            }
            set
            {
                _WriteFile = value;
            }
        }

        // ログテーブル関連
        private static bool _WriteTable = false;
        public bool WriteTable 
        {
            get
            {
                return _WriteTable;
            }
            set
            {
                _WriteTable = value;
            }
        }

        // DEBUG
        public void WriteDebug(string message1)
        {
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();

            var stack = new StackTrace(1, false);
            this.Write(DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), "DEBUG", getThread(), stack.GetFrame(0).GetMethod().ToString(), message1);
        }

        // INFO
        public void WriteInfo(string message1)
        {
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();

            var stack = new StackTrace(1, false);
            this.Write(DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), "INFO", getThread(), stack.GetFrame(0).GetMethod().ToString(), message1);
        }

        // WARN
        public void WriteWarn(string message1)
        {
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();

            var stack = new StackTrace(1, false);
            this.Write(DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), "WARN", getThread(), stack.GetFrame(0).GetMethod().ToString(), message1);
        }

        // ERROR
        public void WriteError(string message1)
        {
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();

            var stack = new StackTrace(1, false);
            this.Write(DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), "ERROR", getThread(), stack.GetFrame(0).GetMethod().ToString(), message1);
        }
        public void WriteError(Exception e)
        {
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();

            var stack = new StackTrace(1, false);
            this.Write(DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), "ERROR", getThread(), stack.GetFrame(0).GetMethod().ToString(), e.Message);
        }

        // FATAL
        public void WriteFatal(string message1)
        {
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();

            var stack = new StackTrace(1, false);
            this.Write(DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), "FATAL", getThread(), stack.GetFrame(0).GetMethod().ToString(), message1);
        }

        // 実行パスを取得する
        private static string GetCurrentPath()
        {

            string path = "";

            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/";
            }
            else
            {
                HttpServerUtility server = context.Server;
                path = server.MapPath("./");
            }

            return path;
        }

        private string getThread()
        {

            int processId = 0;

            // ここでプロセスIDまたはリクエストIDを取得して返す
            try
            {

                HttpContext context = HttpContext.Current;

                if (context == null)
                {
                    processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                }
                else
                {
                    //HttpServerUtility server = context.Server;
                    //HttpRequest request = context.Request;
                    processId = System.Threading.Thread.CurrentThread.ManagedThreadId;

                }

            }
            catch (Exception ex)
            {
            }
            return processId.ToString();
        }

        // ログ出力
        private void Write(string date, string level, string thread, string logger, string message)
        {

            try
            {
                // ファイルへの書き込みフラグONの場合
                if (WriteFile)
                {

                    if (fs == null)
                    {

                        if (WriteFilePath == null || string.Empty.Equals(WriteFilePath))
                        {
                            Config.Configurator.Configure(callingAssembly);
                            WriteFilePath = Config.Configurator.LogFilename;
                        }

                        string path = Path.GetDirectoryName(WriteFilePath);
                        if (string.Empty.Equals(path))
                        {
                            // パスが指定されていない場合は実行パスを取得する
                            path = GetCurrentPath();
                            WriteFilePath = path + WriteFilePath;
                        }
                        else
                        {
                            // パスが存在しなければ作成する
                            if (Directory.Exists(path) == false)
                            {
                                Directory.CreateDirectory(path);
                            }
                        }

                        fs = new FileStream(WriteFilePath, FileMode.Append);
                        sw = new StreamWriter(fs, this.Enc);
                    }

                    sw.WriteLine(date + " " + level + " [" + thread + "] " + message);
                    sw.Flush();

                }

                // テーブルへの書き込みフラグONの場合
                if (WriteTable)
                {

                    if (db == null)
                    {
                        db = new SQLServer();
                    }

                    //string sqlString = "INSERT INTO LOG (DATETIME, THREAD, FUNC, MESSAGE) VALUES ({0}, '{1}','{2}','{3}')";
                    string sqlString = "INSERT INTO LOG (DATETIME, THREAD, LOG_LEVEL, LOGGER, MESSAGE) VALUES (cast('{0}' as DateTime), '{1}','{2}','{3}','{4}')";
                    sqlString = string.Format(sqlString, date, thread, level, logger, message);

                    db.ExecuteCommit(sqlString);

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Log.Writeエラー : " + ex.Message);
                throw ex;
            }
        }
    }
}
