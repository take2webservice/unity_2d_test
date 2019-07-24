using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour
{
    public float jumpHeight = 4;
    // 頂点に達するまでの時間？
    public float timeToJumpApex = .4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;
    float moveSpeed = 6;

    //  壁ジャンプ登り？
	public Vector2 wallJumpClimb;
    
    // 壁ジャンプ停止？
    public Vector2 wallJumpOff;

    // 壁ジャンプ離れる？
    public Vector2 wallLeap;
    // 壁をスライドするスピードの最大値
    public float wallSlideSpeedMax = 3;
    // 壁をスライドするスピードの最大値
    public float wallStickTime = .25f;
    // 壁にひっついていられる時間？
    float timeToWallUnstick;

    float gravity;
    float jumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;
    Controller2D controller;
    // Start is called before the first frame update
    void Start()
    {
        // Controller2Dをコンポーネントとして取得して、インスタンス変数に渡す
        controller = GetComponent<Controller2D> ();
        // -(2 * jumpHeight)/(timeToJumpApex^2) => h = gt^2 / 2 を変形して重力加速度を定義
        // 画面の高さと落下時間から重力加速度を定義した方がメンテしやすい
        gravity = -(2 * jumpHeight) / Mathf.Pow (timeToJumpApex, 2);
        // v = v0 - gt を元に、最奥到達点で速度が0になる初速を算出
        // 0 = v0 - gt => vo = gt
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print ("Gravity: " + gravity + "  Jump Velocity: " + jumpVelocity);
    }

    // フレームごとに呼び出される処理
    // フレームごとなので重い処理は禁止
    void Update()
    {
        // GetAxisRaw: 値はキーボードとジョイスティックの入力によって-1から1の範囲で取得
        // 入力値を2次元ベクトルでローカル変数化
        Vector2 input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));

        int wallDirX = (controller.collisions.left) ? -1 : 1;
        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below)?accelerationTimeGrounded:accelerationTimeAirborne);
        bool wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0) {
            wallSliding = true;
            if (velocity.y < -wallSlideSpeedMax) {
                velocity.y = -wallSlideSpeedMax;
            }
            if (timeToWallUnstick > 0) {
                velocityXSmoothing = 0;
                velocity.x = 0;
                if (input.x != wallDirX && input.x != 0) {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else {
                timeToWallUnstick = wallStickTime;
            }
        }

        // 上方向にぶつかっている or した方向にぶつかっている
        // 縦方向の速度を0にする
        if (controller.collisions.above || controller.collisions.below) {
            velocity.y = 0;
        }

        // キーボードのスペースが押されて
	    if (Input.GetKeyDown (KeyCode.Space)) {
            // 壁滑り状態なら
            if (wallSliding) {
                // 壁の向きとx軸の入力の向きが同じ？
                if (wallDirX == input.x) {
                    // 壁の向きと逆の方向にxの速度を壁のぼり用の速度で飛ばす
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    // 壁登り用の速度で上に飛ばす
                    velocity.y = wallJumpClimb.y;
                }
                // x軸の入力の向きがニュートラル
                else if (input.x == 0) {
                    // 壁ジャンプ停止用の速度で壁から離れる
                    velocity.x = -wallDirX * wallJumpOff.x;
                    // 壁ジャンプ停止用の速度で上に飛ばす
                    velocity.y = wallJumpOff.y;
                }
                // 壁の向きとx軸の入力の向きが逆
                else {
                    // 壁から離れる用の速度で壁から離れる
                    velocity.x = -wallDirX * wallLeap.x;
                    // 壁から離れる用の速度で上に飛ばす
                    velocity.y = wallLeap.y;
                }
            }
            // 下が接触しているなら
            if (controller.collisions.below) {
                // 普通のジャンプ
                velocity.y = jumpVelocity;
            }
        }

        velocity.y += gravity * Time.deltaTime;
        // オブジェクトを移動させる
        controller.Move (velocity * Time.deltaTime);
    }
}
