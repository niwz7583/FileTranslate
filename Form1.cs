using AheadTec;
using FileTranslate.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace FileTranslate
{
    public partial class Form1 : ICSU.Controls.UIForm
    {
        /*是否运行状态*/
        static bool _running = true;
        /*本地同步锁对象*/
        static readonly object syncRoot = new object();
        /*设置参数*/
        static Properties.Settings Settings = FileTranslate.Properties.Settings.Default;
        /*总录音任务*/
        static List<RecordItem> recordItems = new List<RecordItem>();
        /*当前正在上传的数量*/
        static Queue<RecordItem> uploadItems = new Queue<RecordItem>();
        /*当前正在获取结果的数量*/
        static Queue<RecordItem> getResultItems = new Queue<RecordItem>();
        /*上传并发线程数量*/
        static int uploadCount = 0;
        /*获取结果并发线程数量*/
        static int getResultCount = 0;
        //当前选中项
        ListViewItem curItem = null;

        public Form1()
        {
            InitializeComponent();
            //
            ListViewGroup[] lvgs ={ new ListViewGroup("Mp3文件", HorizontalAlignment.Center),
                                          new ListViewGroup("Wav文件", HorizontalAlignment.Center)};
            //
            lvRecords.Groups.AddRange(lvgs);
            //
            ImageList il = new ImageList();
            il.Images.Add("mp3", Resources.MP3);
            il.Images.Add("wav", Resources.Wav_Doc);
            il.ColorDepth = ColorDepth.Depth32Bit;
            il.ImageSize = new Size(60, 60);
            //
            lvRecords.LargeImageList = il;
            lvRecords.SmallImageList = il;
            lvRecords.LabelEdit = true;
            lvRecords.ShowItemToolTips = true;

            lvRecords.View = View.LargeIcon;
        }

        private void glassButton1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.Filter = "录音文件(*.mp3,*.wav)|*.mp3;*.wav";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    //MessageBox.Show(string.Format("{0}个录音文件。", ofd.FileNames.Length));
                    LoadFiles(ofd.FileNames);
                }
            }
        }

        /// <summary>
        /// 添加需要上传的文件内容。
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        int LoadFiles(string[] files)
        {
            int count = 0;
            for (int idx = 0; idx < files.Length; idx++)
            {
                string path = files[idx];
                //检查本地是否存在
                if (recordItems.Exists(v => { return string.Compare(v.Path, path, true) == 0; }))
                    continue;
                //添加新项目
                ListViewItem lvi = new ListViewItem();
                //
                var recItem = RecordItemHelper.GetRecordItem(path);
                recordItems.Add(recItem);
                //
                lvi.Text = recItem.Text;
                lvi.BackColor = RecordItemHelper.ItemStatuColors[recItem.Statu];
                lvi.ToolTipText = recItem.ToString();
                lvi.ImageIndex = recItem.ImageIndex;
                //
                lvi.Group = lvRecords.Groups[recItem.ImageIndex];
                lvi.Tag = recItem;
                //
                recItem.StatuChanged += (s, e) => { ChangeRecordItemState(recItem, lvi); };
                //
                lvRecords.Items.Add(lvi);
                //
                if (recItem.Statu == ItemStatu.Initial || recItem.Statu == ItemStatu.UploadFailed)
                {
                    ++count;
                    //加入到上传队列
                    PushItemToUploadQueue(recItem);
                }
            }
            return count;
        }

        /// <summary>
        /// 改变对象状态。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dest"></param>
        void ChangeRecordItemState(RecordItem item, ListViewItem lvi)
        {
            //
            if (this.InvokeRequired)
            {
                MethodInvoker mi = () => { ChangeRecordItemState(item, lvi); };
                this.BeginInvoke(mi);
                return;
            }
            //
            lvi.ToolTipText = item.ToString();

            var color = RecordItemHelper.ItemStatuColors[item.Statu];
            foreach (ListViewItem.ListViewSubItem lvs in lvi.SubItems)
                lvs.BackColor = color;
            //lvi.ForeColor = Color.WhiteSmoke;
        }

        /// <summary>
        /// 录音内容上传线程。
        /// </summary>
        /// <param name="state"></param>
        void RecordItemUploadCallback(object state)
        {
            //上传次数
            int times = 0;
            RecordItem item = null;
            //循环获取并执行上传
            while (_running)
            {
                item = null;
                lock (syncRoot)
                {
                    if (uploadItems.Count > 0)
                        item = uploadItems.Dequeue();
                }
                //
                if (item == null)
                {
                    Thread.Sleep(5000);
                    ++times;
                    //等待到达次数后，退出线程
                    if (times >= 600)
                        break;
                    //
                    continue;
                }
                //
                times = 0;
                //执行上传操作//重试5次
                for (int idx = 0; idx < 5; idx++)
                {
                    if (AliUtils.AppendRecordItemToOSS(item))
                    {
                        //执行识别请求操作成功，则扔入结果获取队列
                        if (AliUtils.CommitToRecognizer(item))
                            PushItemToGetResultQueue(item);
                        break;
                    }
                }
            }
            //
            lock (syncRoot)
                --uploadCount;
        }
        /// <summary>
        /// 将对象扔到上传队列。
        /// </summary>
        /// <param name="item"></param>
        void PushItemToUploadQueue(RecordItem item)
        {
            lock (syncRoot)
            {
                //添加到上传队列
                if (item != null && !uploadItems.Contains(item))
                {
                    uploadItems.Enqueue(item);
                    LogHelper.Write("PushItemToUploadQueue", "加入录音文件:{0}，大小:{1}。", item.Path, item.Length);
                }
                //启动上传线程
                if (uploadCount < Settings.UploadThreadCount)
                {
                    if (ThreadPool.QueueUserWorkItem(RecordItemUploadCallback))
                        ++uploadCount;
                }
            }
        }

        void GetItemResultCallback(object state)
        {
            //等待查询次数
            int times = 0;
            RecordItem item = null;
            //循环获取并执行查询
            while (_running)
            {
                item = null;
                lock (syncRoot)
                {
                    if (getResultItems.Count > 0)
                        item = getResultItems.Dequeue();
                }
                //
                if (item == null)
                {
                    Thread.Sleep(5000);
                    ++times;
                    //等待到达次数后，退出线程
                    if (times >= 600)
                        break;
                    //
                    continue;
                }
                //执行结果查询操作
                times = 0;
                //此处会一直等待结果
                if (item.Statu < ItemStatu.Querying)
                    AliUtils.GetRecognizerResult(item);
                //查询成功
                if (item.Statu == ItemStatu.Inquired)
                {
                    //转换成word文档
                    if (OfficeHelper.CreateDocument(item))
                        item.Statu = ItemStatu.Finished;
                    //缓存本次结果，以便下次使用
                    RecordItemHelper.UpdateRecordCache(item);
                    //删除OSS存储内容
                    if (!Settings.OssKeepRecord)
                        AliUtils.DeleteRecordItemFromOSS(item);
                }
            }
            //
            lock (syncRoot)
                --getResultCount;
        }

        void PushItemToGetResultQueue(RecordItem item)
        {
            lock (syncRoot)
            {
                //添加获取结果队列
                if (item != null && !getResultItems.Contains(item))
                {
                    getResultItems.Enqueue(item);
                    LogHelper.Write("PushItemToGetResultQueue", "加入录音文件:{0}，大小:{1}。", item.Path, item.Length);
                }
                //启动获取线程
                if (getResultCount < Settings.GetResultThreadCount)
                {
                    if (ThreadPool.QueueUserWorkItem(GetItemResultCallback))
                        ++getResultCount;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //
            timer1.Enabled = true;
            //存在本地缓存数据，则加载
            //所有选择项转换完成之后，清除本地缓存
            //账号设置提醒
            if (string.IsNullOrEmpty(Settings.AccessKeyId) || string.IsNullOrEmpty(Settings.AccessKeySecret)
                || string.IsNullOrEmpty(Settings.AppKeyId) || string.IsNullOrEmpty(Settings.BucketName))
            {
                glassButton1.Visible = false;
                MessageBox.Show("请先在config文件中设置好账户信息后再试！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tssl_status.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var oriText = tssl_netstat.Text;
            //
            if (Utils.IsConnected())
            {
                tssl_netstat.Text = "网络已连接";
                tssl_netstat.ForeColor = Color.Green;
            }
            else
            {
                tssl_netstat.Text = "网络已断开";
                tssl_netstat.ForeColor = Color.Red;
            }
            //
            var nowText = tssl_netstat.Text;
            if (string.Compare(oriText, nowText) != 0)
            {
                LogHelper.Write("NetworkStateChanged", "网络状态发生变化：{0} => {1} ", oriText, nowText);
            }
        }

        /// <summary>
        /// 执行当前项的下载。
        /// </summary>
        void exec_download_result(RecordItem rec)
        {
            if (rec == null)
                return;
            //未上传的启动上传
            if (rec.Statu == ItemStatu.Initial || rec.Statu == ItemStatu.UploadFailed)
            {
                //
                if (rec.Statu != ItemStatu.Initial)
                {
                    if (MessageBox.Show("是否需要重新进行上传转换？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;
                }
                //执行上传和转换操作
                PushItemToUploadQueue(rec);
                return;
            }
            //上传中的不处理
            //转换中的，检查结果
            if ((rec.Statu == ItemStatu.Converting || rec.Statu == ItemStatu.ConvertFalied
                || rec.Statu == ItemStatu.Converted || rec.Statu == ItemStatu.Inquired)
                && rec.Statu != ItemStatu.Querying)
            {
                //如果不在转换结果检查队列的，则加入队列并启动结果检查
                //转换结束的，需要启动结果获取处理  //生成word文档
                PushItemToGetResultQueue(rec);
                return;
            }
            //已翻译状态
            if (rec.Statu == ItemStatu.Inquired)
            {
                if (OfficeHelper.CreateDocument(rec))
                {
                    rec.Statu = ItemStatu.Finished;
                    ll_download.Visible = true;
                }
            }
            //已经有结果的，则提示是否打开word文件
            if (rec.Statu == ItemStatu.Finished)
            {
                if (MessageBox.Show("是否需要打开该录音文件识别结果Word文档？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                //
                if (!OfficeHelper.OpenDocument(rec))
                    MessageBox.Show("识别结果文档打开失败，请与系统管理员联系！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Question);
            }
        }

        private void ll_download_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            exec_download_result((RecordItem)curItem.Tag);
        }

        private void lvRecords_MouseClick(object sender, MouseEventArgs e)
        {
            ll_download.Visible = false;
            //
            curItem = lvRecords.GetItemAt(e.X, e.Y);
            //判断是否存在识别结果
            var rec = (RecordItem)curItem.Tag;
            if (rec == null)
                return;
            //已经完成状态，则可以显示下载按钮
            if (rec.Statu == ItemStatu.Finished)
                ll_download.Visible = true;
        }

        private void lvRecords_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            curItem = lvRecords.GetItemAt(e.X, e.Y);
            exec_download_result((RecordItem)curItem.Tag);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //关闭窗体之前，如果存在转换项，确认是否关闭
            if (uploadItems.Count > 0 || getResultItems.Count > 0)
            {
                if (MessageBox.Show("当然还有任务正在执行中，是否确认要退出？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            //停止当前线程标志
            _running = AliUtils.Running = false;
            //如果存在转换项，则需要缓存本地数据
            lock (syncRoot)
            {
                //
                while (uploadItems.Count > 0)
                {
                    var item = uploadItems.Dequeue();
                    RecordItemHelper.UpdateRecordCache(item);
                }
                //
                while (getResultItems.Count > 0)
                {
                    var item = getResultItems.Dequeue();
                    RecordItemHelper.UpdateRecordCache(item);
                }
            }
        }
    }
}
