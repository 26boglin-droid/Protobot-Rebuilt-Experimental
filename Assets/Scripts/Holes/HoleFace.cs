using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// World-space overlay that visually marks a selected hole on a part.
    ///
    /// The HoleCollider that owns the HoleData runs its own Update() every frame
    /// and keeps holeData.position / holeData.rotation / holeData.forward in sync
    /// with the part's current transform.  This Update() mirrors those live values
    /// so the overlay automatically follows the hole when the part moves or rotates
    /// (e.g. via the ring gizmo or the Properties Menu rotation fields).
    /// </summary>
    public class HoleFace : MonoBehaviour {
        public Vector3 direction;
        public Vector3 position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Quaternion LookRotation => Quaternion.LookRotation(direction, transform.up);
        public HoleData hole;

        [CacheComponent] private MeshFilter meshFilter;

        // +1 if direction was along hole.forward when Set() was called, -1 if opposite.
        // Stored so Update() can recalculate the correct face direction as the part rotates.
        private float dirSign = 1f;

        private void OnEnable() => ViewportPresentation.Changed();
        private void OnDisable() => ViewportPresentation.Changed();

        public void Set(HoleData newHole, Vector3 newDir) {
            var pose = transform;
            var rotation = Quaternion.LookRotation(-newDir, newHole.rotation * Vector3.up);
            var newPos = newHole.position + newDir * (newHole.depth / 2);
            var scale = new Vector3(newHole.size.x, newHole.size.y, 0.001f);
            bool moved = pose.rotation != rotation || pose.position != newPos;
            bool resized = pose.localScale != scale;
            bool reshaped = meshFilter.sharedMesh != newHole.shape;
            if (hole != newHole || direction != newDir || moved || resized || reshaped) ViewportPresentation.Changed();
            if (moved) pose.SetPositionAndRotation(newPos, rotation);
            if (resized) pose.localScale = scale;
            if (reshaped) meshFilter.sharedMesh = newHole.shape;
            dirSign = Vector3.Dot(newDir, newHole.forward) >= 0f ? 1f : -1f;
            direction = newDir;
            hole = newHole;
        }

        public void Set(HoleFace newHoleFace) => Set(newHoleFace.hole, newHoleFace.direction);

        private void Update() {
            if (hole == null) return;

            // hole.position, hole.rotation, and hole.forward are kept current every frame
            // by HoleCollider.Update(), so using them here tracks the hole as the part moves.
            direction = hole.forward * dirSign;

            // Guard: LookRotation requires a non-zero forward vector.
            // If hole.forward is zero (e.g. during initialisation or a degenerate state)
            // skip this frame rather than spamming "Look rotation viewing vector is zero".
            if (direction == Vector3.zero) return;

            transform.rotation = Quaternion.LookRotation(-direction, hole.rotation * Vector3.up);
            transform.position  = hole.position + (direction * (hole.depth / 2));
        }
    }
}
