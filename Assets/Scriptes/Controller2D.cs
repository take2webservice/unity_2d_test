using UnityEngine;
using System.Collections;

[RequireComponent (typeof (BoxCollider2D))]
public class Controller2D : RaycastController {

    // 最大登り傾斜角？80度以上登れません的な？
    float maxClimbAngle = 80;
    // 最大下り傾斜角？80度以上は下れず落ちる的な？
    float maxDescendAngle = 80;

    public struct CollisionInfo {
        // 上、下？
        public bool above, below;
        // 左、右？
        public bool left, right;

        // 坂を登ってる？
        public bool climbingSlope;
        // 坂を下ってる？
        public bool descendingSlope;
        // 坂のアングル（角度）、古い坂のアングル(角度)
        public float slopeAngle, slopeAngleOld;
        // 3次元ベクトルで前のフレームでの速度を表現？
        public Vector3 velocityOld;
        // リセット

        public void Reset() {
            // 上下をfalseに
            above = below = false;
            // 左右をfalseに
            left = right = false;
            // 坂は登ってない
            climbingSlope = false;
            // 坂は下ってない
            descendingSlope = false;

            // 現在の角度を古い角度に入れる
            slopeAngleOld = slopeAngle;
            // 現在の角度を0にする
            slopeAngle = 0;
        }
    }
    // 衝突の情報
    public CollisionInfo collisions;

    void Start() {
        // 衝突用のオブジェクトの初期化
        collider = GetComponent<BoxCollider2D> ();
        CalculateRaySpacing ();
    }

    // Playerから毎フレーム呼び出される
    public void Move(Vector3 velocity, bool standingOnPlatform = false) {
        // RayCastの更新？
        // オブジェクトを元に作るRaycastの初期化
        // フレーム時点での位置が毎回スナップショット的に変わるので更新する必要がある
        UpdateRaycastOrigins ();
        // 衝突状態を初期化（全方向で衝突していない）、前回の傾斜の引き継ぎ
        collisions.Reset ();
        // 引数の速度をvelocityOldに入れる？？なぜOld？
        collisions.velocityOld = velocity;

        // 縦方向の速度が0未満なら
        if (velocity.y < 0) {
            // 傾斜を滑らせる
            // 中でvelocityに変化が加わる
            DescendSlope(ref velocity);
        }
        // 縦方向の速度が0なら
        if (velocity.x != 0) {
            // 水平移動させる
            // 中でvelocityに変化が加わる
            HorizontalCollisions (ref velocity);
        }
        // 縦方向の速度が0以外であれば
        if (velocity.y != 0) {
            // 傾斜を登らせる
            // 中でvelocityに変化が加わる
            VerticalCollisions (ref velocity);
        }

        // ゲームオブジェクトを指定した距離だけ移動させる
        transform.Translate (velocity);

        // オブジェクトの上にいる場合、下にぶつかってるとする
        if (standingOnPlatform) {
            collisions.below = true;
        }
    }

    void HorizontalCollisions(ref Vector3 velocity) {
        // x軸の速度の方向が正or0なら1, 負なら-1
        float directionX = Mathf.Sign (velocity.x);
        // x軸の速度の絶対値を取得し、オブジェクトのスキンの厚さを加算
        float rayLength = Mathf.Abs (velocity.x) + skinWidth;

        // horizontalRayCountごとに以下の処理を行う
        // 下から順番に積み上げていく感じ
        for (int i = 0; i < horizontalRayCount; i ++) {
            // 左方向への移動なら、raycastOriginsの左下のVectorを、右方向への移動ならraycastOriginsの右下のVectorを取得
            Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
            // ベクトル "Vector2(0, 1)"に 謎のhorizontalRaySpacing* i したものを加算する？？
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            // 当たり判定を行う
            // Raycastは空間上のある地点から特定方向へ発射されたセンサーのようなもの
            // センサーと接触したすべてのオブジェクトは検知され報告される
            //
            // rayOrigin: 2D 空間上のRaycastが発射される原点
            // Vector2.right:Vector2(1, 0) 
            // =>  Vector2.right * directionX => x軸の移動方向のベクトル
            // rayLength：Raycastを投影する最大距離、ここではx方向の移動速度にスキンの暑さを加算したもの
            // collisionMask: 特定のレイヤーのコライダーのみを判別するためのフィルター、当たり判定の対象となるオブジェクトの種類
            // => 移動した先に衝突しうるオブジェクトがあるかどうか？
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            // Debug.DrawRay: デバッグ用にScene画面にワールド座標にて start （開始地点）から start + dir （開始地点＋方向）までラインを描画
            // 第4引数：描画時間(s)
            // 第5引数：ゴッチャリする場合に表示しない的なの
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red, 10, false);
            // Physics2D.Raycastは暗黙の変換が走る仕様なので、booleanとして扱われる
            if (hit) {
                // 接地してる場合は何もしない？
                if (hit.distance == 0) {
                    continue;
                }
                // 2点間（Vector2(0, 1) と Raycastがヒットしたサーフェスの法線）の角度を計算する
                // サーフェス: Raycastがヒットした面
                // 法線: 2次元ではある線に垂直なベクトル
                // x軸方向に四角いオブジェクトが四角いオブジェクトにぶつかった場合、(1, 0) or (-1, 0)となる
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // iが0かつ、角度が登れる角度の最大より小さい場合 => 登る処理
                if (i == 0 && slopeAngle <= maxClimbAngle) {
                    // すでに坂を下ってる？
                    if (collisions.descendingSlope) {
                        // 坂は下ってないとする
                        collisions.descendingSlope = false;
                        // 速度を衝突オブジェクトの古い速度を入れる
                        velocity = collisions.velocityOld;
                    }
                    // 傾きが始まる距離を一旦0で定義
                    float distanceToSlopeStart = 0;
                    // 想定される傾きと衝突オブジェクトの古い傾きが異なる
                    // 傾きに変化が発生した
                    if (slopeAngle != collisions.slopeAngleOld) {
                        // 傾きが始まる距離 = Raycastがヒットする距離 - スキンの厚み
                        distanceToSlopeStart = hit.distance-skinWidth;
                        // x軸方向の速度から、"傾きが始まる距離"と"移動方向の積"を差し引く
                        // 5行目との違いはなんやこれ？
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    // 速度と角度を渡して、実際に坂を登らせる
                    ClimbSlope(ref velocity, slopeAngle);
                    // x軸の速度に　"傾きが始まる距離"と"移動方向の積" を加算する
                    velocity.x += distanceToSlopeStart * directionX;
                }

                // 衝突オブジェクトで坂を登っていない、または登る角度が最大傾斜より大きい => 登れない
                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) {
                    // x軸の速度は (あたり判定のあるところまでの距離 - スキンの距離) * 移動方向
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    // Raycastの当たり判定があるところまでの距離を代入
                    rayLength = hit.distance;
                    // 衝突オブジェクトが坂を登っている場合
                    if (collisions.climbingSlope) {
                        // 縦方向の速度を計算する
                        // tan(角度*度からラジアンに変換する定数) * x軸方向の速度の絶対値
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }
                    // 衝突オブジェクトの左: 左側に向いてるかどうか
                    collisions.left = directionX == -1;
                    // 衝突オブジェクトの右: 右側に向いてるかどうか
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocity) {
        // 縦方向の速度から向きを算出
        float directionY = Mathf.Sign (velocity.y);
        // 縦方向の速度にスキンの厚さを足す
        float rayLength = Mathf.Abs (velocity.y) + skinWidth;

        // 縦方向のRaycastの本数分回す
        for (int i = 0; i < verticalRayCount; i ++) {
            // directionY == -1 => 落下状態なら、Raycastの発信元を左下に、上向きであれば、左上に設定
            Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
            // Vector2.right = Vector2(1, 0)に 「Raycastの発信スペース * i + x方向の速度」の積を足し合わせ、x軸の発信位置をずらす
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            // RayCastの発信
            // 登っている状態で考える
            // rayOrigin: オブジェクトの左上から発信
            // 方向は縦方向上向き
            // 発信距離は速度+スキン分
            // 判定対象は絞る => 
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            // デバッグ用のRaycastを表示
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength,Color.red);
            // もしぶつかる場合であれば
            if (hit) {
                // y方向の速さを(raycastの当る距離 - スキンの厚さ) * 移動方向 に設定
                velocity.y = (hit.distance - skinWidth) * directionY;
                // rayLengthをraycastの当る距離に設定
                rayLength = hit.distance;
                // 衝突状態が坂を登っているなら
                if (collisions.climbingSlope) {
                    // b / (b/a) = b * (a/b) = a => x軸の速度を三角関数から導出
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }
                // directionYが負なら、衝突状態を下向きに
                collisions.below = directionY == -1;
                // directionYが正なら、衝突状態を上向きに
                collisions.above = directionY == 1;
            }
        }

        // 衝突状態が坂を登っているなら
        if (collisions.climbingSlope) {
            // x軸の移動方向を算出
            float directionX = Mathf.Sign(velocity.x);
            // x方向の移動速度 + スキンの厚さをrayLengthに指定
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            // Raycastの発信元をx軸の移動がが左むきなら、オブジェクトの左下、右むきなら右下に設定、それぞれにVector2(0, 1)* y軸の移動距離を加算する
            Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            // Raycastの発信
            // rayOriginを発信元とし
            // x軸の進行方向向きに
            // x方向の移動距離分
            // 除外対象は除外する的な？
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            // もし当るのであれば
            if (hit) {
                // hit.normalと縦方向のベクトルから傾斜を算出
                float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
                // 現在の傾斜と、既存の衝突オブジェクトの傾斜が異なる場合
                if (slopeAngle != collisions.slopeAngle) {
                    // (x軸方向の速度はRaycastのhitするまでの距離 - スキンの厚さ) * x軸の方向
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    // 傾斜の角度を更新する
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
        // x軸の速度から移動距離を割り出す
        float moveDistance = Mathf.Abs (velocity.x);
        // sin(角度*度からラジアンに変換する定数) * x軸方向の移動距離 => なんぞこれ？
        float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

        // yの移動距離が先ほど求めたclimbVelocityYより小さい場合
        if (velocity.y <= climbVelocityY) {
            // yの速度をclimbVelocityYで上書き
            velocity.y = climbVelocityY;
            // xの速度をcos(角度*度からラジアンに変換する定数) * xの移動距離 => 坂を登る道のり
            // 坂を登る道のり * x軸の移動方向
            velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
            // 衝突オブジェクトは下に動いている？
            collisions.below = true;
            // 衝突オブジェクトは坂を登っている
            collisions.climbingSlope = true;
            // 衝突オブジェクトはの傾斜に引数の傾斜を代入する
            collisions.slopeAngle = slopeAngle;
        }
    }

    // 傾斜を滑らせる
    void DescendSlope(ref Vector3 velocity) {
        // x軸の速度から方向を割り出す
        float directionX = Mathf.Sign (velocity.x);
        // 方向が負なら、rayCastの発信元を右下に、正なら発信元を左下にする。
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        // 移動した先に衝突しうるオブジェクトがあるかどうか？を検証
        // -Vector2.up = Vector2.down = (0, -1) 
        // 仮に / こういう傾斜にいて、左方向に力がかかっているとする
        // rayOrigin = 右下を起点にraycastを発信
        // -Vector2.up　＝　(0, -1) のベクトル = 下向き
        // Mathf.Infinity: raycastの到着範囲を無限とする
        // collisionMask: 当たり判定があるオブジェクトにだけ当る
        RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
        // もしhitしていれば？
        if (hit) {
            // Raycastがヒットしたサーフェスの法線と上むきベクトルの角度を求める
            // ぱっと見計算できなさそうだが計算できてる！！
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            // 角度が0ではなく、傾斜が最大下り角度以下であれば
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
                // rayCastの方向と速度のx方向の向きが同じであれば
                if (Mathf.Sign(hit.normal.x) == directionX) {
                    // 「rayCastの距離 - スキンの厚さ」が 「傾斜のtan* x軸の移動速度、つまりy軸方向の移動距離」
                    // どういうこと？
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) {
                        // x軸方向の移動距離の絶対値
                        float moveDistance = Mathf.Abs(velocity.x);
                        // (b / a) * c　を求めて descendVelocityY ってどういうこと？
                        float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        // (c / a) * c * (x軸の方向)ってどういうこと？
                        velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
                        // 縦方向の移動速度にdescendVelocityYを代入
                        velocity.y -= descendVelocityY;
                        // 衝突オブジェクトに角度を渡す
                        collisions.slopeAngle = slopeAngle;
                        // 衝突オブジェクトを下っている状態にする
                        collisions.descendingSlope = true;
                        // 方向を下にする
                        collisions.below = true;
                    }
                }
            }
        }
    }

}