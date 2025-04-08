//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
//using Windows.Kinect;
using System.Collections;

public class RockCollider : MonoBehaviour
{
    public UnityEngine.AudioSource audioSource; // 播放声音的组件
    public float detectionRadius = 0.3f; // 碰撞检测的半径

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<UnityEngine.AudioSource>();  // 自动获取 AudioSource 组件
        }
    }

    public bool CheckCollision(Vector3 handPosition)
    {
        Collider[] hitColliders = Physics.OverlapSphere(handPosition, detectionRadius); //调取触发器信息

        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == this.gameObject)  //检测是否碰撞
            {
                // 播放声音
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                    Debug.Log("Hand collided with target object in 3D. Sound played!");
                }
                return true; // 表示已触碰到目标
            }
        }
        return false; // 没有触碰到目标
    }
}
