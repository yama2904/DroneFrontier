using Network;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Drone.Network
{
    public class InputPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Input;

        public InputData Input { get; private set; } = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public InputPacket() { }

        public InputPacket(InputData input)
        {
            Input = input;
        }

        protected override IPacket ParseBody(byte[] body)
        {
            int offset = 0;

            // 入力キー数取得
            int keysCount = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // 入力キー取得
            List<KeyCode> keys = new List<KeyCode>();
            for (int count = 0; count < keysCount; count++)
            {
                keys.Add((KeyCode)BitConverter.ToInt16(body, offset));
                offset += sizeof(short);
            }

            // 入力キー数取得
            int downedCount = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // 入力キー取得
            List<KeyCode> downedKeys = new List<KeyCode>();
            for (int count = 0; count < downedCount; count++)
            {
                downedKeys.Add((KeyCode)BitConverter.ToInt16(body, offset));
                offset += sizeof(short);
            }

            // 入力キー数取得
            int uppedCount = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // 入力キー取得
            List<KeyCode> uppedKeys = new List<KeyCode>();
            for (int count = 0; count < uppedCount; count++)
            {
                uppedKeys.Add((KeyCode)BitConverter.ToInt16(body, offset));
                offset += sizeof(short);
            }

            // マウス左クリック
            bool mouseBtnL = BitConverter.ToBoolean(body, offset);
            offset += sizeof(bool);

            // マウス右クリック
            bool mouseBtnR = BitConverter.ToBoolean(body, offset);
            offset += sizeof(bool);

            // X軸マウス移動量取得
            float mouseX = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            // Y軸マウス移動量取得
            float mouseY = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            // スクロール量取得
            float scroll = BitConverter.ToSingle(body, offset);
            offset += sizeof(float);

            // インスタンスを作成して返す
            InputData input = new InputData(keys, downedKeys, uppedKeys, mouseBtnL, mouseBtnR, mouseX, mouseY, scroll);
            return new InputPacket(input);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // 入力キー数
            byte[] keysCount = BitConverter.GetBytes(Input.Keys.Count);

            // 入力キー
            byte[] keys = new byte[0];
            foreach (KeyCode key in Input.Keys)
            {
                keys = keys.Concat(BitConverter.GetBytes((short)key)).ToArray();
            }

            // 入力キー数
            byte[] downedCount = BitConverter.GetBytes(Input.DownedKeys.Count);

            // 入力キー
            byte[] downedKeys = new byte[0];
            foreach (KeyCode key in Input.DownedKeys)
            {
                downedKeys = downedKeys.Concat(BitConverter.GetBytes((short)key)).ToArray();
            }

            // 入力キー数
            byte[] uppedCount = BitConverter.GetBytes(Input.UppedKeys.Count);

            // 入力キー
            byte[] uppedKeys = new byte[0];
            foreach (KeyCode key in Input.UppedKeys)
            {
                uppedKeys = uppedKeys.Concat(BitConverter.GetBytes((short)key)).ToArray();
            }

            // マウスクリック
            byte[] mouseBtnL = BitConverter.GetBytes(Input.MouseButtonL);
            byte[] mouseBtnR = BitConverter.GetBytes(Input.MouseButtonR);

            // マウス移動
            byte[] mouseX = BitConverter.GetBytes(Input.MouseX);
            byte[] mouseY = BitConverter.GetBytes(Input.MouseY);

            // スクロール
            byte[] scroll = BitConverter.GetBytes(Input.MouseScrollDelta);

            return keysCount.Concat(keys)
                            .Concat(downedCount)
                            .Concat(downedKeys)
                            .Concat(uppedCount)
                            .Concat(uppedKeys)
                            .Concat(mouseBtnL)
                            .Concat(mouseBtnR)
                            .Concat(mouseX)
                            .Concat(mouseY)
                            .Concat(scroll)
                            .ToArray();
        }
    }
}