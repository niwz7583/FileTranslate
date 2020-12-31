using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AheadTec
{
    public enum LogType
    {
        Error = 1,
        Warn = 2,
        Infor = 4,
    }
    public class LogHelper
    {
        /// <summary>是否启用日志记录。</summary>
        public static bool Enabled { get; set; }
        /// <summary>是否使用系统事件日志模式。</summary>
        public static bool UseEventLog { get; set; }
        //日志文件参数
        static string _traceFile = "Trace.log";
        static string _traceDir = "Logs";
        static string _sourceFlag = "AheadTec Platform";
        static long _fileMaxLen = 4 * 1024 * 1024; //4M        
        static MyTextWriter _writer = null;

        static LogHelper()
        {
            Enabled = ConfigHelper.IsBoolean("AheadTec.LogEnabled");
            if (!Enabled)
                return;

            UseEventLog = ConfigHelper.IsBoolean("AheadTec.UseEventLog");
            if (!UseEventLog)
            {
                _traceFile = ConfigHelper.GetValue("AheadTec.LogFileName");
                if (string.IsNullOrEmpty(_traceFile))
                    _traceFile = "Trace.log";
                _fileMaxLen = ConfigHelper.GetValue<long>("AheadTec.LogFileSize", 4 * 1024 * 1024);
            }
            else
            {
                _sourceFlag = ConfigHelper.GetValue("AheadTec.EventSource");
                if (string.IsNullOrEmpty(_sourceFlag))
                    _sourceFlag = "AheadTec Platform";
            }
        }

        public static void Write(string flag, Exception ex)
        {
            Write(flag, LogType.Infor, ex);
        }
        public static void Write(string flag, LogType logtype, Exception ex)
        {
            StringBuilder _sb = new StringBuilder();
            _sb.AppendFormat("异常类型：{0}\r\n", ex.GetType().FullName);
            _sb.AppendFormat("异常消息：{0}\r\n", ex.Message);
            _sb.AppendFormat("堆栈跟踪：{0}", new System.Diagnostics.StackTrace(ex, true).ToString());
            Write(flag, logtype, _sb.ToString());
        }

        public static void Write(string flag, string format, params object[] args)
        {
            Write(flag, LogType.Infor, format, args);
        }
        public static void Write(string flag, LogType logtype, string format, params object[] args)
        {
            string msg = format;
            if (args != null && args.Length > 0)
                msg = string.Format(format, args);
            //
            Write(logtype, flag, msg);
        }
        public static void Write(LogType logtype, string flag, string message)
        {
            if (!Enabled)
            {
#if DEBUG
                string _writeLogWithDebug = string.Format("[{0}] [{1}] {2}", flag, logtype, message);
                Console.WriteLine(_writeLogWithDebug);
#endif
                return;
            }

            //string _writeLog = string.Format("[{0}] [{1}] {2}", flag, logtype, message);
            string _writeLog = string.Format("[{0}] {1}", flag, message);
            if (UseEventLog)
            {
                if (!EventLog.SourceExists(_sourceFlag))
                    EventLog.CreateEventSource(_sourceFlag, "Application");

                EventLog.WriteEntry(_sourceFlag, _writeLog, (EventLogEntryType)((int)logtype));
            }
            else
            {
                if (_writer == null)
                {
                    string _dirName = ConfigHelper.GetValue("AheadTec.LogDirectory");
                    if (string.IsNullOrEmpty(_dirName))
                        _dirName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _traceDir);
                    _dirName = Path.GetFullPath(_dirName);
                    if (!Directory.Exists(_dirName))
                        Directory.CreateDirectory(_dirName);
                    string _logFilename = Path.Combine(_dirName, _traceFile);
                    _writer = new MyTextWriter(_logFilename, _fileMaxLen);

                }
                if (logtype == LogType.Error)
                    _writer.SetTextColor(ConsoleColor.Red);
                else if (logtype == LogType.Warn)
                    _writer.SetTextColor(ConsoleColor.Yellow);
                else
                    _writer.SetTextColor(ConsoleColor.Green);
                //
                Console.WriteLine(_writeLog);
                _writer.SetTextColor(ConsoleColor.Green);
            }
        }
    }

    class FileStreamWithBackup : FileStream
    {
        private long _maxLen;
        private string _fileDir;
        private string _fileBase;
        private string _fileExt;
        private DateTime _curDay = DateTime.Now.Date;
        private int _curHour = DateTime.Now.Date.Hour;
        /// <summary>
        /// 按天输出备份文件内容。
        /// </summary>
        public bool LogWithDay { get; set; }
        /// <summary>
        /// 按小时输出备份文件内容。
        /// </summary>
        public bool LogWithHour { get; set; }

        public bool NeedBackup { get; private set; }

        public FileStreamWithBackup(string path, long maxLen)
            : base(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)
        {
            Initialize(path, maxLen);
            this.LogWithDay = ConfigHelper.IsBoolean("AheadTec.LogWithDay");
            this.LogWithHour = ConfigHelper.IsBoolean("AheadTec.LogWithHour");
        }

        private void Initialize(string path, long maxLen)
        {
            if (maxLen <= 0)
                throw new ArgumentOutOfRangeException("无效的文件最大长度。");

            _maxLen = maxLen;
            string fullpath = Path.GetFullPath(path);
            _fileDir = Path.GetDirectoryName(fullpath);
            _fileBase = Path.GetFileNameWithoutExtension(fullpath);
            _fileExt = Path.GetExtension(fullpath);
            Seek(0, SeekOrigin.End);
        }

        public override bool CanRead { get { return false; } }

        public override void Write(byte[] array, int offset, int count)
        {
            if (this.LogWithHour)
                this.NeedBackup = (DateTime.Now.Date != _curDay || DateTime.Now.Date.Hour != _curHour);
            else if (this.LogWithDay)
                this.NeedBackup = DateTime.Now.Date != _curDay;
            else
            {
                int _actualCount = Math.Min(count, array.GetLength(0));
                this.NeedBackup = Position + _actualCount >= _maxLen;
            }
            //写入文件流
            if (!NeedBackup)
            {
                base.Write(array, offset, count);
            }
            else
            {
                BackupAndResetStream();
                Write(array, offset, count);
            }
        }

        private void BackupAndResetStream()
        {
            Flush();
            File.Copy(Name, GetBackupFilename());
            SetLength(0);
        }

        private string GetBackupFilename()
        {
            string _filename = string.Empty;
            if (this.LogWithHour)
            {
                _filename = string.Format("{0}_{1}{2}", _fileBase, _curDay.ToString("yyyy-MM-dd_HH"), _fileExt);
                _curDay = DateTime.Now.Date;
                _curHour = DateTime.Now.Date.Hour;
            }
            else if (this.LogWithDay)
            {
                _filename = string.Format("{0}_{1}{2}", _fileBase, _curDay.ToString("yyyy-MM-dd"), _fileExt);
                _curDay = DateTime.Now.Date;
            }
            else
            {
                _filename = string.Format("{0}_{1}{2}", _fileBase, DateTime.Now.ToString("yyyyMMddHHmmss"), _fileExt);
            }
            return Path.Combine(_fileDir, _filename);
        }
    }

    public class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }

    class MyTextWriter : StreamWriter
    {
        StreamWriter _output;
        bool _isCmd = false;

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public MyTextWriter(string tracefile, long maxlen)
            : base(new FileStreamWithBackup(tracefile, maxlen), Encoding.Default)
        {
            _output = new StreamWriter(Console.OpenStandardOutput(), this.Encoding); //Console的标准输出
            _output.AutoFlush = true;
            this.AutoFlush = true;
            //判断是否为命令行模式
            IntPtr ptr = ConsoleHelper.GetForegroundWindow();
            int processId;
            ConsoleHelper.GetWindowThreadProcessId(ptr, out processId);
            Process process = Process.GetProcessById(processId);
            _isCmd = (process.ProcessName == "cmd");
#if DEBUG
            WriteLine("ProcessName:{0}", process.ProcessName);
#endif
            //
            Console.SetOut(this);
        }

        public override void WriteLine(string value)
        {
            string _newMsg = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"), value);
            _output.WriteLine(_newMsg); //输出至控制台
            base.WriteLine(_newMsg);
        }
        public override void WriteLine(string format, params object[] arg)
        {
            WriteLine(string.Format(format, arg));
        }

        public void SetTextColor(ConsoleColor color)
        {
            if (_isCmd)
                Console.ForegroundColor = color;
        }
    }
}
