using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour
{
    public float jumpHeight = 4;
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
        gravity = -(2 * jumpHeight) / Mathf.Pow (timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print ("Gravity: " + gravity + "  Jump Velocity: " + jumpVelocity);
    }

    // フレームごとに呼び出される処理
    // フレームごとなので重い処理は禁止
    void Update()
    {
        if (controller.collisions.above || controller.collisions.below) {
            velocity.y = 0;
        }
        // GetAxisRaw: 値はキーボードとジョイスティックの入力によって-1から1の範囲で取得
        // 入力値を2次元ベクトルでローカル変数化
        Vector2 input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));

        if (Input.GetKeyDown (KeyCode.Space) && controller.collisions.below) {
            velocity.y = jumpVelocity;
        }
        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below)?accelerationTimeGrounded:accelerationTimeAirborne);
        // 横軸の移動速度を計算
        velocity.x = input.x * moveSpeed;
        // 縦軸の移動速度を計算
        velocity.y += gravity * Time.deltaTime;
        controller.Move (velocity * Time.deltaTime);
    }
}
