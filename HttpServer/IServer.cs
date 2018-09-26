﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ModuleReaderManager.HttpServer
{
    interface IServer
    {
        /// <summary>
        /// 响应GET方法
        /// </summary>
        /// <param name="request">Http请求</param>
        /// <param name="response"></param>
        void OnGet(HttpRequest request, HttpResponse response);

        /// <summary>
        /// 响应Post方法
        /// </summary>
        /// <param name="request">Http请求</param>
        /// <param name="response"></param>
        void OnPost(HttpRequest request, HttpResponse response);

        /// <summary>
        /// 响应默认请求
        /// </summary>
        /// <param name="request">Http请求</param>
        /// <param name="response"></param>
        void OnDefault(HttpRequest request, HttpResponse response);
    }
}
