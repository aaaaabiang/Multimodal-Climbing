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
    
    public void VolumeFeedback(Vector3 leftfootPosition, Vector3 rightfootPosition, Vector3 HeadPosition, Vector3 rockPosition)
    {
        if (Time.time - lastVolFeedbackTime < feedbackUpdateInterval) return;
        lastVolFeedbackTime = Time.time; //添加最小反馈时间间隔

        float MaxDistance = 15.0f; // 声音开始播放的距离
        float MinDistance = 0.3f; // 声音达到最大值的距离（碰撞检测半径）
        float exp = 3f; //指数映射参数
        float leftFootDistance = Vector3.Distance(leftfootPosition, rockPosition);
        float rightFootDistance = Vector3.Distance(rightfootPosition, rockPosition);
        float distance = Mathf.Min(leftFootDistance, rightFootDistance);  // 取双脚间最小的距离
        float NormalizedDistance = Mathf.Clamp01((MaxDistance - distance) / (MaxDistance - MinDistance));//将距离归一化为0-1之间
        float volume = Mathf.Pow(NormalizedDistance, exp);

        if (audioSource != null)
        {
            audioSource.volume = volume; // 设置音量

            // 如果距离进入触发范围，则播放提示音
            if (distance < MaxDistance)
            {
                StartBeepLoop(); //启动提示音
                //Debug.Log("Audio started playing at volume: " + audioSource.volume);
            }
            else
            {
                StopBeepLoop(); //停止提示音
            }
        }
    }

    public void PanFeedback(Vector3 HeadPosition, Vector3 rockPosition)
    {
        if (Time.time - lastPanFeedbackTime < feedbackUpdateInterval) return;
        lastPanFeedbackTime = Time.time; //添加最小反馈时间间隔

        float panStereo = 0f; //设置声相初始值
        Vector3 directionToTarget = rockPosition - HeadPosition; // 计算方位差值
        panStereo = directionToTarget.x < 0 ? 1.0f : -1.0f; // 判断声音方位
        if (audioSource != null)
        {
            audioSource.panStereo = panStereo;  // 设置声相
        }
    }
    // 新增：音高反馈方法，基于二元离散和最近脚的Y轴，参数在函数内部定义
    public void PitchFeedback(Vector3 leftFootPosition, Vector3 rightFootPosition, Vector3 rockPosition)
    {
        // ------------------ 音高反馈的内部参数 ------------------
        float highPitch = 1.5f;   // 高于目标时的音高
        float lowPitch = 0.7f;    // 低于目标时的音高
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

        if (relativeHeight > 0) // 岩点高于参考脚
        {
            audioSource.pitch = highPitch;
        }
        else if (relativeHeight < 0) // 岩点低于参考脚
        {
            audioSource.pitch = lowPitch;
        }
    }

    public void TempoFeedback(Vector3 leftfootPosition, Vector3 rightfootPosition, Vector3 HeadPosition, Vector3 rockPosition)
    {
        if (Time.time - lastTempoFeedbackTime < feedbackUpdateInterval) return;
        lastTempoFeedbackTime = Time.time; //添加最小反馈时间间隔

        // 定义阈值
        float minDistance = 0.3f; // 最小距离（对应最大节奏）
        float maxDistance = 15.0f; // 最大距离（对应最小节奏）
        float minTempo = 0.2f;    // 最小节奏
        float maxTempo = 4f;    // 最大节奏
        float currentTempo; // 当前节奏

        float leftFootDistance = Vector3.Distance(leftfootPosition, rockPosition);
        float rightFootDistance = Vector3.Distance(rightfootPosition, rockPosition);
        float distance = Mathf.Min(leftFootDistance, rightFootDistance);  // 取双手间最小的距离
        
        currentTempo = Mathf.Lerp(minTempo, maxTempo, Mathf.InverseLerp(maxDistance, minDistance, distance)); //线性插值计算速度

        if (audioSource != null)
        {
            SetTempo(currentTempo); //更新节奏
            // 如果距离进入触发范围，则播放提示音
            if (distance < maxDistance)
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
