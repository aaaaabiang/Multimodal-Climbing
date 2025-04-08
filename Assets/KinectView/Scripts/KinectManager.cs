using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

public class KinectManager : MonoBehaviour
{
    private KinectSensor kinectSensor;
    private BodyFrameReader bodyFrameReader;
    private Body[] bodies;

    // 提供给其他脚本访问的关节数据
    //public Body[] Bodies => bodies;
    public Body[] Bodies
    {
        get { return bodies; }
    }

    // Start is called before the first frame update
    void Start()
    {
        kinectSensor = KinectSensor.GetDefault();

        if (kinectSensor != null)
        {
            kinectSensor.Open();
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
        }
        else
        {
            Debug.LogError("Kinect sensor not found!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        var frame = bodyFrameReader.AcquireLatestFrame();

        if (frame != null)
        {
            frame.GetAndRefreshBodyData(bodies);
            frame.Dispose();
        }
    }
    // 在游戏结束时关闭 Kinect
    void OnApplicationQuit()
    {
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
        }

        if (kinectSensor != null)
        {
            kinectSensor.Close();
        }
    }
}
