using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using Yarn.Saliency;
using Yarn.Unity;

/// <summary>
/// Moves the Virtual Instructor.
/// 
/// Called via Yarn Scripts
/// </summary>
public class InstructorAnim : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotateSpeed = 5f;

    [SerializeField] private PathMover pathMover;
    [SerializeField] private SplineContainer walkPath;
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    private List<BezierKnot> knotList; // TODO: def a way to use arrays here but ._ .
    private int knotIndex = 0;

    private Transform t;
    private Transform lookTarget;
    private bool isWalking = false;

    void Start() {
        t = transform;
        SetLookTarget(player);

        if (walkPath != null) { knotList = walkPath.Spline.Knots.ToList(); }
    }

    void Update() {
        if (!isWalking) {
            RotateInstructor();
        }
    }

    /// <summary>
    /// Called in Yarn scripts. Moves the Instructor to the knot point specified.
    /// </summary>
    [YarnCommand("move_instructor")]
    public void MoveVirtualInstructor(int knotIndexToMoveTo) {
        //Debug.Log("knotListLength: " + knotList.Count);
        //Debug.Log("Before Move Knot Index: " + knotIndex + "| Moving too: " + knotIndexToMoveTo);
        List<BezierKnot> path = knotList.GetRange(knotIndex, (knotIndexToMoveTo - knotIndex) + 1);
        pathMover.MoveAlongPath(t, path, moveSpeed);
        knotIndex = knotIndexToMoveTo;  // TODO: theoretically i should probably increment this in the move method in case any weird errors occur but (._ .)
        //Debug.Log("Curr Knot Index: " + knotIndex);
    }

    public void SetLookTarget(Transform target) {
       lookTarget = target;
    }

    public void StartWalking() {
        animator.SetBool("isWalking", true);
        isWalking = true; 
    }
    public void StopWalking() {
        animator.SetBool("isWalking", false);
        isWalking = false; 
    }

    private void RotateInstructor() {
        Vector3 direction = (lookTarget.position - t.position).normalized;  // calculate direction to target
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);   // calc target rotation

        t.rotation = Quaternion.Slerp(t.rotation, lookRotation, rotateSpeed * Time.deltaTime);
    }

    /// <summary>
    /// In Yarn Scripts theres certain moments I don't want the player to be able to progress dialogue until
    /// the instructor has reached a certain point
    /// </summary>
    [YarnCommand("wait_until_done")]
    public IEnumerator WaitUntilDone() {
        yield return new WaitUntil(() => isWalking == false);
    }
}
