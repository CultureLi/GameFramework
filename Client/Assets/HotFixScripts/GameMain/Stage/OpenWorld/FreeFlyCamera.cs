using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// Pans the camera by "grab-drag" on the ground plane: press and drag a mouse button,
    /// the world point under the cursor stays glued to the cursor. Scroll wheel changes
    /// altitude within [minHeight, maxHeight]. Camera rotation is auto-aimed at the ground
    /// (optional) or left alone. XZ movement is optionally clamped to the map bounds so the
    /// camera's ground projection cannot leave the terrain grid.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FreeFlyCamera : MonoBehaviour
    {
        [Header("Altitude")]
        public float minHeight = 200f;
        public float maxHeight = 800f;
        [Tooltip("Height change per scroll notch.")]
        public float scrollSensitivity = 60f;
        [Tooltip("Optional smoothing time for altitude changes (seconds). Use 0 for snap.")]
        public float heightSmoothTime = 0.15f;

        [Header("Drag")]
        [Tooltip("Mouse button used to drag-pan the ground. 0=Left, 1=Right, 2=Middle.")]
        public int dragMouseButton = 0;
        [Tooltip("World-space Y of the ground plane used for drag raycasts and auto-aim.")]
        public float groundY = 0f;

        [Header("Look")]
        [Tooltip("If true, camera pitches toward a forward ground point so the terrain stays framed.")]
        public bool autoAimAtGround = true;
        [Tooltip("Forward distance (m) from the camera XZ position to the aim point at ground level.")]
        public float aimForwardOffset = 120f;

        [Header("Map bounds")]
        [Tooltip("If true, clamp the camera's XZ (its ground projection) inside the map rectangle.")]
        public bool clampToMapBounds = true;
        [Tooltip("If assigned, mapMinXZ / mapMaxXZ are ignored and bounds are derived from the streaming component (gridOriginXZ + gridSize * blockSize).")]
        public WorldChunkStreaming boundsSource;
        public Vector2 mapMinXZ = new Vector2(0f, 0f);
        public Vector2 mapMaxXZ = new Vector2(1000f, 1000f);
        [Tooltip("Shrinks the allowed area by this many meters on each side. Increase (e.g. to aimForwardOffset) if you also want the camera's aim point to stay on the map.")]
        public float boundsPadding = 0f;

        Camera _cam;
        float _targetHeight;
        float _heightVel;
        bool _dragging;
        Vector3 _grabWorld;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _targetHeight = Mathf.Clamp(transform.position.y, minHeight, maxHeight);
            var p = transform.position;
            p.y = _targetHeight;
            transform.position = p;
            if (clampToMapBounds) ClampPositionToBounds();
        }

        void Update()
        {
            HandleAltitude();
            if (autoAimAtGround) AimAtGround();
            HandleDrag();
            // Final safety net — altitude / rotation changes can also grow the frustum past
            // the map even without a drag event, so re-clamp XZ every frame.
            if (clampToMapBounds) transform.position = ClampByFrustum(transform.position);
        }

        void HandleAltitude()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                _targetHeight = Mathf.Clamp(_targetHeight - scroll * scrollSensitivity, minHeight, maxHeight);

            var p = transform.position;
            if (heightSmoothTime > 0f)
                p.y = Mathf.SmoothDamp(p.y, _targetHeight, ref _heightVel, heightSmoothTime);
            else
                p.y = _targetHeight;
            transform.position = p;
        }

        void HandleDrag()
        {
            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                if (RaycastGround(Input.mousePosition, out var grab))
                {
                    _grabWorld = grab;
                    _dragging = true;
                }
            }
            if (!Input.GetMouseButton(dragMouseButton))
            {
                _dragging = false;
                return;
            }
            if (!_dragging) return;

            // Recompute the world point under the current cursor with the *current* camera
            // position, then shift the camera so the original grab point returns beneath the
            // cursor. Iterating this each frame keeps the grabbed spot glued to the mouse.
            if (RaycastGround(Input.mousePosition, out var current))
            {
                Vector3 delta = _grabWorld - current;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0f)
                {
                    Vector3 target = transform.position + delta;
                    Vector3 clamped = clampToMapBounds ? ClampByFrustum(target) : target;
                    transform.position = clamped;

                    // If the clamp actually capped the move, the drag anchor would keep pulling
                    // in the same direction next frame and produce jitter. Re-anchor the grab
                    // point to whatever the cursor is now over, so the pan feels like it hits
                    // a soft wall instead of accumulating a stale delta.
                    if ((target - clamped).sqrMagnitude > 1e-6f &&
                        RaycastGround(Input.mousePosition, out var reGrab))
                    {
                        _grabWorld = reGrab;
                    }
                }
            }
        }

        bool RaycastGround(Vector2 screenPos, out Vector3 worldPoint)
        {
            worldPoint = default;
            var cam = _cam != null ? _cam : Camera.main;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;
            float t = (groundY - ray.origin.y) / ray.direction.y;
            if (t <= 0f) return false;
            worldPoint = ray.origin + ray.direction * t;
            return true;
        }

        void AimAtGround()
        {
            Vector3 p = transform.position;
            Vector3 aim = new Vector3(p.x, groundY, p.z + aimForwardOffset);
            transform.rotation = Quaternion.LookRotation(aim - p, Vector3.up);
        }

        void ClampPositionToBounds()
        {
            // At Awake time camera rotation isn't set yet (AimAtGround runs in Update), so
            // fall back to plain XZ clamping. Update() will re-clamp via ClampByFrustum once
            // rotation is ready.
            transform.position = ClampXZ(transform.position);
        }

        Vector3 ClampXZ(Vector3 pos)
        {
            GetBounds(out Vector2 min, out Vector2 max);
            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
            pos.z = Mathf.Clamp(pos.z, min.y, max.y);
            return pos;
        }

        /// <summary>
        /// Clamp <paramref name="pos"/> so the camera's frustum ground-projection AABB stays
        /// inside the map. Since the auto-aim rotation depends only on camera Y (aim vector is
        /// (0, groundY - camY, aimForwardOffset)), the frustum quad is translation-invariant in
        /// XZ — we measure the quad's AABB offsets from the *current* camera XZ and use those
        /// same offsets to derive the legal window for <paramref name="pos"/>.
        /// </summary>
        Vector3 ClampByFrustum(Vector3 pos)
        {
            GetBounds(out Vector2 min, out Vector2 max);
            if (!ComputeFrustumAabbOffsets(out Vector2 lo, out Vector2 hi))
            {
                pos.x = Mathf.Clamp(pos.x, min.x, max.x);
                pos.z = Mathf.Clamp(pos.z, min.y, max.y);
                return pos;
            }

            float xLo = min.x - lo.x;
            float xHi = max.x - hi.x;
            if (xLo <= xHi) pos.x = Mathf.Clamp(pos.x, xLo, xHi);
            else pos.x = 0.5f * (min.x + max.x - lo.x - hi.x);  // frustum wider than map → center it

            float zLo = min.y - lo.y;
            float zHi = max.y - hi.y;
            if (zLo <= zHi) pos.z = Mathf.Clamp(pos.z, zLo, zHi);
            else pos.z = 0.5f * (min.y + max.y - lo.y - hi.y);

            return pos;
        }

        /// <summary>
        /// Returns the XZ AABB of the camera's ground-projected frustum expressed as offsets
        /// from the camera's current XZ (so lo/hi are frustum-local extents that translate
        /// linearly with the camera).
        /// </summary>
        bool ComputeFrustumAabbOffsets(out Vector2 lo, out Vector2 hi)
        {
            lo = default; hi = default;
            var cam = _cam != null ? _cam : Camera.main;
            if (cam == null) return false;

            var corners = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(1f, 1f, 0f), new Vector3(0f, 1f, 0f),
            };
            float cap = cam.farClipPlane;
            Vector2 camXZ = new Vector2(transform.position.x, transform.position.z);
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Ray ray = cam.ViewportPointToRay(corners[i]);
                Vector3 hit;
                if (Mathf.Abs(ray.direction.y) < 1e-6f)
                {
                    hit = ray.origin + ray.direction * cap;
                }
                else
                {
                    float t = (groundY - ray.origin.y) / ray.direction.y;
                    if (t <= 0f || t > cap) t = cap;
                    hit = ray.origin + ray.direction * t;
                }
                if (hit.x < minX) minX = hit.x;
                if (hit.x > maxX) maxX = hit.x;
                if (hit.z < minZ) minZ = hit.z;
                if (hit.z > maxZ) maxZ = hit.z;
            }
            lo = new Vector2(minX - camXZ.x, minZ - camXZ.y);
            hi = new Vector2(maxX - camXZ.x, maxZ - camXZ.y);
            return true;
        }

        void GetBounds(out Vector2 min, out Vector2 max)
        {
            if (boundsSource != null)
            {
                min = boundsSource.gridOriginXZ;
                float side = boundsSource.gridSize * boundsSource.blockSize;
                max = new Vector2(min.x + side, min.y + side);
            }
            else
            {
                min = mapMinXZ;
                max = mapMaxXZ;
            }
            min += new Vector2(boundsPadding, boundsPadding);
            max -= new Vector2(boundsPadding, boundsPadding);
            if (max.x < min.x) max.x = min.x;
            if (max.y < min.y) max.y = min.y;
        }

        public float CurrentHeight01 => Mathf.InverseLerp(minHeight, maxHeight, transform.position.y);

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!clampToMapBounds) return;
            GetBounds(out Vector2 min, out Vector2 max);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Vector3 a = new Vector3(min.x, groundY + 0.1f, min.y);
            Vector3 b = new Vector3(max.x, groundY + 0.1f, min.y);
            Vector3 c = new Vector3(max.x, groundY + 0.1f, max.y);
            Vector3 d = new Vector3(min.x, groundY + 0.1f, max.y);
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        }
#endif
    }
}
