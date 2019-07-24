using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
    // 当たり判定の対象となるオブジェクトの種類
    public LayerMask collisionMask;
    
    // スキンの厚さ
    public const float skinWidth = .015f;
    // x軸方向のRaycastの本数
    public int horizontalRayCount = 4;
    // y軸方向のRaycastの本数
    public int verticalRayCount = 4;

    // x軸方向のRaycastの間隔
    [HideInInspector]
    public float horizontalRaySpacing;
    // y軸方向のRaycastの間隔
    [HideInInspector]
    public float verticalRaySpacing;

    // 衝突オブジェクト
    [HideInInspector]
    public BoxCollider2D collider;
    // Raycastの発信元
    public RaycastOrigins raycastOrigins;

    // Raycastの発信元の構造体
    public struct RaycastOrigins {
        // 左上、右上の座標
        public Vector2 topLeft, topRight;
        // 左下、右下の座標
        public Vector2 bottomLeft, bottomRight;
    }

    // Start is called before the first frame update
    public virtual void Start() {
        // スクリプトと一緒にセットされたコンポーネントBoxCollider2Dを取得
        collider = GetComponent<BoxCollider2D> ();
        // RaySpaceの計算
        CalculateRaySpacing ();
    }

    public void CalculateRaySpacing() {
        // 衝突オブジェクトの外周を取得
        Bounds bounds = collider.bounds;
        // 衝突オブジェクトの外周を少し小さくする
        bounds.Expand (skinWidth * -2);
        
        // x軸方向のRaycastの本数が2~Int最大値になるようにする。
        // ぱっと見horizontalRayCountは書き変わらないしこれ必要かね？
        horizontalRayCount = Mathf.Clamp (horizontalRayCount, 2, int.MaxValue);
        // y軸方向のRaycastの本数が2~Int最大値になるようにする。
        // ぱっと見horizontalRayCountは書き変わらないしこれ必要かね？
        verticalRayCount = Mathf.Clamp (verticalRayCount, 2, int.MaxValue);

        // x方向のRayCastの間隔を
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        // y方向のRayCastの間隔を
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public void UpdateRaycastOrigins() {
        // 衝突オブジェクトの外周を取得
        Bounds bounds = collider.bounds;
        // 衝突オブジェクトの外周を少し小さくする
        bounds.Expand (skinWidth * -2);
        
        // boundsを元にRaycastの発信元を設定
        raycastOrigins.bottomLeft = new Vector2 (bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2 (bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2 (bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2 (bounds.max.x, bounds.max.y);
    }
}
