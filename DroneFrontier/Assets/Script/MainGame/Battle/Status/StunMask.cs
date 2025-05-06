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
        // �}�X�N��O�ʂɕ\��������
        transform.SetParent(parent.transform, false);
        transform.SetAsLastSibling();

        // �ŏ���1�b�Ԃ͐^����
        await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: true);

        // ���X�Ƀ}�X�N����
        await ImageFader.FadeOut(GetComponent<Image>(), stunSec - 1);
        OnStunEnd(this, EventArgs.Empty);
    }
}
