using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class ExternalHttpCaller
        : EmulateClasses.IExternalCaller
    {
        readonly IUrlHolder urlHolder;
        readonly ILogger logger;

        Action<string, string, string> Callback = (_, _, _) => { };

        [System.Serializable]
        public class Request
        {
            public string request;
        }

        [System.Serializable]
        public class Response
        {
            public string verify;
            public string response;
        }

        public ExternalHttpCaller(
            IUrlHolder urlHolder,
            ILogger logger
        )
        {
            this.urlHolder = urlHolder;
            this.logger = logger;
        }

        public async void CallExternal(string request, string meta)
        {
            using (var client = new HttpClient())
            {
                //CSETODO 文字数制限入れる。
                var requestJson = GetRequestJson(request);
                var postData = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var result = await client.PostAsync(urlHolder.url, postData);
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    //CSETODO notfoundでどういうメッセージが帰ってくる？
                    var reason = string.Format("{0}:{1}:{2}", result.StatusCode, result.ReasonPhrase, urlHolder.url);
                    Callback.Invoke(null, meta, reason);
                    logger.Error(reason);
                    return;
                }

                var json = await result.Content.ReadAsStringAsync();
                Response response = null;
                try
                {
                    response = UnityEngine.JsonUtility.FromJson<Response>(json);
                }
                catch(ArgumentException e)
                {
                    logger.Error("response is not json format.");
                    logger.Error(json);
                    return;
                }
                Callback.Invoke(response.response, meta, null);

            }
        }

        string GetRequestJson(string request)
        {
            var r = new Request() { request = request };
            var ret = UnityEngine.JsonUtility.ToJson(r);
            return ret;
        }

        public void SetCallEndCallback(Action<string, string, string> Callback)
        {
            this.Callback = Callback;
        }
    }
}
