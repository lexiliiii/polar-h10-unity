using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Takes a transform, and moves it along a path made of bezier knots
/// </summary>
public class PathMover : MonoBehaviour
{
    [SerializeField] private InstructorAnim animatorInstructor; // TODO: refactor if necessary

    public void MoveAlongPath(Transform mover, List<BezierKnot> path, float moveSpeed) {
        StartCoroutine(MovePathCoroutine(mover, path, moveSpeed));
    }

    private IEnumerator MovePathCoroutine(Transform mover, List<BezierKnot> path, float moveSpeed) {
        animatorInstructor.StartWalking();

        foreach (BezierKnot b in path) {
            // look towards next point in path
            Vector3 lookPosition = b.Position;
            lookPosition.y = mover.position.y;
            mover.LookAt(lookPosition);

            // move towards current point
            while (Vector3.Distance(mover.position, b.Position) > 0.05f) {
                mover.position = Vector3.MoveTowards(mover.position, b.Position, moveSpeed * Time.deltaTime);
                yield return null;
            }
            mover.position = b.Position;    // ensure snap at position
            yield return null;
        }

        animatorInstructor.StopWalking();
    }
}
