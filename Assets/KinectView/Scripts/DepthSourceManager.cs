using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class DepthSourceManager : MonoBehaviour
{
    private KinectSensor _Sensor;
    private DepthFrameReader _Reader;
    private ushort[] _Data;
    private FrameDescription _FrameDescription; // 添加这一行

    public ushort[] GetData()
    {
        return _Data;
    }

    // 添加这个方法来获取帧描述信息
    public FrameDescription GetFrameDescription()
    {
        return _FrameDescription;
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.DepthFrameSource.OpenReader();
            _FrameDescription = _Sensor.DepthFrameSource.FrameDescription; // 初始化帧描述
            _Data = new ushort[_FrameDescription.LengthInPixels]; // 使用帧描述的长度

            if (!_Sensor.IsOpen) // 确保传感器已打开
            {
                _Sensor.Open();
            }

            // ！！！添加这个检查！！！
            if (_Sensor.IsOpen)
            {
                Debug.Log("Kinect sensor successfully opened.");
            }
            else
            {
                Debug.LogError("Failed to open Kinect sensor!");
            }
        }
    }

    void Update()
    {
        // 保持不变，DepthSourceView 会决定何时从这里获取数据
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                frame.CopyFrameDataToArray(_Data);

                // ！！！添加这一行调试代码！！！
                // 检查深度数据是否有效，如果_Data[0]一直为0，则说明没有深度信息
                // 或者检查_Data中是否有非零值
                if (_Data != null && _Data.Length > 0 && _Data[0] > 0)
                {
                    Debug.Log("Depth data available: " + _Data[0]);
                }
                else if (_Data != null && _Data.Length > 0 && _Data[0] == 0)
                {
                    Debug.LogWarning("Depth data[0] is 0, possibly no depth detected or sensor issue.");
                }
                frame.Dispose();
                frame = null;
            }
        }
    }

    void OnApplicationQuit()
    {
        // 保持不变，但传感器不在这里关闭，因为它可能被其他组件使用
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        // 重要的是：不在这里关闭_Sensor
        // if (_Sensor != null)
        // {
        //     if (_Sensor.IsOpen)
        //     // {
        //         _Sensor.Close(); // 移除或注释掉这一行，避免关闭Kinect
        //     // }

        //     _Sensor = null;
        // }
    }
}