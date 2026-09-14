using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera mainCam;

    void Start()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            Debug.LogWarning("Camera not assigned");
        }
        
    }


    void LateUpdate()
    {
        if (mainCam != null)
        {
            transform.LookAt(mainCam.transform.position);
            //Rotate 180 degrees due to reverse Z axis
            transform.Rotate(new Vector3(0, 180, 0));
        }
    }
}
