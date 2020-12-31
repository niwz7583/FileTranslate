using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;

namespace AheadTec
{
    /// <summary>
    /// 图片控件。
    /// </summary>
    public class UIImageView : PictureBox
    {
        #region Fields
        bool _isSelected;
        Image _normal, _hovered;
        bool _isHovered;
        string _text = string.Empty;
        #endregion

        #region Attributes
        /// <summary>
        /// 鼠标是否浮动在控件上面。
        /// </summary>
        public bool IsHovered
        {
            get { return _isHovered; }
            set
            {
                _isHovered = value;
                if (!_isHovered)
                {
                    this.Cursor = Cursors.Default;
                    if (!IsSelected)
                        this.Image = this.Normal;
                }
                else
                {
                    this.Cursor = Cursors.Hand;
                    if (this.Hovered == null)
                    {
                        this.Image = this.Normal;
                    }
                    else
                    {
                        this.Image = this.Hovered;
                    }
                }
            }
        }
        /// <summary>
        /// 正常情况下的图片。
        /// </summary>
        public Image Normal
        {
            get { return _normal; }
            set
            {
                if (value != _normal)
                    _normal = value;
                this.IsHovered = false;
            }
        }
        /// <summary>
        /// 鼠标浮动在上面时的图片。
        /// </summary>
        public Image Hovered
        {
            get { return _hovered; }
            set
            {
                if (value != _hovered)
                    _hovered = value;
            }
        }
        /// <summary>
        /// 透明的背景色。
        /// </summary>
        public override Color BackColor
        {
            get { return Color.Transparent; }
            set { }
        }
        public override string Text { get { return _text; } set { _text = value; } }
        public override Font Font { get; set; }
        public override Color ForeColor { get; set; }
        /// <summary>
        /// 选中状态。
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                this.Image = _isSelected ? this.Hovered : this.Normal;
            }
        }
        #endregion

        #region Contructor
        public UIImageView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.UpdateStyles();
            this.BackColor = Color.Transparent;
            this.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Font = new Font("黑体", 12f, FontStyle.Bold);
            this.ForeColor = Color.White;
        }
        #endregion

        #region Mouse Events
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.IsHovered = true;
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.IsHovered = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// 模拟点击事件。
        /// </summary>
        public void PerformClick()
        {
            base.OnClick(EventArgs.Empty);
        }
        public void CreateRegion()
        {
            if (this.Normal != null)
            {
                //调整图片
                if (this.Size != this.Normal.Size)
                    this.Normal = new Bitmap(this.Normal, this.Size);
                this.Region = Utils.RegionFromImage(this.Normal); //new Region(Utils.subGraphicsPath(this.Normal));
            }
        }
        #endregion

        #region OnPaint
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            if (!string.IsNullOrEmpty(this.Text))
            {
                SizeF _size = pe.Graphics.MeasureString(this.Text, this.Font);
                PointF _pos = new PointF((this.Width - _size.Width) / 2, (this.Height - _size.Height) / 2);
                pe.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), _pos);
            }
        }
        #endregion
    }

    /// <summary>
    /// 容器视图。
    /// </summary>
    public class UIView : Panel
    {
        public UIView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.UpdateStyles();
            this.BackColor = Color.Transparent;
        }

        public virtual void Load() { }
    }

    public class UIScrollImageViewIndexChangedEventArgs : EventArgs
    {
        public string Id { get; private set; }
        public int Index { get; private set; }
        public Image Image { get; private set; }

        public UIScrollImageViewIndexChangedEventArgs(string id, int index, Image img)
        {
            this.Id = id;
            this.Index = index;
            this.Image = img;
        }
    }
    /// <summary>
    /// 左右滚动的图片视图。
    /// </summary>
    public class UIScrollImageView : UIView
    {
        #region Fields
        //static Properties.Settings configHelper = Properties.Settings.Default;
        public bool IsLoop { get; set; }
        public bool IsDelayScroll { get; set; }
        public Dictionary<string, Image> Images { get; private set; }
        List<string> _imageIds = null;
        bool CanMove { get { return Images.Count > 1 && !_autoScrolling; } }
        bool _mouseIsDown = false;
        DateTime _downTime;
        POINT _lastPt, _downPt, _upPt;

        int _lastPosX = 0, _offsetX = 0;
        int _curIndex, _othIndex;
        Image _curImage, _othImage;
        RectangleF _dstRect0, _srcRect0, _dstRect1, _srcRect1;
        float _scaleX0, _scaleX1, _imgWidth;

        Timer _tmrAutoScroll;
        Timer _tmrDelayScroll;
        int _delayTimes = 3000;// configHelper.ScrollViewDelayTimes;
        int _lastDelayTimes = 0;
        //int _tickCount = 0;
        bool _autoScrolling = false;
        float _autoSpeed = 0f;
        bool _toRight = false;
        POINT _scrollPt;
        bool _needSwitch = false;
        public bool NeedSwitch
        {
            get { return _needSwitch; }
            set { _needSwitch = value; }
        }
        int _scrollTimes = 5;
        //标签
        List<UIImageView> _flags = new List<UIImageView>();
        #endregion

        #region Constructor
        public UIScrollImageView()
        {
            this.Images = new Dictionary<string, Image>();
            _tmrAutoScroll = new Timer();
            _tmrAutoScroll.Interval = 100;
            _tmrAutoScroll.Tick += new EventHandler(_tmrAutoScroll_Tick);
        }
        #endregion

        #region AutoScroll
        Point toClientPoint(POINT pt)
        {
            return this.PointToClient(new Point(pt.X, pt.Y));
        }
        void _tmrAutoScroll_Tick(object sender, EventArgs e)
        {
            bool _needStop = false;
            _scrollPt.X += (int)(_toRight ? _autoSpeed : -_autoSpeed);
            Point _clientPoint = toClientPoint(_scrollPt);
            if (_clientPoint.X <= 0)
            {
                _scrollPt.X -= _clientPoint.X;
                _needStop = true;
            }
            if (_clientPoint.X >= this.Width)
            {
                _scrollPt.X -= (_clientPoint.X - this.Width);
                _needStop = true;
            }
            if (_needStop)
            {
                _tmrAutoScroll.Stop();
                if (_needSwitch)
                {
                    _curIndex += _toRight ? -1 : 1;
                    if (IsLoop)
                    {
                        if (_curIndex < 0)
                            _curIndex = Images.Count - 1;
                        if (_curIndex >= Images.Count)
                            _curIndex = 0;
                    }
                    else
                    {
                        if (_curIndex < 0)
                            _curIndex = 0;
                        if (_curIndex >= Images.Count)
                            _curIndex = Images.Count - 1;
                    }
                }
                _lastPt = _downPt;
            }
            else
            {
                _lastPt = _scrollPt;
            }
            _offsetX = _lastPt.X - _downPt.X;
            this.Invalidate();
        }
        #endregion

        #region Initialize
        /// <summary>
        /// 初始化集合图片。
        /// </summary>
        public void Initialize()
        {
            if (Images.Count < 1)
                return;
            _imageIds = new List<string>(Images.Keys);
            _curIndex = 0;
            this.Invalidate();
            //启动定时检测
            if (IsDelayScroll && Images.Count > 1)
            {
                _tmrDelayScroll = new Timer();
                _tmrDelayScroll.Tick += new EventHandler(_tmrDelayScroll_Tick);
                _tmrDelayScroll.Interval = 50;// configHelper.ScrollViewInterval;
                _tmrDelayScroll.Start();

                this.Click += (s, e) =>
                {
                    if (_autoScrolling)
                    {
                        _autoScrolling = false;
                        _curIndex += 1;
                        if (_curIndex < 0)
                            _curIndex = Images.Count - 1;
                        if (_curIndex >= Images.Count)
                            _curIndex = 0;
                        this.Invalidate();
                    }
                };
            }
        }

        void _tmrDelayScroll_Tick(object sender, EventArgs e)
        {
            if (!_autoScrolling)
            {
                _lastDelayTimes += 50;// configHelper.ScrollViewInterval;
                if (_lastDelayTimes >= _delayTimes)
                {
                    _lastDelayTimes = 0;
                    //启动自动切换
                    _autoScrolling = true;
                    //偏移量
                    _offsetX = 0;
                    //速度
                    _autoSpeed = (this.Width / 120/*configHelper.ScrollViewOnceTimes*/);
                }
            }
            else
            {
                //滚动内容
                _offsetX -= (int)_autoSpeed;
                _othIndex = _curIndex + 1;
                if (_offsetX <= -this.Width)
                {
                    _autoScrolling = false;
                    _curIndex += 1;
                    if (_curIndex < 0)
                        _curIndex = Images.Count - 1;
                    if (_curIndex >= Images.Count)
                        _curIndex = 0;
                }
                this.Invalidate();
            }
        }
        #endregion

        #region Mouse Event
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (CanMove) this.Cursor = Cursors.Hand;
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.Cursor = Cursors.Default;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _lastDelayTimes = 0;
            if (!CanMove)
                return;
            _mouseIsDown = true;
            _downTime = DateTime.Now;
            Utils.GetCursorPos(out _lastPt);
            _downPt = _lastPt;
            _lastPosX = _lastPt.X;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _mouseIsDown = false;
            _lastDelayTimes = 0;
            if (!CanMove)
                return;
            Utils.GetCursorPos(out _upPt);
            //存在移动距离
            //居中判断：小于一半，滚动到左边距，否则，滚动到右边距
            //切换当前索引 _curIndex
            int _absDistance = Math.Abs(_upPt.X - _downPt.X);
            if (_absDistance > 1)
            {
                _scrollPt = _upPt;
                bool _upAtLeft = toClientPoint(_upPt).X < this.Width / 2;
                bool _downAtLeft = toClientPoint(_downPt).X < this.Width / 2;
                _needSwitch = _downAtLeft != _upAtLeft;
                Point _clientPoint = toClientPoint(_lastPt);
                TimeSpan ts = DateTime.Now - _downTime;
                if (ts.TotalMilliseconds < 200)
                {
                    _toRight = (_downPt.X - _upPt.X) < 0;
                    _needSwitch = true;
                    _clientPoint = toClientPoint(_upPt);
                    _autoSpeed = (float)_clientPoint.X / _scrollTimes;
                }
                else
                {
                    if (_upAtLeft && _downAtLeft)
                        _toRight = false;
                    else if (!_upAtLeft && !_downAtLeft)
                        _toRight = true;
                    else
                        _toRight = _clientPoint.X > this.Width / 2;

                    _autoSpeed = (float)_clientPoint.X / 10f; //(float)(_toRight ? _clientPoint.X : this.Width - _clientPoint.X) / 10f;
                }
                _flags.ForEach(v => this.Controls.Remove(v));
                _tmrAutoScroll.Start();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_mouseIsDown)
            {
                _lastDelayTimes = 0;
                Utils.GetCursorPos(out _lastPt);
                _offsetX = _lastPt.X - _downPt.X;
                _othIndex = _curIndex + (_offsetX < 0 ? 1 : -1);
                this.Invalidate();
            }
        }
        #endregion

        #region Paint
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Images.Count < 1)
                base.OnPaintBackground(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Images.Count < 1)
            {
                base.OnPaint(e);
                return;
            }
            _curImage = Images[_imageIds[_curIndex]];
            if (this.IsLoop)
            {
                if (_othIndex < 0)
                    _othIndex = Images.Count - 1;
                if (_othIndex >= Images.Count)
                    _othIndex = 0;
            }
            if (_othIndex >= 0 && _othIndex < Images.Count)
                _othImage = Images[_imageIds[_othIndex]];
            else
                _othImage = null;

            _scaleX0 = (float)this.Width / (float)_curImage.Width;
            if (_othImage != null)
                _scaleX1 = (float)this.Width / (float)_othImage.Width;
            _imgWidth = 0f;
            if (_offsetX <= 0)
            {
                _dstRect0 = new RectangleF(0f, 0f, this.Width + _offsetX, this.Height);
                _imgWidth = Math.Abs(_offsetX) / _scaleX0;
                _srcRect0 = new RectangleF(_imgWidth, 0f, _curImage.Width - _imgWidth, _curImage.Height);
                //
                if (_othImage != null)
                {
                    _dstRect1 = new RectangleF(this.Width + _offsetX, 0f, Math.Abs(_offsetX), this.Height);
                    _imgWidth = Math.Abs(_offsetX) / _scaleX1;
                    _srcRect1 = new RectangleF(0, 0f, _imgWidth, _othImage.Height);
                }
            }
            else
            {
                _dstRect0 = new RectangleF(_offsetX, 0f, this.Width - _offsetX, this.Height);
                _imgWidth = Math.Abs(_offsetX) / _scaleX0;
                _srcRect0 = new RectangleF(0f, 0f, _curImage.Width - _imgWidth, _curImage.Height);
                //
                if (_othImage != null)
                {
                    _dstRect1 = new RectangleF(0f, 0f, Math.Abs(_offsetX), this.Height);
                    _imgWidth = Math.Abs(_offsetX) / _scaleX1;
                    _srcRect1 = new RectangleF(_othImage.Width - _imgWidth, 0f, _imgWidth, _othImage.Height);
                }
            }
            //如果图片为空，则不移动当前图片
            if (_othImage == null)
            {
                _dstRect0 = this.DisplayRectangle;
                _srcRect0 = new RectangleF(0f, 0f, _curImage.Width, _curImage.Height);
            }
            BufferedGraphicsContext currentContext = BufferedGraphicsManager.Current;
            BufferedGraphics buffer = currentContext.Allocate(e.Graphics, this.DisplayRectangle);
            buffer.Graphics.DrawImage(_curImage, _dstRect0, _srcRect0, GraphicsUnit.Pixel);
            if (_othImage != null)
                buffer.Graphics.DrawImage(_othImage, _dstRect1, _srcRect1, GraphicsUnit.Pixel);
            //输出第几页
            if (Images.Count > 1)
            {
                if (Images.Count > 15)
                {
                    string _text = string.Format("{0}/{1}", _curIndex + 1, Images.Count);
                    Font _fnt = new Font("黑体", 12f, FontStyle.Bold);
                    Size _size = e.Graphics.MeasureString(_text, _fnt).ToSize();
                    PointF _loc = new PointF(this.Width - _size.Width, this.Height - _size.Height - 10);
                    buffer.Graphics.DrawString(_text, _fnt, new SolidBrush(Color.Red), _loc);
                }
                else
                {
                    int _padding = 8;
                    SizeF _size = new Size(10, 10);
                    PointF _loc = new PointF((this.Width - Images.Count * (_size.Width + _padding)) / 2, this.Height - _size.Height - 10);
                    for (int i = 0; i < Images.Count; i++)
                    {
                        _loc.X += _size.Width + _padding;
                        buffer.Graphics.FillEllipse(new SolidBrush(i == _curIndex ? Color.Chocolate : SystemColors.ControlLight), new RectangleF(_loc, _size));
                    }
                }
            }
            //
            buffer.Render(e.Graphics);
            buffer.Dispose();
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_tmrDelayScroll != null)
                {
                    _tmrDelayScroll.Stop();
                    _tmrDelayScroll.Dispose();
                    _tmrDelayScroll = null;
                }

                if (_tmrAutoScroll != null)
                {
                    _tmrAutoScroll.Stop();
                    _tmrAutoScroll.Dispose();
                    _tmrAutoScroll = null;
                }
                foreach (var kvp in Images)
                    kvp.Value.Dispose();

                Images.Clear();
                //
                _flags.ForEach(v => this.Controls.Remove(v));
            }
            base.Dispose(disposing);
        }
        #endregion
    }
    /// <summary>
    /// 跑马灯文字。
    /// </summary>

    public class UIRaceLampView : UIView
    {
        Font m_font = new Font("黑体", 12f, FontStyle.Bold);
        /// <summary>
        /// 字体。
        /// </summary>
        public override Font Font { get { return m_font; } set { m_font = value; } }

        int m_interval = 50;
        /// <summary>
        /// 移动速度。
        /// </summary>
        public int Interval
        {
            get { return m_interval; }
            set
            {
                if (m_interval != value)
                {
                    m_interval = value;
                    if (tmr_autoScroll != null)
                        tmr_autoScroll.Interval = m_interval;
                }
            }
        }

        Color m_foreColor = Color.Red;
        /// <summary>
        /// 字体颜色。
        /// </summary>
        public override Color ForeColor { get { return m_foreColor; } set { m_foreColor = value; } }
        /// <summary>
        /// 文本字体预计的大小尺寸。
        /// </summary>
        Size m_textSize = Size.Empty;
        int m_scrollTop = 5;
        string m_text = string.Empty;
        /// <summary>
        /// 文本内容。
        /// </summary>
        [Browsable(true)]
        public override string Text
        {
            get { return m_text; }
            set
            {
                if (string.Compare(m_text, value) != 0)
                {
                    m_text = value;
                    if (string.IsNullOrEmpty(m_text))
                        return;
                    //计算大小
                    BufferedGraphicsContext currentContext = BufferedGraphicsManager.Current;
                    BufferedGraphics buffer = currentContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
                    m_textSize = buffer.Graphics.MeasureString(this.Text, m_font).ToSize();
                    m_scrollTop = Math.Max(2, (this.Height - m_textSize.Height) / 2);
                    //_lastPosX = (this.Width - m_textSize.Width) / 2;
                    buffer.Dispose();
                    this.Invalidate();
                }
            }
        }
        Timer tmr_autoScroll = null;
        bool m_scrollEnabled = false;
        int _lastPosX = 0;

        /// <summary>
        /// 是否正在滚动中。
        /// </summary>
        public bool ScrollEnabled
        {
            get { return m_scrollEnabled; }
            set
            {
                if (m_scrollEnabled != value)
                {
                    m_scrollEnabled = value;
                    if (m_scrollEnabled)
                    {
                        if (tmr_autoScroll == null)
                        {
                            tmr_autoScroll = new Timer();
                            tmr_autoScroll.Interval = this.Interval;
                            tmr_autoScroll.Tick += tmr_autoScroll_Tick;
                        }
                        tmr_autoScroll.Start();
                    }
                    else
                    {
                        if (tmr_autoScroll != null)  //停止
                        {
                            tmr_autoScroll.Stop();
                            tmr_autoScroll = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 定时器事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmr_autoScroll_Tick(object sender, EventArgs e)
        {
            _lastPosX -= 1;
            if ((_lastPosX + m_textSize.Width) < 0)
                _lastPosX = this.Width - 1;
            //
            this.Invalidate();
        }
        /// <summary>
        /// 画背景。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(this.Text))
                base.OnPaintBackground(e);
        }

        /// <summary>
        /// 重画文字。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(this.Text))
            {
                base.OnPaint(e);
                return;
            }
            BufferedGraphicsContext currentContext = BufferedGraphicsManager.Current;
            BufferedGraphics buffer = currentContext.Allocate(e.Graphics, this.DisplayRectangle);
            PointF _loc = new PointF(_lastPosX, m_scrollTop);
            buffer.Graphics.Clear(this.BackColor);
            buffer.Graphics.DrawString(this.Text, m_font, new SolidBrush(Color.Red), _loc);
            buffer.Render(e.Graphics);
            buffer.Dispose();
        }
    }
}