using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.XWPF;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AheadTec;
using System.Diagnostics;
using NPOI.XWPF.UserModel;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI;
using static NPOI.POIXMLDocumentPart;
using NPOI.OpenXml4Net.OPC;

namespace FileTranslate
{
    /// <summary>
    /// 
    /// </summary>
    public class OfficeHelper
    {
        /*设置参数*/
        static Properties.Settings Settings = FileTranslate.Properties.Settings.Default;

        /// <summary>
        /// 创建文本格式文档。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static bool CreateTextDoc(RecordItem item, bool reCreate = false)
        {
            bool rv = false;
            //
            var file = RecordItemHelper.GetCachePath(item.Path, "txt");
            //
            if (File.Exists(file) && !reCreate)
                return true;
            //
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("录音名称：{0}\r\n", item.Text);
            builder.AppendFormat("录音大小：{0}\r\n", item.Length);
            builder.AppendLine("=======================================================================================");
            //
            try
            {
                var sents = RecordItemHelper.GetRecordItemSentences(item);
                //
                foreach (var s in sents)
                {
                    var n = s.ChannelId == 0 ? Settings.LeftName : Settings.RightName;
                    var tm1 = RecordItemHelper.FormatDuration(s.BeginTime);
                    var tm2 = RecordItemHelper.FormatDuration(s.EndTime);
                    builder.AppendFormat("{0,10}: [{1}-{2}] {3}\r\n", n, tm1, tm2, s.Text);
                }
                rv = true;
            }
            catch (Exception ex)
            {
                LogHelper.Write("CreateTextDoc", "录音[osskey={0}]解析识别结果时出错：{1}", item.OssKey, ex.Message);
                builder.AppendFormat("录音[osskey={0}]解析识别结果时出错：{1}\r\n", item.OssKey, ex.Message);
                builder.AppendLine("请与系统管理员或销售方负责人联系，谢谢！");
            }
            //写入文件
            try { File.WriteAllText(file, builder.ToString()); }
            catch { rv = false; }

            return rv;
        }

        static XWPFRun CreateWordRunFormat(XWPFDocument doc, string fmt, params object[] args)
        {
            var local_graph = doc.CreateParagraph();
            local_graph.Alignment = ParagraphAlignment.LEFT;
            local_graph.FirstLineIndent = 0;
            local_graph.SpacingBeforeLines = 1;
            local_graph.SpacingAfterLines = 1;

            XWPFRun r1 = local_graph.CreateRun();
            r1.FontSize = 12;
            //
            string text = fmt;
            //
            if (args != null && args.Length > 0)
                text = string.Format(fmt, args);
            //
            r1.SetText(text);
            //
            return r1;
        }
        /// <summary>
        /// 获取页眉页脚的ID号。
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>

        static string GetRelationshipID(POIXMLDocumentPart owner)
        {
            //
            var parent = owner.GetParent();
            if (parent != null)
            {
                foreach (RelationPart part in parent.RelationParts)
                {
                    if (part.DocumentPart == owner)
                        return part.Relationship.Id;
                }
            }
            else
            {
                OPCPackage package = owner.GetPackagePart().Package;
                string name = owner.GetPackagePart().PartName.Name;
                foreach (PackageRelationship relationship in package.Relationships)
                {
                    if (relationship.TargetUri.ToString().Equals(name))
                    {
                        return relationship.Id;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 创建Word格式文档。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="reCreate"></param>
        /// <returns></returns>
        static bool CreateWordDoc(RecordItem item, bool reCreate = false)
        {
            bool rv = false;
            //
            var file = RecordItemHelper.GetCachePath(item.Path, "docx");
            //
            if (File.Exists(file) && !reCreate)
                return true;
            //
            XWPFDocument doc = new XWPFDocument();
            //设置页边距
            doc.Document.body.sectPr = new CT_SectPr();
            CT_SectPr m_SectPr = doc.Document.body.sectPr;
            m_SectPr.pgSz.h = (ulong)16838;
            m_SectPr.pgSz.w = (ulong)11906;
            m_SectPr.pgMar.left = (ulong)1000;
            m_SectPr.pgMar.right = (ulong)1000;
            m_SectPr.pgMar.top = "1000";
            m_SectPr.pgMar.bottom = "1000";

            #region =页眉=
            //创建页眉
            CT_Hdr header = new CT_Hdr();
            CT_P m_P = header.AddNewP();
            m_P.AddNewR().AddNewT().Value = "录音识别结果文档";
            m_P.AddNewPPr().AddNewJc().val = ST_Jc.left;
            //创建页脚
            CT_Ftr footer = new CT_Ftr();
            CT_P m_fP = footer.AddNewP();
            m_fP.AddNewR().AddNewT().Value = "杭州领先科技有限公司";
            m_fP.AddNewPPr().AddNewJc().val = ST_Jc.center;
            //创建页眉关系
            XWPFRelation relation1 = XWPFRelation.HEADER;
            XWPFHeader myHeader = (XWPFHeader)doc.CreateRelationship(relation1, XWPFFactory.GetInstance(), doc.HeaderList.Count + 1);
            //创建页脚关系
            XWPFRelation relation2 = XWPFRelation.FOOTER;
            XWPFFooter myFooter = (XWPFFooter)doc.CreateRelationship(relation2, XWPFFactory.GetInstance(), doc.FooterList.Count + 1);
            //Set the header
            myHeader.SetHeaderFooter(header);
            CT_HdrFtrRef myHeaderRef = m_SectPr.AddNewHeaderReference();
            myHeaderRef.type = ST_HdrFtr.@default;
            myHeaderRef.id = GetRelationshipID(myHeader);
            //
            //var buffer = File.ReadAllBytes(@"C:\Users\niwz\Pictures\Logo\innerlogo.png");
            //myHeader.AddPictureData(buffer, (int)(int)PictureType.PNG);

            //Set the footer
            myFooter.SetHeaderFooter(footer);
            CT_HdrFtrRef myFooterRef = m_SectPr.AddNewFooterReference();
            myFooterRef.type = ST_HdrFtr.@default;
            myFooterRef.id = GetRelationshipID(myFooter);
            #endregion
            //首段落
            CreateWordRunFormat(doc, "录音名称：{0}", item.Text).IsBold = true;
            CreateWordRunFormat(doc, "录音大小：{0}", item.Length).IsBold = true;
            CreateWordRunFormat(doc, "=============================================================");
            //
            try
            {
                var sents = RecordItemHelper.GetRecordItemSentences(item);
                //
                foreach (var s in sents)
                {
                    var n = s.ChannelId == 0 ? Settings.LeftName : Settings.RightName;
                    var tm1 = RecordItemHelper.FormatDuration(s.BeginTime);
                    var tm2 = RecordItemHelper.FormatDuration(s.EndTime);
                    CreateWordRunFormat(doc, "{0,5}: [{1}-{2}] {3}\r\n", n, tm1, tm2, s.Text);
                }
                rv = true;
            }
            catch (Exception ex)
            {
                LogHelper.Write("CreateWordDoc", "录音[osskey={0}]解析识别结果时出错：{1}", item.OssKey, ex.Message);
                CreateWordRunFormat(doc, "录音[osskey={0}]解析识别结果时出错：{1}\r\n", item.OssKey, ex.Message);
                CreateWordRunFormat(doc, "请与系统管理员或销售方负责人联系，谢谢！");
            }

            try
            {
                using (FileStream stream = File.OpenWrite(file))
                    doc.Write(stream);

            }
            catch
            {
                rv = false;
            }


            return rv;
        }

        /// <summary>
        /// 创建Word文档。
        /// 先检查本地缓存目录是否存在，如果存在，则直接使用。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool CreateDocument(RecordItem item)
        {
            if (string.IsNullOrEmpty(item.Result))
                return false;

            //创建Word格式内容
            if (Settings.DocType)
                return CreateWordDoc(item);
            //创建文本格式内容
            return CreateTextDoc(item);
        }

        /// <summary>
        /// 打开结果文档。
        /// </summary>
        /// <param name="item"></param>
        public static bool OpenDocument(RecordItem item)
        {
            var ext = Settings.DocType ? "docx" : "txt";
            var file = RecordItemHelper.GetCachePath(item.Path, ext);
            //
            for (int idx = 0; idx < 5; idx++)
            {
                if (File.Exists(file))
                    break;
                //
                CreateDocument(item);
            }
            //
            if (!File.Exists(file))
                return false;
            //
            if (!Settings.DocType)
            {
                Process.Start("notepad.exe", file);
            }
            else
            {
                Process.Start(file);
            }
            //
            return true;
        }
    }
}
