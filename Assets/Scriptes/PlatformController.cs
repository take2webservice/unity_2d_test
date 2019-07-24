using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformController : RaycastController {

    // 床の上を動くオブジェクトのマスク？
    // Unity側のインスペクターのScriptから選択できる
    public LayerMask passengerMask;

    // 床の移動方向？
	public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;
    public float speed;
    public bool cyclic;
    public float waitTime;
    [Range(0,2)]
    public float easeAmount;
    int fromWaypointIndex;
    float percentBetweenWaypoints;
    float nextMoveTime;
    // 床の上を動くオブジェクトの動きのリスト？
    List<PassengerMovement> passengerMovement;
    // 変形とコントローラの組み合わせの辞書？
    Dictionary<Transform,Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();
    
    public override void Start () {
        // 継承元のRaycastControllerのStartを実行
        // Raycastの準備をしてる
        base.Start ();
        // localWaypointsの長さでglobalWaypointsを初期化
	    globalWaypoints = new Vector3[localWaypoints.Length];
        // 長さ分、繰り返してtransformを加算したlocalWaypointをGlobalに代入
        for (int i =0; i < localWaypoints.Length; i++) {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }

    void Update () {
        // 継承元のUpdateRaycastOriginsを実行
        // 移動後の位置でRaycastの準備をしてる
        UpdateRaycastOrigins ();

        // 地面の速度を計算？
        Vector3 velocity = CalculatePlatformMovement();

        // 床の上を動くオブジェクトの動きを計算？
        CalculatePassengerMovement(velocity);

        // 移動前の床の上のオブジェクトの移動？
        MovePassengers (true);
        // 床の移動
        transform.Translate (velocity);
        // 移動後の床の上のオブジェクトを移動？
        MovePassengers (false);
    }
    // 滑らかにするための？
	float Ease(float x) {
        float a = easeAmount + 1;
        return Mathf.Pow(x,a) / (Mathf.Pow(x,a) + Mathf.Pow(1-x,a));
    }

    // 床の動きを計算
    Vector3 CalculatePlatformMovement() {	
        // Time.time: ゲーム開始からの時間(秒)	
        // nextMoveTime： 指定されてないけど動くのか？
        Debug.Log($"nextMoveTime {nextMoveTime}s");
        if (Time.time < nextMoveTime) {
            // Vector3(0, 0, 0)
            return Vector3.zero;
        }
        // fromWaypointIndexをglobalWaypointsの長さで割る
        fromWaypointIndex %= globalWaypoints.Length;
        // (a + 1) % a => 1じゃない？
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        Debug.Log($"toWaypointIndex is {toWaypointIndex}");
        //Vector3.Distance： a と b の間の距離
        // 次のポイントまでの距離
        float distanceBetweenWaypoints = Vector3.Distance (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex]);
        // Time.deltaTime: 最後のフレームを完了するのに要した時間
        // speed:初期化されてない: 速さ(point/s)
        // distanceBetweenWaypoints: 距離
        // 0.5km/h 1km => 0.5/1km = 1/2
        // 時間に時間の逆数を足したらあかんのでは？
        percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
        // Mathf.Clamp01: 0~1の範囲外であれば、0~1の間に入るようにする
        percentBetweenWaypoints = Mathf.Clamp01 (percentBetweenWaypoints);
        // よくわからん値をEaseで滑らかにする？
        float easedPercentBetweenWaypoints = Ease (percentBetweenWaypoints);
        // Vector3.Lerp: 直線上にある 2 つのベクトル間を補間します
        // 徐々に目的地へ移動していくときに使用
        // 次のポイントまでの中間ポイントをとる？
        Vector3 newPos = Vector3.Lerp (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex], easedPercentBetweenWaypoints);
        // percentBetweenWaypointsが1以上
        if (percentBetweenWaypoints >= 1) {
            // percentBetweenWaypointsを0にする
            percentBetweenWaypoints = 0;
            // fromWaypointIndexに1を追加
            fromWaypointIndex ++;
            // 周期的ではない
            if (!cyclic) {
                // fromWaypointIndexがglobalWaypointsの長さより大きい（範囲外？）
                if (fromWaypointIndex >= globalWaypoints.Length-1) {
                    // fromWaypointIndexを0にする
                    fromWaypointIndex = 0;
                    // globalWaypointsを反転させる
                    System.Array.Reverse(globalWaypoints);
                }
            }
            // 次に動き始める時間をフレームの開始する時間と待ち時間の和で設定する
            nextMoveTime = Time.time + waitTime;
        }
        // 新しいポジションと現在のポジションの差分
        return newPos - transform.position;
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

    // Gizmos: スクリーンビューにデバッグで色々表示するやつ
    void OnDrawGizmos() {
        // localWaypointsがnullじゃないなら
        if (localWaypoints != null) {
            // 次に描画されるギズモのカラーを赤に設定します
            Gizmos.color = Color.red;
            // 何かのサイズを0.3にする
            float size = .3f;
            // localWaypointsの長差分繰り返す
            for (int i =0; i < localWaypoints.Length; i ++) {
                // 実行中ならglobalWaypointsの指定の番号を、実行中でなければ、localWaypointsの指定の番号 + 現在のポジション？
                Vector3 globalWaypointPos = (Application.isPlaying)?globalWaypoints[i] : localWaypoints[i] + transform.position;
                // Gizmos.DrawLine: from から to に向かってラインを描画
                // globalWaypointPosから(0, 0.3)を引いた位置から、globalWaypointPosに(0, 0.3)を足した位置までラインを引く
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                // globalWaypointPosから(0.3, 0)を引いた位置から、globalWaypointPosに(0.3)を足した位置までラインを引く
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }
}