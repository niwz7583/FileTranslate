using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FileTranslate.Properties;

namespace ICSU.Controls
{
    public partial class UIForm : Form
    {
        private static Image closeNormal = null;
        private static Image closeHover = null;
        private static Image closeClick = null;

        private static Image formBorderLeftImg = null;
        private static Image formBorderBottomImg = null;
        private static Image formBorderRightImg = null;

        private static Image formDegBottomLeftImg = null;
        private static Image formDegBottomRightImg = null;

        private static Image formTitleLeftImg = null;
        private static Image formTitleRightImg = null;
        private static Image formTitleMiddleImg = null;

        static int titleHeight = 32;
        internal static Color LinearBackColor1 = Color.FromArgb(35, 35, 62);
        internal static Color LinearBackColor2 = Color.FromArgb(24, 24, 24);

        static UIForm()
        {
            //边框
            formBorderLeftImg = Resources.formBorderLeftImg;
            formBorderRightImg = Resources.formBorderRightImg;
            formBorderBottomImg = Resources.formBorderBottomImg;
            //边角
            formDegBottomLeftImg = Resources.formDegBottomLeft;
            formDegBottomRightImg = Resources.formDegBottomRight;
            //标题
            formTitleLeftImg = Resources.formTitleLeftImg;
            formTitleMiddleImg = Resources.formTitleMiddleImg;
            formTitleRightImg = Resources.formTitleRightImg;
            //关闭按钮图片
            closeClick = Resources.closeClick;
            closeHover = Resources.closeHover;
            closeNormal = Resources.closeNormal;
        }

        public UIForm()
        {
            InitializeComponent();
            titleHeight = formTitleLeftImg.Height;
            this.MaximizedBounds = Screen.PrimaryScreen.WorkingArea;
            this.Padding = new Padding(formBorderLeftImg.Width, titleHeight, formBorderRightImg.Width, formBorderBottomImg.Height);
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                bool _need = string.Compare(value, base.Text) != 0;
                base.Text = value;
                if (_need) this.Invalidate();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            pbClose.Image = closeNormal;
            base.OnLoad(e);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            pbClose.Location = new Point(this.Width - pbClose.Width - 2, 4);
        }

        private void pbClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pbClose_MouseEnter(object sender, EventArgs e)
        {
            pbClose.Image = closeHover;
        }

        private void pbClose_MouseLeave(object sender, EventArgs e)
        {
            pbClose.Image = closeNormal;
        }

        private void pbClose_MouseDown(object sender, MouseEventArgs e)
        {
            pbClose.Image = closeClick;
        }

        bool _mouseIsDown;
        Point _lastLoc;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Y < titleHeight)
            {
                _mouseIsDown = true;
                _lastLoc = this.PointToScreen(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _mouseIsDown = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_mouseIsDown)
            {
                var pt = this.PointToScreen(e.Location);
                var moveSize = new Size(pt.X - _lastLoc.X, pt.Y - _lastLoc.Y);
                _lastLoc = pt;
                this.Location = Point.Add(this.Location, moveSize);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(this.Width, this.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        //使填充矩形的颜色从红色到黄色渐变                        
                        int _height = Math.Max(this.Height - formTitleLeftImg.Height, 1);
                        Rectangle _fillRect = new Rectangle(0, formTitleLeftImg.Height - 1, this.Width, _height);
                        LinearGradientBrush lBrush = new LinearGradientBrush(_fillRect, LinearBackColor1, LinearBackColor2, LinearGradientMode.Vertical | LinearGradientMode.Horizontal);
                        g.FillRectangle(lBrush, _fillRect);
                        //
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes();
                        ia.ClearColorKey();
                        //绘制角
                        //左边                
                        g.DrawImage(formDegBottomLeftImg, new Rectangle(0, this.Height - formDegBottomLeftImg.Height, formDegBottomLeftImg.Width, formDegBottomLeftImg.Height),
                            0, 0, formDegBottomLeftImg.Width, formDegBottomLeftImg.Height, GraphicsUnit.Pixel, ia);
                        //右边
                        g.DrawImage(formDegBottomRightImg, new Rectangle(this.Width - formDegBottomRightImg.Width, this.Height - formDegBottomRightImg.Height, formDegBottomRightImg.Width, formDegBottomRightImg.Height),
                            0, 0, formDegBottomRightImg.Width, formDegBottomRightImg.Height, GraphicsUnit.Pixel, ia);
                        //绘制边框
                        //底边
                        ia.SetWrapMode(WrapMode.TileFlipX);
                        g.DrawImage(formBorderBottomImg, new Rectangle(formDegBottomLeftImg.Width, this.Height - formBorderBottomImg.Height, this.Width - formDegBottomLeftImg.Width * 2, formBorderBottomImg.Height),
            0, 0, formBorderBottomImg.Width, formBorderBottomImg.Height, GraphicsUnit.Pixel, ia);
                        //左边
                        ia.SetWrapMode(WrapMode.TileFlipY);
                        g.DrawImage(formBorderLeftImg, new Rectangle(0, titleHeight, formBorderLeftImg.Width, this.Height - titleHeight - formDegBottomLeftImg.Height),
                            0, 0, formBorderLeftImg.Width, formBorderLeftImg.Height, GraphicsUnit.Pixel, ia);
                        //右边
                        g.DrawImage(formBorderRightImg, new Rectangle(this.Width - formBorderRightImg.Width, titleHeight, formBorderRightImg.Width, this.Height - titleHeight - formDegBottomLeftImg.Height),
           0, 0, formBorderRightImg.Width, formBorderRightImg.Height, GraphicsUnit.Pixel, ia);
                        //标题栏
                        ia.SetWrapMode(WrapMode.TileFlipX);
                        //left
                        g.DrawImage(formTitleLeftImg, new Rectangle(0, 0, formTitleLeftImg.Width, formTitleLeftImg.Height),
                            0, 0, formTitleLeftImg.Width, formTitleLeftImg.Height, GraphicsUnit.Pixel, ia);
                        //middle
                        g.DrawImage(formTitleMiddleImg, new Rectangle(formTitleLeftImg.Width, 0, this.Width - formTitleLeftImg.Width - formTitleRightImg.Width, formTitleMiddleImg.Height),
                            0, 0, formTitleMiddleImg.Width, formTitleMiddleImg.Height, GraphicsUnit.Pixel, ia);
                        //right
                        g.DrawImage(formTitleRightImg, new Rectangle(this.Width - formTitleRightImg.Width, 0, formTitleRightImg.Width, formTitleRightImg.Height),
                            0, 0, formTitleRightImg.Width, formTitleRightImg.Height, GraphicsUnit.Pixel, ia);

                        //绘制标题信息
                        SizeF s = g.MeasureString(Text, Font);
                        float x = 5;
                        float y = (formTitleLeftImg.Height - s.Height) / 2 + 2;
                        StringFormat sf = new StringFormat();
                        g.DrawString(Text, new Font(Font.FontFamily, 10), new SolidBrush(Color.FromArgb(230, 230, 230)), x, y, sf);
                        //画到窗口上
                        e.Graphics.DrawImageUnscaled(bmp, 0, 0, this.Width, this.Height);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public void WarningBox(string info, params object[] paras)
        {
            string _message = info;
            if (paras != null && paras.Length > 0)
                _message = string.Format(info, paras);
            //
            MessageBox.Show(_message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public DialogResult QuestionBox(string info, params object[] paras)
        {
            string _message = info;
            if (paras != null && paras.Length > 0)
                _message = string.Format(info, paras);
            return MessageBox.Show(_message, "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }        
    }
}
