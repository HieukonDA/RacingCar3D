using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float moveSmoothness = 0.3f;
    public float rotationSmoothness = 5f;

    public Vector3 moveOffset;
    public Vector3 rotationOffset;
    public Transform carTarget;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (carTarget == null) return; // Kiểm tra null để tránh lỗi
        FollowTarget();
    }

    void FollowTarget()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        // Tính toán vị trí đích và áp dụng mượt mà
        Vector3 targetPos = carTarget.TransformPoint(moveOffset);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, moveSmoothness);
    }

    void HandleRotation()
    {
        // Xác định hướng nhìn từ camera tới xe
        Vector3 direction = carTarget.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Áp dụng offset cho hướng nhìn
        targetRotation *= Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
    }
}
