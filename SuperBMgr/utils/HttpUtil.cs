using SuperBMgr.models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuperBMgr
{
    public static class HttpUtil
    {
        public static async Task<string> LoginAsync(string url, LoginUser param)
        {
            string res = string.Empty;
            try
            {
                byte[] payload = JsonSrialize.Object2Bytes(param);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                request.ContentLength = payload.Length;
                var streamTask = request.GetRequestStreamAsync();
                await streamTask;
                if (streamTask.Result == null) return res;
                Stream writer = streamTask.Result;
                writer.Write(payload, 0, payload.Length);
                writer.Close();
                var resTask = request.GetResponseAsync();
                await resTask;
                if (!(resTask.Result is HttpWebResponse response)) return res;
                Stream readStream = response.GetResponseStream();
                StreamReader Reader = new StreamReader(readStream, Encoding.UTF8);
                res = Reader.ReadLine();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            return res;
        }

        public static async Task<RequestResponse> PostAsync(string url, string param, string token = null)
        {
            RequestResponse res = null;
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(param);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                request.ContentLength = payload.Length;
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("access_token", token);
                var streamTask = request.GetRequestStreamAsync();
                await streamTask;
                if (streamTask.Result == null) return res;
                Stream writer = streamTask.Result;
                writer.Write(payload, 0, payload.Length);
                writer.Close();
                
                var resTask = request.GetResponseAsync();
                await resTask;
                if (!(resTask.Result is HttpWebResponse response)) return res;
                Stream readStream = response.GetResponseStream();
                StreamReader Reader = new StreamReader(readStream, Encoding.UTF8);
                res = JsonSrialize.Desrialize<RequestResponse>(Reader.ReadLine());
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            return res;
        }
        public static async Task<T> GetAsync<T>(string url, string token = null)
        {
            T res = default(T);
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json;charset=UTF-8";
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("access_token", token);

                var resTask = request.GetResponseAsync();
                await resTask;
                if (!(resTask.Result is HttpWebResponse response)) return res;
                Stream readStream = response.GetResponseStream();
                StreamReader Reader = new StreamReader(readStream, Encoding.UTF8);
                res = JsonSrialize.Desrialize<T>(Reader.ReadLine());
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            return res;
        }

        public static async Task<RequestResponse> PutAsync(string url, string param, string token = null)
        {
            RequestResponse res = null;
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(param);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "PUT";
                request.ContentType = "application/json;charset=UTF-8";
                request.ContentLength = payload.Length;
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("access_token", token);
                var streamTask = request.GetRequestStreamAsync();
                await streamTask;
                if (streamTask.Result == null) return res;
                Stream writer = streamTask.Result;
                writer.Write(payload, 0, payload.Length);
                writer.Close();

                var resTask = request.GetResponseAsync();
                await resTask;
                if (!(resTask.Result is HttpWebResponse response)) return res;
                Stream readStream = response.GetResponseStream();
                StreamReader Reader = new StreamReader(readStream, Encoding.UTF8);
                res = JsonSrialize.Desrialize<RequestResponse>(Reader.ReadLine());
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            return res;
        }
    }
}
