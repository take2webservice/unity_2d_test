using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformController : RaycastController {

    // 床の上を動くオブジェクトのマスク？
    // Unity側のインスペクターのScriptから選択できる
    public LayerMask passengerMask;

    // 床の移動方向？
    public Vector3 move;

    // 床の上を動くオブジェクトの動きのリスト？
    List<PassengerMovement> passengerMovement;
    // 変形とコントローラの組み合わせの辞書？
    Dictionary<Transform,Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();
    
    public override void Start () {
        // 継承元のRaycastControllerのStartを実行
        // Raycastの準備をしてる
        base.Start ();
    }

    void Update () {
        // 継承元のUpdateRaycastOriginsを実行
        // 移動後の位置でRaycastの準備をしてる
        UpdateRaycastOrigins ();

        // 速度を計算？
        // moveは初期化されてないけど、動くのか？
        // Time.deltaTime: 最後のフレームを完了するのに要した時間
        Vector3 velocity = move * Time.deltaTime;

        // 床の上を動くオブジェクトの動きを計算？
        CalculatePassengerMovement(velocity);

        // 移動前の床の上のオブジェクトの移動？
        MovePassengers (true);
        // 床の移動
        transform.Translate (velocity);
        // 移動後の床の上のオブジェクトを移動？
        MovePassengers (false);
    }

    void MovePassengers(bool beforeMovePlatform) {
        // passengerMovementに対して繰り返し処理
        foreach (PassengerMovement passenger in passengerMovement) {
            // 辞書に移動者がいない場合
            if (!passengerDictionary.ContainsKey(passenger.transform)) {
                // 辞書に移動者として追加
                passengerDictionary.Add(passenger.transform,passenger.transform.GetComponent<Controller2D>());
            }
            // 移動者の動く順番を確認(床の前か後か)
            if (passenger.moveBeforePlatform == beforeMovePlatform) {
                // 辞書からController2Dを取り出し、移動させる。
                // 移動速度
                // 床の上に立っているかどうか？
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    // 動きの計算
    void CalculatePassengerMovement(Vector3 velocity) {
        // 移動後の状態をまとめるSetの初期化
        HashSet<Transform> movedPassengers = new HashSet<Transform> ();
        // 移動状態を突っ込むリストの初期化
        passengerMovement = new List<PassengerMovement> ();

        // x方向の向き
        float directionX = Mathf.Sign (velocity.x);
        // y方向の向き
        float directionY = Mathf.Sign (velocity.y);

        // y方向の動きがある場合
        if (velocity.y != 0) {
            // RayCastの長さを速度+スキンの厚さに設定
            float rayLength = Mathf.Abs (velocity.y) + skinWidth;
            // 継承元のverticalRayCountの数までRayCast実行
            for (int i = 0; i < verticalRayCount; i ++) {
                // 落下方向なら左下から、上昇なら左上から発信
                Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
                // 発信元は少しずつ右にずらす
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                // RayCastを縦の移動方向に向けて発信、長さは速度分だけ
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);
                // もしヒットしたら
                if (hit) {
                    // 移動後の状態をまとめるSetにまだ「衝突したコライダーまたは Rigidbody(物理特性？) の Transform」が含まれているかチェック？
                    // RaycatHit.transform: 衝突したコライダーまたは Rigidbody(物理特性？) の Transform
                    if (!movedPassengers.Contains(hit.transform)) {
                        // 移動状態を突っ込むSetに衝突結果を突っ込む
                        movedPassengers.Add(hit.transform);
                        // y方向の向きが上むきなら、横方向の動きはそのまま、下向きなら横方向の動きは0？
                        float pushX = (directionY == 1)?velocity.x:0;
                        // y方向の速さ - (移動距離 - スキンの厚さ) * 方向？
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;
                        // 移動状態を突っ込むリストに、構造体PassengerMovementを新規作成して突っ込む
                        // 初期値はhitしたコライダーのTransform, x方向y方向の速度, Y方向の向き, 床の前に動かす
                        passengerMovement.Add(new PassengerMovement(hit.transform,new Vector3(pushX,pushY), directionY == 1, true));
                    }
                }
            }
        }

        // x軸方向に動きがある場合
        if (velocity.x != 0) {
            // RayCastの長さを速度+スキンの厚さに設定
            float rayLength = Mathf.Abs (velocity.x) + skinWidth;
            // 継承元のhorizontalRayCountの数までRayCast実行
            for (int i = 0; i < horizontalRayCount; i ++) {
                // Raycastの発信元：左方向への移動なら左下から、右方向への移動なら右下から
                Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
                // 少しずつ高さをずらす
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                // Raycastの発信(移動方向に、移動距離分)
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);
                // もしヒットしたら
                if (hit) {
                    // 移動後の状態をまとめるSetに衝突したコライダーの変形が含まれているかチェック？
                    if (!movedPassengers.Contains(hit.transform)) {
                        // 移動後の状態をまとめるSetに衝突したコライダーを追加
                        movedPassengers.Add(hit.transform);
                        // x方向の速度を計算
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        // y方向の速度を計算
                        float pushY = -skinWidth;
                        // 移動状態を突っ込むリストに、構造体PassengerMovementを新規作成して突っ込む
                        passengerMovement.Add(new PassengerMovement(hit.transform,new Vector3(pushX,pushY), false, true));
                    }
                }
            }
        }

        // Passenger on top of a horizontally or downward moving platform
        // 下向きに速度がある or 縦には動かず横には動いている場合
        // 何やってんだこれ？
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0) {
            // Raycastはスキンの2倍
            float rayLength = skinWidth * 2;
            // 継承元のverticalRayCountの数までRayCast実行
            for (int i = 0; i < verticalRayCount; i ++) {
                // 左上を起点に、少しずつ右にずらして発信
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                // 上方向にRayCastを発信
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);
                // hitしたら？
                if (hit) {
                    // 移動後の状態をまとめるSetに衝突したコライダーの変形が含まれているかチェック？
                    if (!movedPassengers.Contains(hit.transform)) {
                        // 移動後の状態をまとめるSetに衝突したコライダーを追加
                        movedPassengers.Add(hit.transform);
                        // x方向の速度を計算
                        float pushX = velocity.x;
                        // y方向の速度を計算
                        float pushY = velocity.y;
                        // 移動状態を突っ込むリストに、構造体PassengerMovementを新規作成して突っ込む
                        // ここだけ床の移動後に動かす設定
                        passengerMovement.Add(new PassengerMovement(hit.transform,new Vector3(pushX,pushY), true, false));
                    }
                }
            }
        }
    }

    struct PassengerMovement {
        // Transformオブジェクト、動いた結果を取得するKeyとして使われてる？
        public Transform transform;
        // 移動速度
        public Vector3 velocity;
        // 床の上に立っているか　
        public bool standingOnPlatform;
        // 床の移動前に動かすか
        public bool moveBeforePlatform;

        // コンストラクタ
        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform) {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

}