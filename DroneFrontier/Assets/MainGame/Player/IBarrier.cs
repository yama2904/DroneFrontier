using System.Collections;

public interface IBarrier
{
    float HP { get; set; }
    float RegeneTime { get; set; }  //バリアが回復しだす時間
    float RegeneValue { get; set; } //バリアの回復量
    float RepairBarrierTime { get; set; }   //バリアが破壊されてから修復される時間
    float Reduction { get; set; }           //ダメージの軽減率

    void Regene(float value);    //毎秒value値HPを回復させる
    void Damage(float power);    //バリアにダメージを与える
}
