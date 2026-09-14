using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackHighlight : MonoBehaviour
{
    private Material material;
    private Color color;
    // Start is called before the first frame update
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            material = renderer.material;
            color = material.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        color.a = 0;
    }
}
