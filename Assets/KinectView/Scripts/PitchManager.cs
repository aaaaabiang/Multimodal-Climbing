using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchManager : MonoBehaviour
{
    public AudioSource audioSource;
    public Transform hand;
    public Transform target;

    void Update()
    {
        if (audioSource == null || hand == null || target == null) return;

        // 根据手与目标的距离动态调整音高
        float distance = Vector3.Distance(hand.position, target.position);

        // 将距离映射为音高（根据需要调整范围）
        audioSource.pitch = Mathf.Clamp(1.0f / (distance + 0.1f), 0.5f, 2.0f);
    }
}
