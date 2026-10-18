
using UnityEngine;

public class VRPlayerColliderHeight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Transform playerOrigin;
    [SerializeField] private Transform playerEyes;

    [Header("Height Settings")]
    [SerializeField] private bool autoUpdateHeight = true;
    [SerializeField] private float defaultHeight = 1.4f;
    [SerializeField] private float heightOffset = 0f;

    private const float SellionToTopOfHead = 0.1085f;
    private const float SellionToBackOfHeadHalf = 0.0965f;

    private float _initialBottom;

    private void Awake()
    {
        if (capsule == null) capsule = GetComponent<CapsuleCollider>();

        if (capsule != null)
            _initialBottom = capsule.center.y - capsule.height * 0.5f;
    }

    private void LateUpdate()
    {
        UpdateColliderHeight();
    }

    public void UpdateColliderHeight()
    {
        if (capsule == null) return;

        float playerHeight = defaultHeight;

        if (autoUpdateHeight && playerOrigin != null && playerEyes != null)
        {
            Vector3 playerHead = playerEyes.position - playerEyes.forward * SellionToBackOfHeadHalf;
            Vector3 playerHeadTop = playerHead + Vector3.up * SellionToTopOfHead;

            playerHeight = playerHeadTop.y - playerOrigin.position.y;
        }

        float desiredHeight = Mathf.Max(playerHeight + heightOffset, capsule.radius * 2f);

        capsule.height = desiredHeight;

        // 保持 Collider 的 local 底部位置不變
        Vector3 center = capsule.center;
        center.y = _initialBottom + desiredHeight * 0.5f;
        capsule.center = center;
    }

    public void InjectReferences(CapsuleCollider collider, Transform origin, Transform eyes)
    {
        capsule = collider;
        playerOrigin = origin;
        playerEyes = eyes;

        _initialBottom = capsule.center.y - capsule.height * 0.5f;
    }
}
