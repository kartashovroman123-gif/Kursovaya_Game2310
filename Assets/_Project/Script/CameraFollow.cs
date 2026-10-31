using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Transform target;

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            target = p.transform;
        }
        transform.position = new Vector3(target.position.x, target.position.y, -10f);
    }
}