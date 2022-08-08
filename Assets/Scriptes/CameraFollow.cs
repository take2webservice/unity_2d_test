using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 追従するターゲット
    public Controller2D target;

    // たての範囲
    public float verticalOffset;
    // x軸方向に追いかけ始める距離？
    public float lookAheadDstX;
    // x軸方向に追いかけ始める距離？
    public float lookSmoothTimeX;
    // たてのスムーズな時間？
    public float verticalSmoothTime;
    // フォーカスするサイズ？
    public Vector2 focusAreaSize;

    //実際にフォーカスしてるエリア
    FocusArea focusArea;

    // 現在のx方向の先読み？
    float currentLookAheadX;

    // x方向の先読み？
    float targetLookAheadX;

    // 先読みのx方向の向き（左・右）
    float lookAheadDirX;

    // x方向に追いかけるスムースな速度？
    float smoothLookVelocityX;

    // y方向に追いかけるスムースな速度？
    float smoothVelocityY;

    // 停止の先読み
    bool lookAheadStopped;

    void Start() {
        // フォーカスエリアを初期化
        // 範囲は追従するターゲットのコライダーの周辺
        // フォーカスエリアの範囲
        focusArea = new FocusArea (target.collider.bounds, focusAreaSize);
    }

    // LateUpdate は Update 関数が呼び出された後に実行
    // カメラ追従などの場合に利用
    void LateUpdate() {
        // フォーカスを更新
        focusArea.Update (target.collider.bounds);
        // フォーカスエリアの中心に、(1,0) * verticalOffset の話を代入
        // なぜ少し上にあげる？
        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        // フォーカスエリアの横方向の速度が0以外 => 横に動いている時
        if (focusArea.velocity.x != 0) {
            // フォーカスエリアの速度から向きを判定
            lookAheadDirX = Mathf.Sign (focusArea.velocity.x);
            // ユーザーの入力の向きとフォーカスエリアの向きが同じで、ユーザー入力が動いている場合
            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0) {
                // カメラは動いている状態とし
                lookAheadStopped = false;
                // ターゲットのX方向の追尾はカメラの追いかける向き(左・右) * カメラの追いかける距離？
                // カメラの追従速度？
                targetLookAheadX = lookAheadDirX * lookAheadDstX;
            }
            // ユーザーの入力の向きとフォーカスエリアの向きが異なるか、ユーザー入力が動いていない場合？
            else {
                // // カメラは動いている状態なら
                if (!lookAheadStopped) {
                    // カメラを止め
                    lookAheadStopped = true;
                    // カメラの追従速度にcurrentLookAheadX？ + (カメラの追いかける向き(1 or -1) * カメラの追いかける距離？ - currentLookAheadX/4)
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDstX - currentLookAheadX)/4f;
                }
            }
        }

        // Mathf.SmoothDamp: 徐々に時間をかけて望む目標に向かって値を変更
        // currentLookAheadX: 現在の値
        // targetLookAheadX: 目標の値
        // smoothLookVelocityX: 現在の速度。**この値は関数が呼び出されるたびに変更されます。**
        // lookSmoothTimeX: 目的の状態になるためののおおよその時間
        currentLookAheadX = Mathf.SmoothDamp (currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);
        // フォーカス位置のyの場所
        // 変形のyの位置がフォーカスのyの位置になるまで、verticalSmoothTime秒かけてsmoothVelocityYの速さを少しずつ早めながら変化させる。
        focusPosition.y = Mathf.SmoothDamp (transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
        // フォーカスポジションに(0, 1)とcurrentLookAheadXの積を足す
        focusPosition += Vector2.right * currentLookAheadX;
        // オブジェクトの位置をfocusPositionにと(0, 0, 1)に-10をかけた値(つまり(0, 0, -10))の和に移動させる
        // これなんでz軸はいるんだ？ => 外すとカメラがどっかに行く… => カメラの位置は常に手前なので - 1より小さい値をかけていればOK
        transform.position = (Vector3)focusPosition + Vector3.forward * -10;
    }

    // デバッグ用の表示
    void OnDrawGizmos() {
        Gizmos.color = new Color (1, 0, 0, .5f);
        Gizmos.DrawCube (focusArea.centre, focusAreaSize);
    }

    struct FocusArea {
        public Vector2 centre;
        public Vector2 velocity;
        float left,right;
        float top,bottom;

        public FocusArea(Bounds targetBounds, Vector2 size) {
            // 左にターゲットの周辺から、カメラのフォーカスするサイズのx方向の半分を引いた値
            left = targetBounds.center.x - size.x/2;
            // 右にターゲットの周辺から、カメラのフォーカスするサイズのx方向の半分を足した値
            right = targetBounds.center.x + size.x/2;
            // 下はターゲットの周辺のy方向の最小値
            bottom = targetBounds.min.y;
            // 上はターゲットの周辺のy方向の最小値にカメラのフォーカスするサイズのy方向を足したもの
            top = targetBounds.min.y + size.y;
            // 速度は一旦0
            velocity = Vector2.zero;
            // 上下左右の値から中心を割り出す
            centre = new Vector2((left+right)/2,(top +bottom)/2);
        }

        public void Update(Bounds targetBounds) {
            float shiftX = 0;
            if (targetBounds.min.x < left) {
                shiftX = targetBounds.min.x - left;
            } else if (targetBounds.max.x > right) {
                shiftX = targetBounds.max.x - right;
            }
            left += shiftX;
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < bottom) {
                shiftY = targetBounds.min.y - bottom;
            } else if (targetBounds.max.y > top) {
                shiftY = targetBounds.max.y - top;
            }
            top += shiftY;
            bottom += shiftY;
            centre = new Vector2((left+right)/2,(top +bottom)/2);
            velocity = new Vector2 (shiftX, shiftY);
        }
    }

}
