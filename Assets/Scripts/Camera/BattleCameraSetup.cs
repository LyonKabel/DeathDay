using UnityEngine;

namespace HexTactics.CameraSystem
{
    public class BattleCameraSetup : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 13f, -11f);

        private void Start()
        {
            if (target == null)
            {
                Debug.LogWarning("BattleCameraSetup has no target.");
                return;
            }

            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }
    }
}