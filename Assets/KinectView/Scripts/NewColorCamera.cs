using UnityEngine;
using Windows.Kinect;

public class NewColorCamera : MonoBehaviour
{
    private KinectSensor kinectSensor;         // Kinect 传感器
    private ColorFrameReader colorFrameReader; // 彩色帧读取器
    private Texture2D colorTexture;           // 用于显示彩色数据的纹理
    private byte[] colorData;                 // 彩色数据缓冲区

    public Renderer targetRenderer;           // 显示彩色数据的目标对象（材质）

    void Start()
    {
        // 初始化 Kinect 传感器
        kinectSensor = KinectSensor.GetDefault();

        if (kinectSensor != null)
        {
            // 打开彩色帧读取器
            colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();

            // 获取彩色帧分辨率
            int width = kinectSensor.ColorFrameSource.FrameDescription.Width;
            int height = kinectSensor.ColorFrameSource.FrameDescription.Height;

            // 初始化纹理和数据缓冲区
            colorTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            colorData = new byte[width * height * 4];

            // 打开传感器
            kinectSensor.Open();
        }
        else
        {
            Debug.LogError("Kinect sensor not found!");
        }
    }

    void Update()
    {
        if (colorFrameReader == null) return;

        // 获取最新彩色帧
        var frame = colorFrameReader.AcquireLatestFrame();
        if (frame != null)
        {
            // 将彩色帧数据拷贝到缓冲区
            frame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);

            // 水平翻转数据
            FlipColorDataHorizontally();

            // 加载数据到纹理
            colorTexture.LoadRawTextureData(colorData);
            colorTexture.Apply();

            // 将纹理设置到材质上
            if (targetRenderer != null)
            {
                targetRenderer.material.mainTexture = colorTexture;
            }

            frame.Dispose();
        }
    }

    // 水平翻转彩色数据
    private void FlipColorDataHorizontally()
    {
        int width = colorTexture.width;
        int height = colorTexture.height;
        int bytesPerPixel = 4; // 每个像素的字节数 (BGRA32)

        byte[] flippedData = new byte[colorData.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sourceIndex = (y * width + x) * bytesPerPixel;
                int flippedX = width - 1 - x; // 水平翻转的 X 坐标
                int targetIndex = (y * width + flippedX) * bytesPerPixel;

                // 将原始数据拷贝到翻转后的位置
                flippedData[targetIndex] = colorData[sourceIndex];       // B
                flippedData[targetIndex + 1] = colorData[sourceIndex + 1]; // G
                flippedData[targetIndex + 2] = colorData[sourceIndex + 2]; // R
                flippedData[targetIndex + 3] = colorData[sourceIndex + 3]; // A
            }
        }

        // 将翻转后的数据拷贝回原始数据数组
        System.Buffer.BlockCopy(flippedData, 0, colorData, 0, colorData.Length);
    }

    void OnApplicationQuit()
    {
        if (colorFrameReader != null)
        {
            colorFrameReader.Dispose();
        }
        if (kinectSensor != null)
        {
            kinectSensor.Close();
        }
    }
}
