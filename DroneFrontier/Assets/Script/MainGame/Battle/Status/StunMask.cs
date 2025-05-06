using Common;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

public class StunMask : MonoBehaviour
{
    public event EventHandler OnStunEnd;

    public async void Run(Canvas parent, float stunSec)
    {
        // マスクを前面に表示させる
        transform.SetParent(parent.transform, false);
        transform.SetAsLastSibling();

        // 最初の1秒間は真っ白
        await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: true);

        // 徐々にマスク解除
        await ImageFader.FadeOut(GetComponent<Image>(), stunSec - 1);
        OnStunEnd(this, EventArgs.Empty);
    }
}
