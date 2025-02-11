using Newtonsoft.Json;
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
    }
}
