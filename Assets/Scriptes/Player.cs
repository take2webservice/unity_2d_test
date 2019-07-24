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
        // 上方向にぶつかっている or した方向にぶつかっている
        // 縦方向の速度を0にする
        if (controller.collisions.above || controller.collisions.below) {
            velocity.y = 0;
        }
        // GetAxisRaw: 値はキーボードとジョイスティックの入力によって-1から1の範囲で取得
        // 入力値を2次元ベクトルでローカル変数化
        Vector2 input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));
        // スペースキーが押されて、下方向がぶつかっている
        if (Input.GetKeyDown (KeyCode.Space) && controller.collisions.below) {
            // 縦方向の速度をMathf.Abs(gravity) * timeToJumpApexにする
            velocity.y = jumpVelocity;
        　}
        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below)?accelerationTimeGrounded:accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
        // オブジェクトを移動させる
        controller.Move (velocity * Time.deltaTime);
    }
}
