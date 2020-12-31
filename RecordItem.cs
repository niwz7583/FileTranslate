using AheadTec;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace FileTranslate
{
    /// <summary>
    /// 状态枚举类型。
    /// </summary>
    public enum ItemStatu
    {
        [Description("初始中")]
        Initial = 0,
        [Description("上传中")]
        Uploading = 1,
        [Description("上传失败")]
        UploadFailed = 2,
        [Description("转换中")]
        Converting = 3,
        [Description("转换失败")]
        ConvertFalied = 4,
        [Description("已转换")]
        Converted = 5,
        [Description("查询中")]
        Querying = 6,
        [Description("已翻译")]
        Inquired = 7,
        [Description("生成中")]
        Documenting = 8,
        [Description("已完成")]
        Finished = 9,
    }

    /// <summary>
    /// 录音记录项。
    /// </summary>
    [Serializable]
    public class RecordItem
    {
        public string Path { get; set; }
        public string Text { get; set; }
        public int ImageIndex { get; set; }
        public long Length { get; set; }

        ItemStatu _statu = ItemStatu.Initial;

        public ItemStatu Statu
        {
            get { return _statu; }
            set
            {
                if (_statu != value)
                {
                    _statu = value;
                    if (StatuChanged != null)
                        StatuChanged.BeginInvoke(this, EventArgs.Empty, null, null);
                }
            }
        }
        public string OssKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime TaskStamp { get; set; }
        /// <summary>
        /// 提交到阿里识别成功后，返回的任务号。
        /// </summary>
        public string TaskId { get; set; }
        /// <summary>
        /// 识别成功后的结果。
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// 状态改变触发事件。
        /// </summary>
        public event EventHandler StatuChanged;

        RecordItem()
        {

        }

        public RecordItem(string path)
        {
            FileInfo info = new FileInfo(path);

            Path = path;
            Text = info.Name;
            ImageIndex = string.Compare(info.Extension, ".mp3", true) == 0 ? 0 : 1;
            Length = info.Length;
            Statu = ItemStatu.Initial;

            OssKey = string.Format("{0:yyyy-MM-dd}/{1}", DateTime.Now, Text);
        }

        public override string ToString()
        {
            return string.Format("路径：{0}\r\n大小：{1:0.0}KB\r\n状态：{2}", this.Path, (this.Length / 1024.0),
              Utils.GetEnumDescription(this.Statu));
        }
    }

    [Serializable]
    public class Sentence
    {
        public int ChannelId { get; set; }
        public int BeginTime { get; set; }
        public int EndTime { get; set; }
        public string Text { get; set; }
        public int SilenceDuration { get; set; }
        public int SpeechRate { get; set; }
        public int EmotionValue { get; set; }
    }

    public class RecordItemHelper
    {
        [NonSerialized]
        static Dictionary<ItemStatu, Color> _itemStatuColors = null;
        /// <summary>
        /// 状态对应的颜色。
        /// </summary>
        public static Dictionary<ItemStatu, Color> ItemStatuColors
        {
            get
            {
                if (_itemStatuColors == null)
                {
                    _itemStatuColors = new Dictionary<ItemStatu, Color>();
                    _itemStatuColors.Add(ItemStatu.Initial, Color.White);
                    for (int idx = 1; idx < 6; idx++)
                        _itemStatuColors.Add((ItemStatu)idx, Color.Yellow);
                    _itemStatuColors.Add(ItemStatu.Querying, Color.LightPink);
                    _itemStatuColors.Add(ItemStatu.Inquired, Color.LightPink);
                    _itemStatuColors.Add(ItemStatu.Documenting, Color.LightPink);
                    _itemStatuColors.Add(ItemStatu.Finished, Color.Green);
                }
                return _itemStatuColors;
            }
        }

        /// <summary>
        /// 获取缓存路径。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetCachePath(string path, string ext = "")
        {
            if (string.IsNullOrEmpty(ext))
                ext = "rec";
            //
            var md5str = Utils.GetMD5String(path);
            var subdir = md5str.Substring(0, 3);
            //
            var dir = string.Format("{0}\\{1}", Utils.CacheDirectory, subdir);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            //
            var cachefile = string.Format("{0}\\{1}\\{2}.{3}", Utils.CacheDirectory, subdir, md5str, ext);

            return cachefile;
        }

        /// <summary>
        /// 通过路径，先从本地缓存目录检查是否存在内容。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static RecordItem GetRecordItem(string path)
        {
            RecordItem item = null;
            //路径md5
            var cachefile = GetCachePath(path);
            //
            if (File.Exists(cachefile))
            {
                try
                {
                    var buffer = File.ReadAllText(cachefile);
                    try
                    {
                        var bytes = Convert.FromBase64String(buffer);
                        buffer = Encoding.Default.GetString(bytes);
                    }
                    catch
                    {
                    }
                    //
                    item = JsonConvert.DeserializeObject<RecordItem>(buffer);
                }
                catch (Exception ex)
                {
                    LogHelper.Write("GetRecordItem", "读取本地缓存时出错：{0}", ex.Message);
                }
            }
            //
            if (item == null)
                item = new RecordItem(path);
            //
            return item;
        }

        /// <summary>
        /// 更新本地缓存数据。
        /// </summary>
        /// <param name="item"></param>
        public static void UpdateRecordCache(RecordItem item)
        {
            //路径md5
            var cachefile = GetCachePath(item.Path);
            //
            try
            {
                var strItem = JsonConvert.SerializeObject(item);
                var buffer = Convert.ToBase64String(Encoding.Default.GetBytes(strItem));
                //
                File.WriteAllText(cachefile, buffer);
            }
            catch (Exception ex)
            {
                LogHelper.Write("UpdateRecordCache", "将录音项[{0}]写入本地缓存时出错：{1}", item.OssKey, ex.Message);
            }
        }

        /// <summary>
        /// 获取录音项里识别的句子列表。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static List<Sentence> GetRecordItemSentences(RecordItem item)
        {
            //
            List<Sentence> sents = new List<Sentence>();
            //
            var oResponse = JObject.Parse(item.Result);
            var oResult = (JArray)oResponse["Result"]["Sentences"];
            //
            for (int idx = 0; idx < oResult.Count; idx++)
            {
                var st = oResult[idx].ToObject<Sentence>();
                sents.Add(st);
            }
            //排序
            sents.Sort((x, y) => x.BeginTime.CompareTo(y.BeginTime));
            return sents;
        }

        /// <summary>
        /// 时间转换成 时分秒的样子
        /// </summary>
        /// <param name="tm"></param>
        /// <returns></returns>
        public static string FormatDuration(int tm)
        {
            int hour = 0, min = 0, sec = 0, ms = 0;

            int seconds = tm / 1000;

            hour = seconds / 3600;
            min = (seconds % 3600) / 60;
            sec = seconds % 60;
            ms = tm % 1000;
            //
            if (hour > 0)
                return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", hour, min, sec, ms);

            return string.Format("{0:D2}:{1:D2}.{2:D3}", min, sec, ms);
        }
    }
}