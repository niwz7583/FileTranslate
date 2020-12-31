using AheadTec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aliyun.OSS.Common;
using Aliyun.OSS;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Exceptions;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Core.Http;
using FileTranslate.Properties;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace FileTranslate
{
    /*
     * 基于阿里云的基本操作。
     */
    public class AliUtils
    {
        /*是否运行状态*/
        public static bool Running = true;
        /*设置参数*/
        static Properties.Settings Settings = FileTranslate.Properties.Settings.Default;
        /*Oss客户端*/
        static OssClient client = null;
        /*极速模式*/
        static bool UseFlashRec = Settings.UseFlashRecognizer;

        const string PRODUCT = "nls-filetrans";
        const string DOMAIN = "filetrans.cn-shanghai.aliyuncs.com";
        const string API_VERSION = "2018-08-17";
        const string POST_REQUEST_ACTION = "SubmitTask";
        const string GET_REQUEST_ACTION = "GetTaskResult";
        // 请求参数
        const string KEY_APP_KEY = "appkey";
        const string KEY_FILE_LINK = "file_link";
        const string KEY_VERSION = "version";
        const string KEY_ENABLE_WORDS = "enable_words";
        // 响应参数
        const string KEY_TASK = "Task";
        const string KEY_TASK_ID = "TaskId";
        const string KEY_STATUS_TEXT = "StatusText";
        // 状态值
        const string STATUS_SUCCESS = "SUCCESS";
        const string STATUS_RUNNING = "RUNNING";
        const string STATUS_QUEUEING = "QUEUEING";


        static AliUtils()
        {
            if (!string.IsNullOrEmpty(Settings.AccessKeyId))
                client = new OssClient(Settings.Endpoint, Settings.AccessKeyId, Settings.AccessKeySecret);
        }

        /// <summary>
        /// 从OSS中删除一个录音内容。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool DeleteRecordItemFromOSS(RecordItem item)
        {
            try
            {
                var delResult = client.DeleteObject(Settings.BucketName, item.OssKey);
                LogHelper.Write("AppendRecordItemToOSS", "成功从OSS删除文件内容，返回结果：{0}", delResult.RequestId);
            }
            catch (OssException ex)
            {
                LogHelper.Write("OssException", "从OSS删除文件失败，error code: {0}; Error info: {1}. \nRequestID:{2}\tHostID:{3}",
                    ex.ErrorCode, ex.Message, ex.RequestId, ex.HostId);
                //
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Write("OssException", "从OSS删除文件失败， error info: {0}", ex.Message);
                //
                return false;
            }

            //
            return true;
        }

        /// <summary>
        /// 添加录音文件到OSS服务器。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool AppendRecordItemToOSS(RecordItem item)
        {
            //调整状态
            item.Statu = ItemStatu.Uploading;
            //
            try
            {
                using (var fs = File.Open(item.Path, FileMode.Open))
                {
                    var result = client.PutObject(Settings.BucketName, item.OssKey, fs);
                    LogHelper.Write("AppendRecordItemToOSS", "成功上传到OSS，返回结果：{0}", result.RequestId);
                }
            }
            catch (OssException ex)
            {
                LogHelper.Write("OssException", "上传到OSS失败，error code: {0}; Error info: {1}. \nRequestID:{2}\tHostID:{3}",
                    ex.ErrorCode, ex.Message, ex.RequestId, ex.HostId);
                //
                item.Statu = ItemStatu.UploadFailed;
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Write("OssException", "Failed with error info: {0}", ex.Message);
                item.Statu = ItemStatu.UploadFailed;
                return false;
            }
            return true;
        }

        static DefaultAcsClient GetAcsClient()
        {
            IClientProfile profile = DefaultProfile.GetProfile(Settings.RegionId, Settings.AccessKeyId, Settings.AccessKeySecret);
            return new DefaultAcsClient(profile);
        }

        static CommonRequest GetCommonRequest()
        {
            CommonRequest request = new CommonRequest();
            request.Domain = DOMAIN;
            request.Version = API_VERSION;
            request.Action = POST_REQUEST_ACTION;
            request.Product = PRODUCT;
            request.Method = MethodType.POST;

            return request;
        }

        /// <summary>
        /// 普通版录音转文字操作。
        /// </summary>
        /// <returns></returns>
        static string NormalRecognizer(string osskey)
        {
            string fileurl = string.Format("https://{0}.{1}/{2}", Settings.BucketName, Settings.Endpoint, osskey);
            //
            var client = GetAcsClient();
            var request = GetCommonRequest();
            //
            //设置task，以JSON字符串形式设置到请求Body中。
            JObject obj = new JObject();
            obj[KEY_APP_KEY] = Settings.AppKeyId;
            obj[KEY_FILE_LINK] = fileurl;
            obj[KEY_VERSION] = "4.0";
            obj[KEY_ENABLE_WORDS] = false;
            string task = obj.ToString();
            //
            request.AddBodyParameters(KEY_TASK, task);
            /**
             * 提交录音文件识别请求，处理服务端返回的响应。
             */
            CommonResponse response = client.GetCommonResponse(request);
            //System.Console.WriteLine(response.Data);
            if (response.HttpStatus != 200)
            {
                LogHelper.Write("NormalRecognizer", "录音文件识别请求失败： " + response.HttpStatus);
                return string.Empty;
            }
            // 获取录音文件识别请求任务ID，以供识别结果查询使用。
            string taskId = "";
            JObject jsonObj = JObject.Parse(response.Data);
            string statusText = jsonObj[KEY_STATUS_TEXT].ToString();
            if (statusText.Equals(STATUS_SUCCESS))
            {
                taskId = jsonObj[KEY_TASK_ID].ToString();
                LogHelper.Write("NormalRecognizer", "录音文件识别请求成功响应[osskey={0} taskid={1}]！", osskey, taskId);
                return taskId;
            }
            else
            {
                LogHelper.Write("NormalRecognizer", "录音文件识别请求失败[osskey={0}, response={1}]！", osskey, response.Data);
            }
            //
            return string.Empty;
        }

        /// <summary>
        /// 极速版录音转文字操作。
        /// </summary>
        /// <returns></returns>
        static string FlashRecognizer(string osskey)
        {
            string fileurl = string.Format("https://{0}.{1}/{2}", Settings.BucketName, Settings.Endpoint, osskey);

            return string.Empty;
        }

        /// <summary>
        /// 提交到进行识别转换。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool CommitToRecognizer(RecordItem item)
        {
            item.TaskId = "";
            item.Statu = ItemStatu.Converting;
            for (int idx = 0; idx < 5; idx++)
            {
                item.TaskId = UseFlashRec ? FlashRecognizer(item.OssKey) : NormalRecognizer(item.OssKey);
                if (!string.IsNullOrEmpty(item.TaskId))
                {
                    item.TaskStamp = DateTime.Now;
                    break;
                }
                //
                Thread.Sleep(1000);
            }
            //
            if (!string.IsNullOrEmpty(item.TaskId))
                item.Statu = ItemStatu.Converted;
            else
                item.Statu = ItemStatu.ConvertFalied;
            //
            return item.Statu == ItemStatu.Converted;
        }


        /// <summary>
        /// 查询获取识别结果。
        /// </summary>
        public static bool GetRecognizerResult(RecordItem item)
        {
            if (string.IsNullOrEmpty(item.TaskId))
                return false;
            //
            item.Result = "";
            item.Statu = ItemStatu.Querying;
            //
            var client = GetAcsClient();
            var request = GetCommonRequest();
            request.Action = GET_REQUEST_ACTION;
            request.Method = MethodType.GET;
            request.AddQueryParameters(KEY_TASK_ID, item.TaskId);
            //
            try
            {
                /**
                * 提交录音文件识别结果查询请求
                * 以轮询的方式进行识别结果的查询，直到服务端返回的状态描述为“SUCCESS”、“SUCCESS_WITH_NO_VALID_FRAGMENT”，
                * 或者为错误描述，则结束轮询。
                */
                var statusText = "";
                CommonResponse getResponse = null;
                //
                while (Running)
                {
                    getResponse = client.GetCommonResponse(request);
                    //System.Console.WriteLine(getResponse.Data);
                    if (getResponse.HttpStatus != 200)
                    {
                        LogHelper.Write("GetRecognizerResult", "识别结果查询请求失败，Http错误码：" + getResponse.HttpStatus);
                        break;
                    }
                    JObject jsonObj2 = JObject.Parse(getResponse.Data);
                    statusText = jsonObj2[KEY_STATUS_TEXT].ToString();
                    if (statusText.Equals(STATUS_RUNNING) || statusText.Equals(STATUS_QUEUEING))
                    {
                        // 继续轮询
                        Thread.Sleep(10 * 1000);
                    }
                    else
                    {
                        // 退出轮询
                        break;
                    }
                }
                if (statusText.Equals(STATUS_SUCCESS))
                {
                    LogHelper.Write("GetRecognizerResult", "录音文件识别结果获取成功[osskey={0}]！", item.OssKey);
                    item.Statu = ItemStatu.Inquired;
                    item.Result = getResponse.Data;
                }
                else
                {
                    LogHelper.Write("GetRecognizerResult", "录音文件识别失败[osskey={0}]！", item.OssKey);
                    item.Statu = ItemStatu.ConvertFalied;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write("GetRecognizerResult", "获取录音[osskey={0},taskid={1}]识别结果出错:{1}", item.OssKey, item.TaskId, ex.Message);
                item.Statu = ItemStatu.ConvertFalied;
            }
            //
            return !string.IsNullOrEmpty(item.Result);
        }
    }
}
