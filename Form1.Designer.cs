namespace FileTranslate
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.glassButton1 = new Glass.GlassButton();
            this.lvRecords = new System.Windows.Forms.ListView();
            this.chStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tssl_netstat = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tssl_status = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.ll_download = new System.Windows.Forms.LinkLabel();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // glassButton1
            // 
            this.glassButton1.BackColor = System.Drawing.Color.Lavender;
            this.glassButton1.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.glassButton1.GlowColor = System.Drawing.Color.RoyalBlue;
            this.glassButton1.Image = ((System.Drawing.Image)(resources.GetObject("glassButton1.Image")));
            this.glassButton1.Location = new System.Drawing.Point(17, 32);
            this.glassButton1.Margin = new System.Windows.Forms.Padding(2);
            this.glassButton1.Name = "glassButton1";
            this.glassButton1.Size = new System.Drawing.Size(151, 51);
            this.glassButton1.TabIndex = 1;
            this.glassButton1.Text = "打开文件";
            this.glassButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.glassButton1.Click += new System.EventHandler(this.glassButton1_Click);
            // 
            // lvRecords
            // 
            this.lvRecords.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvRecords.BackColor = System.Drawing.SystemColors.Control;
            this.lvRecords.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chStatus,
            this.chPath,
            this.chSize});
            this.lvRecords.FullRowSelect = true;
            this.lvRecords.GridLines = true;
            this.lvRecords.HideSelection = false;
            this.lvRecords.Location = new System.Drawing.Point(9, 94);
            this.lvRecords.Name = "lvRecords";
            this.lvRecords.Size = new System.Drawing.Size(851, 509);
            this.lvRecords.TabIndex = 3;
            this.lvRecords.UseCompatibleStateImageBehavior = false;
            this.lvRecords.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lvRecords_MouseClick);
            this.lvRecords.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvRecords_MouseDoubleClick);
            // 
            // chStatus
            // 
            this.chStatus.DisplayIndex = 2;
            this.chStatus.Text = "当前状态";
            // 
            // chPath
            // 
            this.chPath.DisplayIndex = 0;
            this.chPath.Text = "文件名";
            // 
            // chSize
            // 
            this.chSize.DisplayIndex = 1;
            this.chSize.Text = "文件大小";
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tssl_netstat,
            this.toolStripStatusLabel3,
            this.tssl_status});
            this.statusStrip1.Location = new System.Drawing.Point(6, 603);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(856, 25);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tssl_netstat
            // 
            this.tssl_netstat.Name = "tssl_netstat";
            this.tssl_netstat.Size = new System.Drawing.Size(84, 20);
            this.tssl_netstat.Text = "网络已连接";
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.AutoSize = false;
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(640, 20);
            // 
            // tssl_status
            // 
            this.tssl_status.Name = "tssl_status";
            this.tssl_status.Size = new System.Drawing.Size(159, 20);
            this.tssl_status.Text = "2020-12-26 10:00:00";
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // ll_download
            // 
            this.ll_download.ActiveLinkColor = System.Drawing.Color.White;
            this.ll_download.AutoSize = true;
            this.ll_download.BackColor = System.Drawing.Color.Transparent;
            this.ll_download.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ll_download.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ll_download.LinkColor = System.Drawing.Color.White;
            this.ll_download.Location = new System.Drawing.Point(173, 66);
            this.ll_download.Name = "ll_download";
            this.ll_download.Size = new System.Drawing.Size(93, 20);
            this.ll_download.TabIndex = 5;
            this.ll_download.TabStop = true;
            this.ll_download.Text = "查看结果";
            this.ll_download.Visible = false;
            this.ll_download.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ll_download_LinkClicked);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(868, 634);
            this.Controls.Add(this.ll_download);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.lvRecords);
            this.Controls.Add(this.glassButton1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "领先科技有限公司 录音文件识别转换工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Controls.SetChildIndex(this.glassButton1, 0);
            this.Controls.SetChildIndex(this.lvRecords, 0);
            this.Controls.SetChildIndex(this.statusStrip1, 0);
            this.Controls.SetChildIndex(this.ll_download, 0);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Glass.GlassButton glassButton1;
        private System.Windows.Forms.ListView lvRecords;
        private System.Windows.Forms.ColumnHeader chPath;
        private System.Windows.Forms.ColumnHeader chSize;
        private System.Windows.Forms.ColumnHeader chStatus;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tssl_netstat;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.ToolStripStatusLabel tssl_status;
        private System.Windows.Forms.LinkLabel ll_download;
    }
}

