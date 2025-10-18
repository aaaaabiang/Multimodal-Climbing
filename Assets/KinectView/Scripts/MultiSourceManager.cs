// --- START OF FILE MultiSourceManager.cs ---
using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class MultiSourceManager : MonoBehaviour
{
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    private KinectSensor _Sensor;
    private MultiSourceFrameReader _Reader;
    private Texture2D _ColorTexture;
    private ushort[] _DepthData;
    private byte[] _ColorData;

    // 添加一个标志，表示是否已成功获取到一帧包含非零深度的帧
    private bool _hasValidDepthFrame = false;

    public Texture2D GetColorTexture()
    {
        return _ColorTexture;
    }

    public ushort[] GetDepthData()
    {
        return _DepthData;
    }

    // 新增方法：检查是否有有效的深度数据
    public bool HasValidDepthData()
    {
        return _hasValidDepthFrame;
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            var colorFrameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = colorFrameDesc.Width;
            ColorHeight = colorFrameDesc.Height;

            _ColorTexture = new Texture2D(colorFrameDesc.Width, colorFrameDesc.Height, TextureFormat.RGBA32, false);
            _ColorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];

            var depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            _DepthData = new ushort[depthFrameDesc.LengthInPixels];

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
                Debug.Log("Kinect sensor opened by MultiSourceManager.");
            }
        }
    }

    void Update()
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                var colorFrame = frame.ColorFrameReference.AcquireFrame();
                if (colorFrame != null)
                {
                    var depthFrame = frame.DepthFrameReference.AcquireFrame();
                    if (depthFrame != null)
                    {
                        colorFrame.CopyConvertedFrameDataToArray(_ColorData, ColorImageFormat.Rgba);
                        _ColorTexture.LoadRawTextureData(_ColorData);
                        _ColorTexture.Apply();

                        depthFrame.CopyFrameDataToArray(_DepthData);

                        // 检查深度数据是否有效
                        bool currentFrameHasValidDepth = false;
                        for (int i = 0; i < _DepthData.Length; i++)
                        {
                            if (_DepthData[i] > 0) // 0 表示无效深度，通常Kinect的有效深度是几百到几千毫米
                            {
                                currentFrameHasValidDepth = true;
                                break;
                            }
                        }
                        if (currentFrameHasValidDepth && !_hasValidDepthFrame)
                        {
                            _hasValidDepthFrame = true;
                            Debug.Log("MultiSourceManager: Successfully acquired first frame with valid depth data!");
                        }
                        else if (!currentFrameHasValidDepth && _hasValidDepthFrame)
                        {
                            // 仅当之前有有效深度现在没有时才警告
                            // Debug.LogWarning("MultiSourceManager: Lost valid depth data.");
                        }
                        else if (!currentFrameHasValidDepth && !_hasValidDepthFrame && Time.frameCount % 60 == 0) // 每隔60帧打印一次，避免刷屏
                        {
                            // Debug.Log("MultiSourceManager: Still waiting for valid depth data...");
                        }

                        depthFrame.Dispose();
                        depthFrame = null;
                    }

                    colorFrame.Dispose();
                    colorFrame = null;
                }

                frame = null;
            }
        }
    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
                Debug.Log("Kinect sensor closed by MultiSourceManager.");
            }

            _Sensor = null;
        }
    }
}
// --- END OF FILE MultiSourceManager.cs ---