using System;
using UnityEngine;

namespace Common
{
    public class Useful
    {
        /// <summary>
        /// 指定した桁数より小さい小数部を切り捨て
        /// </summary>
        /// <param name="value">切り捨てる値</param>
        /// <param name="digits">戻り値の小数部の桁数</param>
        /// <returns></returns>
        public static float Floor(float value, int digits)
        {
            if (digits == 0)
            {
                return Mathf.Floor(value);
            }

            float x = Mathf.Pow(10, digits);
            value *= x;
            value = Mathf.Floor(value) / x;

            return value;
        }

        /// <summary>
        /// GameObjectがnull、又はDestroy済みであるか
        /// </summary>
        /// <param name="obj">nullチェックオブジェクト</param>
        /// <returns>null、又はDestroy済みの場合はtrue</returns>
        public static bool IsNullOrDestroyed(GameObject obj)
        {
            return obj == null || obj is null || !obj;
        }

        /// <summary>
        /// 正規分布に基づいたランダム値を生成
        /// </summary>
        /// <param name="sigma">偏差値</param>
        /// <param name="ave">平均値</param>
        /// <param name="abs">絶対値で返すか</param>
        /// <returns>生成したランダム値</returns>
        public static float RandomByNormalDistribution(float sigma = 1f, float ave = 0, bool abs = true)
        {
            float x = UnityEngine.Random.value;
            float y = UnityEngine.Random.value;
            float value = sigma * (float)(Math.Sqrt(-2.0 * Math.Log(x)) * Math.Cos(2.0 * Math.PI * y)) + ave;
            //Debug.Log($"sigma:{sigma}, ave:{ave}, value => {value}");
            return abs ? Mathf.Abs(value) : value;
        }
    }
}