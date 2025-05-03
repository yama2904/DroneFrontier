using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Network
{
    public class NetworkUtil
    {
        private static JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        /// <summary>
        /// 任意の型をバイト配列へ変換
        /// </summary>
        /// <param name="obj">バイト配列へ変換する任意の型</param>
        /// <returns>変換したバイト配列</returns>
        public static byte[] ConvertToByteArray<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// バイト配列を任意の型へ変換
        /// </summary>
        /// <param name="bytes">任意の型へ変換するバイト配列</param>
        /// <returns>変換した任意の型</returns>
        public static T ConvertToObject<T>(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        /// <summary>
        /// IPアドレスとポート番号の組み合わせを文字列へ変換する
        /// </summary>
        /// <param name="ip">IPアドレス</param>
        /// <param name="port">ポート番号</param>
        /// <returns>変換した文字列</returns>
        public static string ConvertToString(IPAddress ip, int port)
        {
            return ConvertToString(new IPEndPoint(ip, port));
        }

        /// <summary>
        /// IPエンドポイントを文字列へ変換する
        /// </summary>
        /// <param name="ep">文字列へ変換するエンドポイント</param>
        /// <returns>変換した文字列</returns>
        public static string ConvertToString(IPEndPoint ep)
        {
            string ip = ep.Address.ToString();
            string port = ep.Port.ToString();
            return $"{ip}:{port}";
        }

        /// <summary>
        /// 文字列変換されたIPエンドポイントをIPEndPointクラスへ変換する
        /// </summary>
        /// <param name="ep">IPEndPointクラスへ変換する文字列</param>
        /// <returns>変換したIPEndPoint</returns>
        public static IPEndPoint ConvertToIPEndPoint(string ep)
        {
            string[] splitEp = ep.Split(":");
            IPAddress ip = IPAddress.Parse(splitEp[0]);
            int port = int.Parse(splitEp[1]);

            return new IPEndPoint(ip, port);
        }
    }
}
