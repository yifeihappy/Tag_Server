using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web;

namespace ModuleReaderManager.HttpServer
{
    class UHFHttpServer:HttpServer
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public UHFHttpServer(string ipAddress, int port)
            : base(ipAddress, port)
        {

        }
        //用于判断当前摄像头的状态
        string CurState = "";
        StreamWriter sw_interaction;
        Stream interactionFeature;
        //文件名
        private string realName;
        public override void OnPost(HttpRequest request, HttpResponse response)
        {
            //获取客户端传递的参数
            string data = request.Params["type"];
            //构造响应报文
            response.Content_Encoding = "utf-8";
            response.StatusCode = "200";
            response.Headers = new Dictionary<string, string>();
            response.SetHeader(ResponseHeaders.Allow, "*");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Credentials", "true");
            response.Headers.Add("Access-Control-Allow-Methods", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Expose-Headers", "*");
            response.Content_Type = "application/json";
            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "false", msg = "不支持post" }));
            response.SetContent(buffer);
            response.Send();
        }
        private bool started = false;
        //系统一定要先执行get请求
        public override void OnGet(HttpRequest request, HttpResponse response)
        {
            response.Headers = new Dictionary<string, string>();
            response.SetHeader(ResponseHeaders.Allow, "*");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Credentials", "true");
            response.Headers.Add("Access-Control-Allow-Methods", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Expose-Headers", "*");
            response.Content_Type = "application/json";
            response.Encoding = Encoding.UTF8;
            byte[] buffer = new byte[0];
            string data = null;
            if (request.Params != null && (data = request.Params["type"]) != null)
            {
                response.StatusCode = "200";
                bool result = false;

                switch (data)
                {
                    case "start":
                        Form1.EPC = null;
                        result = Form1.instance.startReader();
                        if (result)
                        {
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "true", msg = "打开成功" }));
                        }
                        else
                        {
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "true", msg = "重复打开" }));
                        }
                        break;
                    case "stop":
                        result = Form1.instance.stopReader();
                        if (result)
                        {
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "true", msg = "关闭成功" }));
                        }
                        else
                        {
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "true", msg = "重复关闭" }));
                        }
                        Form1.EPC = null;
                        break;
                    case "get":
                        string readTID = Form1.EPC;
                        if (readTID != null)
                        {
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "true", msg = readTID }));
                        }
                        else
                        {
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "false", msg = "" }));
                        }
                        break;
                    default:
                        buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "false", msg = "参数不合法" }));
                        break;
                }
            }
            else
            {
                buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = "false", msg = "参数不合法" }));
            }
            response.SetContent(buffer);
            // response.Headers.Add("Content-Length", "" + buffer.Length);
            response.Send();
        }
    }
}
