using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;

namespace AheadTec
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
    public class Utils
    {
        [DllImport("user32.dll")]
        internal static extern bool GetLastInputInfo(ref LastInputInfo plii);
        /// <summary>
        /// 获取鼠标当前的屏幕位置。
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr ExtCreateRegion(IntPtr lpXform, uint nCount, IntPtr rgnData);
        /// <summary>
        /// 获取唯一ID号。
        /// </summary>
        /// <returns></returns>
        public static string GetNewId()
        {
            return Guid.NewGuid().ToString("N").ToUpper();
        }

        /// <summary>
        /// 获取MD5串。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMD5String(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
                // To force the hex string to lower-case letters instead of
                // upper-case, use he following line instead:
                // sb.Append(hashBytes[i].ToString("x2")); 
            }
            return sb.ToString();
        }
        static string _temporay;
        /// <summary>
        /// 临时目录。
        /// </summary>
        public static string TempDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_temporay))
                {
                    _temporay = Path.GetFullPath("Temp");
                    if (!Directory.Exists(_temporay))
                        Directory.CreateDirectory(_temporay);
                }
                return _temporay;
            }
        }

        static string _cacheDir = string.Empty;
        /// <summary>
        /// 缓存目录
        /// </summary>
        public static string CacheDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_cacheDir))
                {
                    _cacheDir = Path.GetFullPath("Cached");
                    if (!Directory.Exists(_cacheDir))
                        Directory.CreateDirectory(_cacheDir);
                    //设置属性为隐藏
                    FileAttributes attributes = File.GetAttributes(_cacheDir);
                    File.SetAttributes(_cacheDir, attributes | FileAttributes.Hidden);
                }
                return _cacheDir;
            }
        }

        /// <summary>
        /// 整形数据转成Color。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Color colorWithString(string value)
        {
            int _color = 0;
            //如果是数值
            if (int.TryParse(value, out _color))
            {
                byte a = (byte)((_color >> 24) & 0x000000ff);
                byte b = (byte)((_color >> 16) & 0x000000ff);
                byte g = (byte)((_color >> 8) & 0x000000ff);
                byte r = (byte)(_color & 0x000000ff);

                return Color.FromArgb(a, r, g, b);
            }
            else
            {
                return ColorTranslator.FromHtml(value);
            }
        }

        // 定义常量
        private const long INTERNET_CONNECTION_MODEM = 1;//Local system uses a modem to connect to the Internet.
        private const long INTERNET_CONNECTION_LAN = 2; //Local system uses a local area network to connect to the Internet.
        private const long INTERNET_CONNECTION_PROXY = 4;//Local system uses a proxy server to connect to the Internet.
        private const long INTERNET_CONNECTION_MODEM_BUSY = 8;   //No longer used.
        private const long INTERNET_CONNECTION_CONFIGURED = 64; //Local system has a valid connection to the Internet, but it might or might not be currently connected.
        private const long INTERNET_CONNECTION_OFFLINE = 32; // Local system is in offline mode.
        private const long INTERNET_RAS_INSTALLED = 16; //Local system has RAS installed.
        [DllImport("wininet.dll")]
        public static extern bool InternetGetConnectedState(out long lpdwFlags, long dwReserved);

        /// <summary>
        /// 检测网络是否可用。
        /// </summary>
        /// <returns></returns>
        public static bool IsConnected()
        {
            bool _ret = false;
            long lfag;
            string strConnectionDev = "";
            if (InternetGetConnectedState(out lfag, 0))
            {
                _ret = true;
                strConnectionDev = "网络连接正常!";
            }
            else
            {
                strConnectionDev = "网络连接不可用!";
            }
            if ((lfag & INTERNET_CONNECTION_OFFLINE) > 0)
                strConnectionDev += "OFFLINE 本地系统处于离线模式。";
            if ((lfag & INTERNET_CONNECTION_MODEM) > 0)
                strConnectionDev += "Modem 本地系统使用调制解调器连接到互联网。";
            if ((lfag & INTERNET_CONNECTION_LAN) > 0)
                strConnectionDev += "LAN 本地系统使用的局域网连接到互联网。";
            if ((lfag & INTERNET_CONNECTION_PROXY) > 0)
                strConnectionDev += "a   Proxy";
            if ((lfag & INTERNET_CONNECTION_MODEM_BUSY) > 0)
                strConnectionDev += "Modem   but   modem   is   busy";

            return _ret;
        }

        internal unsafe static Region RegionFromImage(Image img)
        {
            if (img == null) return null;
            GraphicsPath g = new GraphicsPath(FillMode.Alternate);
            Bitmap bitmap = new Bitmap(img);
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte* p = (byte*)bmData.Scan0;
            int offset = bmData.Stride - width * 3;
            int p0, p1, p2; // 记录左上角0，0座标的颜色值 
            p0 = p[0];
            p1 = p[1];
            p2 = p[2];
            int start = -1;
            // 行座标 ( Y col ) 
            for (int Y = 0; Y < height; Y++)
            {
                // 列座标 ( X row ) 
                for (int X = 0; X < width; X++)
                {
                    if (start == -1 && (p[0] != p0 || p[1] != p1 || p[2] != p2)) //如果 之前的点没有不透明 且 不透明 
                    {
                        start = X; //记录这个点 
                    }
                    else if (start > -1 && (p[0] == p0 && p[1] == p1 && p[2] == p2)) //如果 之前的点是不透明 且 透明 
                    {
                        g.AddRectangle(new Rectangle(start, Y, X - start, 1)); //添加之前的矩形到 
                        start = -1;
                    }
                    if (X == width - 1 && start > -1) //如果 之前的点是不透明 且 是最后一个点 
                    {
                        g.AddRectangle(new Rectangle(start, Y, X - start + 1, 1)); //添加之前的矩形到 
                        start = -1;
                    }
                    //if (p[0] != p0 || p[1] != p1 || p[2] != p2) 
                    // g.AddRectangle(new Rectangle(X, Y, 1, 1)); 
                    p += 3; //下一个内存地址 
                }
                p += offset;
            }
            bitmap.UnlockBits(bmData);
            bitmap.Dispose();

            Region rgn = new Region(g);
            g.Dispose();
            return rgn;
        }

        internal static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(System.Drawing.Rectangle rect, int cornerRadius)
        {
            System.Drawing.Drawing2D.GraphicsPath roundedRect = new System.Drawing.Drawing2D.GraphicsPath();
            roundedRect.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
            roundedRect.AddLine(rect.X + cornerRadius, rect.Y, rect.Right - cornerRadius * 2, rect.Y);
            roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
            roundedRect.AddLine(rect.Right, rect.Y + cornerRadius * 2, rect.Right, rect.Y + rect.Height - cornerRadius * 2);
            roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
            roundedRect.AddLine(rect.Right - cornerRadius * 2, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
            roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            roundedRect.AddLine(rect.X, rect.Bottom - cornerRadius * 2, rect.X, rect.Y + cornerRadius * 2);
            roundedRect.CloseFigure();
            return roundedRect;
        }

        /// <summary>
        /// 缓存的拨号历史。
        /// </summary>
        static List<string> m_cachedCallers = null;
        static string filename = "localcached.db";
        /// <summary>
        /// 缓存的数据源。
        /// </summary>
        public static List<string> CachedSource
        {
            get
            {
                if (m_cachedCallers == null)
                {
                    m_cachedCallers = new List<string>();
                    //加载本地缓存的拨号历史记录  //add by niwz 2018.03.18
                    if (File.Exists(filename))
                    {
                        var lines = File.ReadAllLines(filename);
                        m_cachedCallers.AddRange(lines);
                    }
                }
                return m_cachedCallers;
            }
        }

        /// <summary>
        /// 更新缓存数据内容。
        /// </summary>
        /// <param name="dest"></param>
        public static void UpdateCachedSource(string dest)
        {
            try
            {
                //添加当前的拨号
                CachedSource.Remove(dest);
                CachedSource.Add(dest);
                //移除空行
                CachedSource.RemoveAll(s => string.IsNullOrEmpty(s));
                //控制数量
                if (CachedSource.Count > 20)
                    CachedSource.RemoveRange(0, CachedSource.Count - 20);
                //缓存数据
                File.WriteAllLines(filename, CachedSource.ToArray());
            }
            catch
            {
            }
        }

        /// <summary>
        /// 设置文本行的自动完成数据源。
        /// </summary>
        /// <param name="tb"></param>
        public static void BindCompleteSource(TextBox tb)
        {
            var source = new AutoCompleteStringCollection();
            source.AddRange(CachedSource.ToArray());
            //
            tb.AutoCompleteCustomSource = source;
            tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        public static string GetEnumDescription(object enumValue)
        {
            Type enumType = enumValue.GetType();
            if (enumType == null || !enumType.IsEnum)
                return enumValue.ToString();
            //
            FieldInfo[] fieldinfos = enumType.GetFields();
            foreach (FieldInfo field in fieldinfos)
            {
                if (!field.FieldType.IsEnum)
                    continue;
                if ((int)field.GetValue(field.Name) != (int)enumValue)
                    continue;
                //
                DescriptionAttribute[] arrDesc = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (arrDesc != null && arrDesc.Length > 0)
                    return arrDesc[0].Description;
            }
            //
            return enumValue.ToString();
        }
    }

    /// <summary>
    /// 创建结构体用于返回捕获时间
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        /// <summary>
        /// 设置结构体块容量
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public int cbSize;
        /// <summary>
        /// 抓获的时间
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public uint dwTime;
    }
}
