using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    public UnityEngine.AudioSource audioSource; // 播放声音的组件
    public float tempo = 1.0f;       // 播放节奏（1.0 为默认节奏）
    private Coroutine beepCoroutine; // 用于控制循环播放的协程
    private float feedbackUpdateInterval = 0.1f; //最小反馈时间间隔
    private float lastVolFeedbackTime; //上次音量反馈时刻
    private float lastTempoFeedbackTime; //上次节奏反馈时刻
    private float lastPanFeedbackTime; //上次声相反馈时刻
    private float lastPitchFeedbackTime; // 新增：上次音高反馈时刻

    //新增声相反馈基于头/脚的选择
    public enum PanCalculationMode { HeadBased, FootBased };
    public PanCalculationMode currentPanMode = PanCalculationMode.FootBased;
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<UnityEngine.AudioSource>();  // 自动获取 AudioSource 组件
        }
    }

    public void StartBeepLoop() // 开始播放滴滴声
    {
        if (beepCoroutine != null) return; // 防止重复启动
        beepCoroutine = StartCoroutine(PlayBeep());
    }
    public void StopBeepLoop()
    {
        if (beepCoroutine != null)
        {
            StopCoroutine(beepCoroutine);
            beepCoroutine = null;
        }
    }
    public void SetTempo(float newTempo)    // 设置节奏并更新播放状态
    {
        tempo = Mathf.Max(0.1f, newTempo); // 防止节奏过慢导致错误
    }
    private IEnumerator PlayBeep()  // 播放滴滴声的协程
    {
        while (true)
        {
            if (audioSource != null)
            {
                audioSource.Play();
                float interval = 1.0f / tempo; // 计算间隔时间
                yield return new WaitForSeconds(interval);//设置停止间隔
            }
        }
    }    
    
    //public void VolumeFeedback(Vector3 leftfootPosition, Vector3 rightfootPosition, Vector3 HeadPosition, Vector3 rockPosition)
    //{
    //    if (Time.time - lastVolFeedbackTime < feedbackUpdateInterval) return;
    //    lastVolFeedbackTime = Time.time; //添加最小反馈时间间隔

    //    float MaxDistance = 15.0f; // 声音开始播放的距离
    //    float MinDistance = 0.3f; // 声音达到最大值的距离（碰撞检测半径）
    //    float exp = 3f; //指数映射参数
    //    float leftFootDistance = Vector3.Distance(leftfootPosition, rockPosition);
    //    float rightFootDistance = Vector3.Distance(rightfootPosition, rockPosition);
    //    float distance = Mathf.Min(leftFootDistance, rightFootDistance);  // 取双脚间最小的距离
    //    float NormalizedDistance = Mathf.Clamp01((MaxDistance - distance) / (MaxDistance - MinDistance));//将距离归一化为0-1之间
    //    float volume = Mathf.Pow(NormalizedDistance, exp);

    //    if (audioSource != null)
    //    {
    //        audioSource.volume = volume; // 设置音量

    //        // 如果距离进入触发范围，则播放提示音
    //        if (distance < MaxDistance)
    //        {
    //            StartBeepLoop(); //启动提示音
    //            //Debug.Log("Audio started playing at volume: " + audioSource.volume);
    //        }
    //        else
    //        {
    //            StopBeepLoop(); //停止提示音
    //        }
    //    }
    //}

    public void PanFeedback(Vector3 leftFootPosition, Vector3 rightFootPosition, Vector3 HeadPosition, Vector3 rockPosition)
    {
        float panStereo = 0f;
        if (Time.time - lastPanFeedbackTime < feedbackUpdateInterval) return;
        lastPanFeedbackTime = Time.time; //添加最小反馈时间间隔

        // ------------------ 声相反馈的内部参数 ------------------
        float maxHorizontalPanDistance = 2.0f; // 在水平方向上，距离头部多远时声相达到完全左/右
        // --------------------------------------------------------
        if (audioSource == null) return;


        if (currentPanMode == PanCalculationMode.HeadBased)
        {
            Vector3 directionToTarget = rockPosition - HeadPosition; // 计算方位差值

            // 获取水平方向上的差值
            float horizontalDifference = directionToTarget.x;

            // 将水平差值限制在设定的最大距离内
            float clampedDifference = Mathf.Clamp(horizontalDifference, -maxHorizontalPanDistance, maxHorizontalPanDistance);

            // 将限制后的差值映射到 -1.0 (左) 到 1.0 (右) 的声相范围
            // Mathf.InverseLerp(a, b, value) 将 value 从 a 到 b 的范围映射到 0 到 1 的范围
            // 然后再用 Mathf.Lerp(min, max, t) 将 0 到 1 的值映射到 -1.0 到 1.0 的声相
            panStereo = Mathf.Lerp(-1.0f, 1.0f, Mathf.InverseLerp(-maxHorizontalPanDistance, maxHorizontalPanDistance, clampedDifference));

        }
        else if (currentPanMode == PanCalculationMode.FootBased)
        {
            // 1. 获取最近脚的X轴位置作为参考
            float leftFootDistance = Vector3.Distance(leftFootPosition, rockPosition);
            float rightFootDistance = Vector3.Distance(rightFootPosition, rockPosition);
            float referenceFootX;

            if (leftFootDistance < rightFootDistance)
            {
                referenceFootX = leftFootPosition.x;
            }
            else
            {
                referenceFootX = rightFootPosition.x;
            }

            // 2. 计算岩点与最近脚的水平距离差
            float horizontalDifference = rockPosition.x - referenceFootX;
            // 3. 根据距离和高度差设置音高

            // 将水平差值限制在设定的最大距离内
            float clampedDifference = Mathf.Clamp(horizontalDifference, -maxHorizontalPanDistance, maxHorizontalPanDistance);

            // 将限制后的差值映射到 -1.0 (左) 到 1.0 (右) 的声相范围
            // Mathf.InverseLerp(a, b, value) 将 value 从 a 到 b 的范围映射到 0 到 1 的范围
            // 然后再用 Mathf.Lerp(min, max, t) 将 0 到 1 的值映射到 -1.0 到 1.0 的声相
            panStereo = Mathf.Lerp(-1.0f, 1.0f, Mathf.InverseLerp(-maxHorizontalPanDistance, maxHorizontalPanDistance, clampedDifference));

        }

        audioSource.panStereo = -panStereo;  // 设置声相
    }
    // 新增：音高反馈方法，基于二元离散和最近脚的Y轴，参数在函数内部定义
    public void PitchFeedback(Vector3 leftFootPosition, Vector3 rightFootPosition, Vector3 rockPosition)
    {
        // ------------------ 音高反馈的内部参数 ------------------
        float highPitch = 2.0f;   // 高于目标时的音高
        float lowPitch = 0.7f;    // 低于目标时的音高
        //float midPitch = 1.0f;    // 与目标持平时的音高
        // --------------------------------------------------------

        if (Time.time - lastPitchFeedbackTime < feedbackUpdateInterval) return;
        lastPitchFeedbackTime = Time.time; // 更新上次音高反馈时刻

        if (audioSource == null) return;

        // 1. 获取最近脚的Y轴位置作为参考
        float leftFootDistance = Vector3.Distance(leftFootPosition, rockPosition);
        float rightFootDistance = Vector3.Distance(rightFootPosition, rockPosition);
        float referenceFootY;

        if (leftFootDistance < rightFootDistance)
        {
            referenceFootY = leftFootPosition.y;
        }
        else
        {
            referenceFootY = rightFootPosition.y;
        }
        // 2. 计算岩点与最近脚的垂直高度差
        float relativeHeight = rockPosition.y - referenceFootY;

        // 3. 根据距离和高度差设置音高

        if (relativeHeight >= 0) // 岩点高于参考脚
        {
            audioSource.pitch = highPitch;
        }
        else if (relativeHeight < 0) // 岩点低于参考脚
        {
            audioSource.pitch = lowPitch;
        }
        //else // 岩点与参考脚持平
        //{
        //    audioSource.pitch = midPitch;
        //}
    }

    public void TempoFeedback(Vector3 leftfootPosition, Vector3 rightfootPosition, Vector3 HeadPosition, Vector3 rockPosition)
    {
        if (Time.time - lastTempoFeedbackTime < feedbackUpdateInterval) return;
        lastTempoFeedbackTime = Time.time; //添加最小反馈时间间隔

        // 定义阈值
        float minDistance = 0.01f; // 最小距离（对应最大节奏）
        float maxDistance = 15.0f; // 最大距离（对应最小节奏）
        float minTempo = 0.1f;    // 最小节奏
        float maxTempo = 7f;    // 最大节奏
        float currentTempo; // 当前节奏

        // ------------------ 节奏反馈的内部参数（根据X轴） ------------------
        // 此处沿用“离岩点X轴最近的脚”的逻辑
        float leftFootXDistanceAbs = Mathf.Abs(leftfootPosition.x - rockPosition.x);
        float rightFootXDistanceAbs = Mathf.Abs(rightfootPosition.x - rockPosition.x);
        float xDistance = Mathf.Min(leftFootXDistanceAbs, rightFootXDistanceAbs);  // 取双脚X轴绝对差值中最小的

        currentTempo = Mathf.Lerp(minTempo, maxTempo, Mathf.InverseLerp(maxDistance, minDistance, xDistance)); //线性插值计算速度

        if (audioSource != null)
        {
            SetTempo(currentTempo); //更新节奏
            // 如果距离进入触发范围，则播放提示音
            if (xDistance < maxDistance)
            {
                StartBeepLoop(); //启动提示音
                //Debug.Log("Audio started playing at Tempo: " + currentTempo);
            }
            else
            {
                StopBeepLoop(); //停止提示音
            }
        }
    }

}
