using System;

namespace Network
{
    public class NetworkException : Exception
    {
        /// <summary>
        /// エラーコード
        /// </summary>
        public ExceptionError ErrorCode { get; private set; } = ExceptionError.NoError;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NetworkException() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="errorCode">エラーコード</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        public NetworkException(ExceptionError errorCode, string errorMessage) : base(errorMessage)
        {
            ErrorCode = errorCode;
        }
    }
}