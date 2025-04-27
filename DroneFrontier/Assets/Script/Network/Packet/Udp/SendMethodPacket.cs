using System;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    public class SendMethodPacket : BasePacket
    {
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
        public string ObjectId { get; private set; } = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SendMethodPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">オブジェクト共有ID</param>
        /// <param name="className">メソッドを持つクラス名</param>
        /// <param name="methodName">実行させるメソッド名</param>
        /// <param name="args">実行させるメソッドの引数</param>
        public SendMethodPacket(string id, string className, string methodName, object[] args = null)
        {
            ObjectId = id;
            ClassName = className;
            MethodName = methodName;
            Arguments = args == null ? new object[0] : args;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            // IDのバイト長取得
            int idLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // ID取得
            string id = Encoding.UTF8.GetString(body, offset, idLen);
            offset += idLen;

            // クラス名のバイト長取得
            int classNameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // クラス名取得
            string className = Encoding.UTF8.GetString(body, offset, classNameLen);
            offset += classNameLen;

            // メソッド名のバイト長取得
            int methodNameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // メソッド名取得
            string methodName = Encoding.UTF8.GetString(body, offset, methodNameLen);
            offset += methodNameLen;

            // メソッド引数のバイト長取得
            int argsLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // メソッド引数取得
            object[] args = NetworkUtil.ConvertToObject<object[]>(body.Skip(offset).Take(argsLen).ToArray());
            offset += argsLen;

            // インスタンスを作成して返す
            return new SendMethodPacket(id, className, methodName, args);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // IDをバイト変換
            byte[] idByte = Encoding.UTF8.GetBytes(ObjectId);

            // IDのバイト長を取得
            byte[] idLen = BitConverter.GetBytes(idByte.Length);

            // クラス名をバイト変換
            byte[] classNameByte = Encoding.UTF8.GetBytes(ClassName);

            // クラス名のバイト長を取得
            byte[] classNameLen = BitConverter.GetBytes(classNameByte.Length);

            // メソッド名をバイト変換
            byte[] methodNameByte = Encoding.UTF8.GetBytes(MethodName);

            // メソッド名のバイト長を取得
            byte[] methodNameLen = BitConverter.GetBytes(methodNameByte.Length);

            // メソッド引数をバイト変換
            byte[] argsByte = NetworkUtil.ConvertToByteArray(Arguments);

            // メソッド引数のバイト長を取得
            byte[] argsLen = BitConverter.GetBytes(argsByte.Length);

            // [IDバイト長][ID][クラス名バイト長][クラス名][メソッド名バイト長][メソッド名][メソッド引数バイト長][メソッド引数]
            return idLen.Concat(idByte)
                        .Concat(classNameLen)
                        .Concat(classNameByte)
                        .Concat(methodNameLen)
                        .Concat(methodNameByte)
                        .Concat(argsLen)
                        .Concat(argsByte)
                        .ToArray();
        }
    }
}
