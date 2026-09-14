using UnityEngine;

public class AvatarInitializer : MonoBehaviour
{
    void Start()
    {
        var spawner = GetComponent<ExosuitSpawner>();
        if (RobotSelectionManager.Instance.HasEquippedSuit())
        {
            var equipped = RobotSelectionManager.Instance.equippedSet;
            var copiedSet = ScriptableObject.Instantiate(equipped);
            spawner.materialSet = copiedSet;
            spawner.ApplyMaterials();
            Debug.Log("Equipped Exosuit");
        } else
        {
            Debug.Log("Exosuit not equipped");
        }
    }
}
