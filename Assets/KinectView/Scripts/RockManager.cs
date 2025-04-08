using UnityEngine;
using System.Collections.Generic;
using Windows.Kinect;

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
        if (bodySourceManager == null) return;

        Body[] bodies = bodySourceManager.GetData();
        if (bodies == null) return; //监测骨点数据，若无则直接退出

        foreach (var body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                // 获取左右手以及头部的坐标
                Vector3 leftHand = GetJointPosition(body, JointType.HandLeft);
                Vector3 rightHand = GetJointPosition(body, JointType.HandRight);
                Vector3 head = GetJointPosition(body, JointType.Head);
                // 发送坐标给当前 Rock 进行碰撞检测
                CheckRocksCollision(leftHand,rightHand,head);
            }
        }
    }

    private void CheckRocksCollision(Vector3 leftHandPosition, Vector3 rightHandPosition, Vector3 HeadPosition)
    {
        if (currentTriggerIndex >= rocks.Count) { feedbackManager.StopBeepLoop(); return; } // 所有点位已触发则不继续进行判断
        RockCollider currentRock = rocks[currentTriggerIndex];
        if (currentRock == null) { Debug.Log("Collision don't detected!"); return; }
        Vector3 currentRockPosition = currentRock.transform.position;
        if (currentRock.CheckCollision(leftHandPosition) || currentRock.CheckCollision(rightHandPosition)) // 检测碰撞
        {
            currentTriggerIndex++; // 移动到下一个 Rock
        }
        else
        {
            //feedbackManager.TempoFeedback(leftHandPosition,rightHandPosition,HeadPosition,currentRockPosition);    //给出节奏反馈
            feedbackManager.VolumeFeedback(leftHandPosition,rightHandPosition,HeadPosition, currentRockPosition);   //给出音量反馈
            feedbackManager.PanFeedback(HeadPosition,currentRockPosition);
        }
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

    // 调试用：在 Unity 场景中可视化手部位置
    private void OnDrawGizmos()
    {
        if (bodySourceManager == null) return;

        Body[] bodies = bodySourceManager.GetData();
        if (bodies == null) return;

        foreach (var body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                Vector3 leftHand = GetJointPosition(body, JointType.HandLeft);
                Vector3 rightHand = GetJointPosition(body, JointType.HandRight);
                Vector3 head = GetJointPosition(body, JointType.Head);

                // 在 Unity 场景中绘制手部位置
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(leftHand, 1f);    //左手红球
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(rightHand, 1f);   //右手蓝球
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(head, 1f);        //头白球
            }
        }
    }
}
