# FileTranslate
录音文件转换成文本内容，基于阿里云的识别引擎。

录音文件识别转换工具使用说明
===========================================
1、本程序需要安装.net framework 4.6环境；
2、本程序使用时需要能够正常访问阿里云的相关网站；
3、使用前需要在config文件中设置以下参数：
AccessKeyId				阿里云访问账号，必需
AccessKeySecret		阿里云访问密码，必需
BucketName			阿里云OSS中的Bucket名称，需要事先创建，必需
Endpoint					阿里云服务器地址，一般为oss-cn-shanghai.aliyuncs.com
RegionId					阿里云区域编号，一般为cn-shanghai；
AppKeyId					阿里云开通录音文件识别功能的项目ID号，必需
OssKeepRecord		是否在阿里云OSS中保留录音文件，默认为False
DocType					是否生成Word文档，默认为False，生成为Txt文档；
LeftName					左声道  角色名称
RightName				右声道  角色名称

4、使用时选择的录音文件会根据状态显示不同颜色；
