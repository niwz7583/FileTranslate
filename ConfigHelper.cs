using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Configuration;

namespace AheadTec
{

    /// <summary>
    /// 配置参数值发生变化时触发事件的委托。
    /// </summary>
    public delegate void PropertyChangedEventHandler(string key, string oldValue, string newValue);

    /// <summary>
    /// 配置管理器。
    /// </summary>
    public class ConfigHelper
    {
        static readonly object _syncRoot = new object();
        const string DBPrefix = "AheadTec.Core.ConnectionString";
        static event PropertyChangedEventHandler _evPropertyChanged;
        /// <summary>
        /// 配置属性值发生变化时触发的事件。
        /// </summary>
        public static event PropertyChangedEventHandler PropertyChanged
        {
            add { lock (_syncRoot)_evPropertyChanged += value; }
            remove { lock (_syncRoot)_evPropertyChanged -= value; }
        }
        /// <summary>
        /// 获取配置文件中的值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValue<T>(string key)
        {
            string obj = GetValue(key);
            if (!string.IsNullOrEmpty(obj))
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(obj.GetType()))
                {
                    object retValue = converter.ConvertFrom(obj);
                    if (retValue != null)
                        return (T)retValue;
                }
            }
            return default(T);
        }
        /// <summary>
        /// 获取配置属性值。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
        /// <summary>
        /// 核心数据库连接串。
        /// </summary>
        public static string ConnectString
        {
            get { return ConfigurationManager.ConnectionStrings[DBPrefix].ConnectionString; }
            set
            {
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var connections = config.ConnectionStrings.ConnectionStrings;
                    connections[DBPrefix].ConnectionString = value;
                    config.Save(ConfigurationSaveMode.Full);
                    ConfigurationManager.RefreshSection("connectionStrings");
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// 保存配置值。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetValue(string key, string value)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string _oldValue = appSettings[key];
                if (string.Compare(_oldValue, value, true) != 0)
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var settings = config.AppSettings.Settings;
                    settings.Remove(key);
                    settings.Add(key, value);
                    config.Save(ConfigurationSaveMode.Full);
                    ConfigurationManager.RefreshSection("appSettings");

                    //触发事件
                    if (_evPropertyChanged != null)
                        _evPropertyChanged(key, _oldValue, value);
                }
            }
            catch
            {
            }
        }
        /// <summary>
        /// 获取配置值是否为布尔值。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsBoolean(string key)
        {
            string _result = string.Empty;
            try
            {
                _result = GetValue(key);
                if (!string.IsNullOrEmpty(_result))
                    _result = _result.ToLower();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
            }
            return _result == "true" || _result == "yes" || _result == "1";
        }
        /// <summary>
        /// 获取配置值，不存在时返回默认值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValue<T>(string key, T defaultValue)
        {
            string obj = GetValue(key);
            if (!string.IsNullOrEmpty(obj))
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(obj.GetType()))
                {
                    object retValue = converter.ConvertFrom(obj);
                    if (retValue != null)
                        return (T)retValue;
                }
            }
            return defaultValue;
        }
    }
}
