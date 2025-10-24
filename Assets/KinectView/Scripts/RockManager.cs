using UnityEngine;
using System.Collections.Generic;
using Windows.Kinect;
using System.Collections;

public class RockManager : MonoBehaviour
{
    public FeedbackManager feedbackManager;
    public BodySourceManager bodySourceManager; // Kinect 数据管理器引用
    public List<RockCollider> rocks;            // 所有 Rock 对象的列表
    private int currentTriggerIndex = 0;        // 当前需要触发的 Rock 索引

    private const float kinectToUnityScaleFactor = 10.0f; // 缩放因子
    private Vector3 kinectOffset = new Vector3(0, 0, 0);  // 偏移校准值
    private bool useMirroring = false;                    // 是否使用镜像处理

    void Update()
    {
        // 如果所有 Rock 都被消除了，则停止反馈并退出
        if (currentTriggerIndex >= rocks.Count)
        {
            feedbackManager.StopBeepLoop();
            return;
        }

        if (bodySourceManager == null) return;

        Body[] bodies = bodySourceManager.GetData();
        if (bodies == null)
        {
            // If no body data is available, stop the beep loop.
            feedbackManager.StopBeepLoop();
            return; //监测骨点数据，若无则直接退出
        }
        // Check if there's at least one tracked body.
        bool anyBodyTracked = false;
        foreach (var body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                anyBodyTracked = true;
                break;
            }
        }
        // If no bodies are tracked, stop the feedback and return.
        if (!anyBodyTracked)
        {
            feedbackManager.StopBeepLoop();
            return;
        }


        foreach (var body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                // 获取左右脚以及头部的坐标
                Vector3 leftFoot = GetJointPosition(body, JointType.FootLeft);
                Vector3 rightFoot = GetJointPosition(body, JointType.FootRight);
                Vector3 head = GetJointPosition(body, JointType.Head);

                if (!isHandlingCollision) // 只有在没有处理碰撞时才给反馈
                {
                    RockCollider currentRock = rocks[currentTriggerIndex];
                    if (currentRock != null && currentRock.gameObject != null)
                    {
                        Vector3 currentRockPosition = currentRock.transform.position;
                        // 即使没有碰撞，也需要持续更新反馈，而不是只在 Update 周期末尾
                        feedbackManager.TempoFeedback(leftFoot, rightFoot, head, currentRockPosition);
                        feedbackManager.PanFeedback(leftFoot, rightFoot, head, currentRockPosition);
                        feedbackManager.PitchFeedback(leftFoot, rightFoot, currentRockPosition);
                    }
                }
                // 发送坐标给当前 Rock 进行碰撞检测
                CheckRocksCollision(leftFoot, rightFoot, head);
            }
        }
    }

    private bool isHandlingCollision = false; // 新增标记，防止在协程运行时 Update 再次触发常规反馈

    private void CheckRocksCollision(Vector3 leftFootPosition, Vector3 rightFootPosition, Vector3 HeadPosition)
    {
        // 再次检查，防止在 Update 循环中多帧处理，导致越界
        if (currentTriggerIndex >= rocks.Count) return;

        RockCollider currentRock = rocks[currentTriggerIndex];
        if (currentRock == null || currentRock.gameObject == null || isHandlingCollision) 
        {
            // 如果 currentRock 已经为 null，可能是因为上一帧被销毁了，此时直接尝试处理下一个
            //currentTriggerIndex++;
            return; }
        Vector3 currentRockPosition = currentRock.transform.position;
        if (currentRock.CheckCollision(leftFootPosition) || currentRock.CheckCollision(rightFootPosition)) // 检测碰撞
        {
            feedbackManager.StopBeepLoop(); // 1. 停止滴滴反馈音 (在 RockCollider 播放碰撞音之前)

            // 标记正在处理碰撞
            isHandlingCollision = true; 
            // 延时销毁，确保碰撞音播放完毕
            StartCoroutine(HandleCollisionAndNextRock(currentRock, leftFootPosition, rightFootPosition, HeadPosition));
        }
        //else
        //{
        //    feedbackManager.TempoFeedback(leftFootPosition, rightFootPosition, HeadPosition, currentRockPosition);    //给出节奏反馈
        //    //feedbackManager.VolumeFeedback(leftfootPosition,rightfootPosition,HeadPosition, currentRockPosition);   //给出音量反馈
        //    feedbackManager.PanFeedback(HeadPosition, currentRockPosition);
        //}
    }
    // 新增协程来处理碰撞后的逻辑
    private IEnumerator HandleCollisionAndNextRock(RockCollider collidedRock, Vector3 leftFootPosition, Vector3 rightFootPosition, Vector3 HeadPosition)
    {
        // 确保碰撞音有足够时间播放
        // 你可能需要调整这个延迟时间，取决于你的碰撞音的长度
        yield return new WaitForSeconds(collidedRock.audioSource != null && collidedRock.audioSource.clip != null ? collidedRock.audioSource.clip.length : 0.5f);

        // 如果碰撞的 Rock 仍然存在 (以防万一)
        if (collidedRock != null && collidedRock.gameObject != null)
        {
            Destroy(collidedRock.gameObject); // 销毁当前的 Rock 对象
        }

        currentTriggerIndex++; // 移动到下一个 Rock

        // 2. 检查是否还有下一个 Rock
        if (currentTriggerIndex < rocks.Count)
        {
            RockCollider nextRock = rocks[currentTriggerIndex];
            if (nextRock != null && nextRock.gameObject != null)
            {
                // 3. 根据下一个 Rock 的距离继续播放滴滴反馈音
                // 这里我们立即触发一次反馈，确保新岩石的反馈立即开始
                Vector3 nextRockPosition = nextRock.transform.position;
                feedbackManager.TempoFeedback(leftFootPosition, rightFootPosition, HeadPosition, nextRockPosition);
                feedbackManager.PanFeedback(leftFootPosition, rightFootPosition, HeadPosition, nextRockPosition);
                feedbackManager.PitchFeedback(leftFootPosition, rightFootPosition, nextRockPosition);
            }
        }
        else
        {
            // 如果没有下一个 Rock 了，确保停止所有反馈
            feedbackManager.StopBeepLoop();
        }

        isHandlingCollision = false; // 碰撞处理完毕，重置标记，Update 将恢复持续反馈
    }
    // 获取 Kinect 关节的世界坐标
    private Vector3 GetJointPosition(Body body, JointType jointType)
    {
        CameraSpacePoint jointPosition = body.Joints[jointType].Position;

        // 转换为 Unity 世界坐标
        Vector3 unityPosition = new Vector3(
            jointPosition.X * kinectToUnityScaleFactor,
            jointPosition.Y * kinectToUnityScaleFactor,
            jointPosition.Z * kinectToUnityScaleFactor
        );

        // 可选：处理镜像
        if (useMirroring)
        {
            unityPosition.x = -unityPosition.x;
        }

        // 添加偏移校准
        return unityPosition + kinectOffset;
    }

    // 调试用：在 Unity 场景中可视化脚部位置
    private void OnDrawGizmos()
    {
        if (bodySourceManager == null) return;

        Body[] bodies = bodySourceManager.GetData();
        if (bodies == null) return;

        foreach (var body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                Vector3 leftFoot = GetJointPosition(body, JointType.FootLeft);
                Vector3 rightFoot = GetJointPosition(body, JointType.FootRight);
                Vector3 head = GetJointPosition(body, JointType.Head);

                // 在 Unity 场景中绘制手部位置
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(leftFoot, 1f);    //左手红球
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(rightFoot, 1f);   //右手蓝球
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(head, 1f);        //头白球
            }
        }
    }
}
