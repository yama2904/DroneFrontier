using Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Drone
{
    public class InputData
    {
        public List<KeyCode> Keys { get; private set; } = new List<KeyCode>();
        public List<KeyCode> DownedKeys { get; private set; } = new List<KeyCode>();
        public List<KeyCode> UppedKeys { get; private set; } = new List<KeyCode>();
        public bool MouseButtonL { get; private set; } = false;
        public bool MouseButtonR { get; private set; } = false;
        public float MouseX { get; private set; } = 0;
        public float MouseY { get; private set; } = 0;
        public float MouseScrollDelta { get; private set; } = 0;

        public InputData()
        {
        }

        public InputData(List<KeyCode> keys, List<KeyCode> downedKeys, List<KeyCode> uppedKeys, bool mouseBtnL, bool mouseBtnR, float mouseX, float mouseY, float scroll)
        {
            Keys = keys;
            DownedKeys = downedKeys;
            UppedKeys = uppedKeys;
            MouseButtonL = mouseBtnL;
            MouseButtonR = mouseBtnR;
            MouseX = mouseX;
            MouseY = mouseY;
            MouseScrollDelta = scroll;
        }

        public void UpdateInput()
        {
            Clear();

            // 入力キー取得
            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKey(code))
                {
                    Keys.Add(code);
                }
                if (Input.GetKeyDown(code))
                {
                    DownedKeys.Add(code);
                }
                if (Input.GetKeyUp(code))
                {
                    UppedKeys.Add(code);
                }
            }

            // マウスクリック
            MouseButtonL = Input.GetMouseButton(0);
            MouseButtonR = Input.GetMouseButton(1);

            // マウスによる向き取得
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                MouseX = Input.GetAxis("Mouse X") * CameraManager.ReverseX * CameraManager.CameraSpeed;
                MouseY = Input.GetAxis("Mouse Y") * CameraManager.ReverseY * CameraManager.CameraSpeed;
            }

            // マウススクロール取得
            MouseScrollDelta = Input.mouseScrollDelta.y;
        }

        public void Clear()
        {
            Keys.Clear();
            DownedKeys.Clear();
            UppedKeys.Clear();
            MouseButtonL = false;
            MouseButtonR = false;
            MouseX = 0;
            MouseY = 0;
            MouseScrollDelta = 0;
        }
    }
}