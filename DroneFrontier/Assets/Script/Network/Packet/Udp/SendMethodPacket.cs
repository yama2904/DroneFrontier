using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Network.Udp
{
    public class SendMethodPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.SendMethod;

        /// <summary>
        /// 実行させるメソッドを持つクラス名
        /// </summary>
        public string ClassName { get; private set; } = string.Empty;

        /// <summary>
        /// 実行させるメソッド名
        /// </summary>
        public string MethodName { get; private set; } = string.Empty;

        /// <summary>
        /// 実行させるメソッドの引数
        /// </summary>
        public object[] Arguments { get; private set; } = new object[0];

        /// <summary>
        /// オブジェクト共有ID
        /// </summary>
        public long ObjectId { get; private set; } = -1;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SendMethodPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="className">メソッドを持つクラス名</param>
        /// <param name="methodName">実行させるメソッド名</param>
        /// <param name="args">実行させるメソッドの引数</param>
        /// <param name="id">オブジェクト共有ID</param>
        public SendMethodPacket(string className, string methodName, object[] args = null, long id = -1)
        {
            ClassName = className;
            MethodName = methodName;
            Arguments = args == null ? new object[0] : args;
            ObjectId = id;
        }

        public override Packet Parse(byte[] data)
        {
            // ボディ部取得
            Split(data, out _, out byte[] body);

            int offset = 0;

            // クラス名のバイト長取得
            int classNameLen = BitConverter.ToInt32(body, offset);
            offset += 4;

            // クラス名取得
            string className = Encoding.UTF8.GetString(body, offset, classNameLen);
            offset += classNameLen;

            // メソッド名のバイト長取得
            int methodNameLen = BitConverter.ToInt32(body, offset);
            offset += 4;

            // メソッド名取得
            string methodName = Encoding.UTF8.GetString(body, offset, methodNameLen);
            offset += classNameLen;

            // メソッド引数のバイト長取得
            int argsLen = BitConverter.ToInt32(body, offset);
            offset += 4;

            // メソッド名取得
            object[] args = ConvertToObjectArray(body.Skip(offset).Take(argsLen).ToArray());
            offset += argsLen;

            // ID取得
            long id = BitConverter.ToInt64(body, offset);
            offset += 8;

            // インスタンスを作成して返す
            return new SendMethodPacket(className, methodName, args, id);
        }

        public override byte[] ConvertToPacket()
        {
            // 先頭はヘッダ部
            byte[] data = GetHeaderBytes();

            // クラス名をバイト変換
            byte[] classNameByte = Encoding.UTF8.GetBytes(ClassName);

            // クラス名のバイト長を取得
            byte[] classNameLen = BitConverter.GetBytes(classNameByte.Length);

            // メソッド名をバイト変換
            byte[] methodNameByte = Encoding.UTF8.GetBytes(MethodName);

            // メソッド名のバイト長を取得
            byte[] methodNameLen = BitConverter.GetBytes(methodNameByte.Length);

            // メソッド引数をバイト変換
            byte[] argsByte = ConvertToByteArray(Arguments);

            // メソッド引数のバイト長を取得
            byte[] argsLen = BitConverter.GetBytes(argsByte.Length);

            // IDをバイト変換
            byte[] idByte = BitConverter.GetBytes(ObjectId);

            // [クラス名バイト長][クラス名][メソッド名バイト長][メソッド名][メソッド引数バイト長][メソッド引数][ID]
            return data.Concat(classNameLen)
                       .Concat(classNameByte)
                       .Concat(methodNameLen)
                       .Concat(methodNameByte)
                       .Concat(argsLen)
                       .Concat(argsByte)
                       .Concat(idByte)
                       .ToArray();
        }

        /// <summary>
        /// object配列をバイト配列へ変換
        /// </summary>
        /// <param name="objects">バイト配列へ変換するobject配列</param>
        /// <returns>変換したバイト配列</returns>
        private byte[] ConvertToByteArray(object[] objects)
        {
            if (objects == null) return new byte[0];

            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, objects);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// object配列をバイト配列へ変換
        /// </summary>
        /// <param name="objects">バイト配列へ変換するobject配列</param>
        /// <returns>変換したバイト配列</returns>
        private object[] ConvertToObjectArray(byte[] bytes)
        {
            if (bytes == null) return new object[0];

            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(memoryStream) as object[];
            }
        }
    }
}
