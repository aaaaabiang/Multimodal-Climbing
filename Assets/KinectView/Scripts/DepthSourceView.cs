// --- START OF FILE DepthSourceView.cs ---
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEditor; // 引入 UnityEditor 命名空间，保存 Asset 需要用到

public class DepthSourceView : MonoBehaviour
{
    public GameObject MultiSourceManager;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;

    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Indices;

    private const int _DownsampleSize = 4;
    private const int _Speed = 50;

    private MultiSourceManager _MultiManager;

    private bool _pointCloudGenerated = false;

    // 新增：用于保存点云的按钮
    public bool SavePointCloud = false; // 在Inspector中勾选即可保存一次

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

            if (MultiSourceManager != null)
            {
                _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
            }

            StartCoroutine(DelayedGeneratePointCloud());
        }
        else
        {
            Debug.LogError("Kinect Sensor not found!");
        }
    }

    IEnumerator DelayedGeneratePointCloud()
    {
        while (!_Sensor.IsOpen)
        {
            Debug.Log("DelayedGeneratePointCloud: Waiting for Kinect sensor to open...");
            yield return null;
        }

        while (_MultiManager == null)
        {
            Debug.Log("DelayedGeneratePointCloud: Waiting for MultiSourceManager component...");
            yield return null;
            if (MultiSourceManager != null)
            {
                _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
            }
        }

        while (!_MultiManager.HasValidDepthData())
        {
            Debug.Log("DelayedGeneratePointCloud: Waiting for MultiSourceManager to acquire valid depth data...");
            yield return null;
        }

        while (_Sensor.DepthFrameSource.FrameDescription == null)
        {
            Debug.Log("DelayedGeneratePointCloud: Waiting for DepthFrameSource.FrameDescription...");
            yield return null;
        }

        GenerateStaticPointCloud();
    }

    void GenerateStaticPointCloud()
    {
        if (_pointCloudGenerated) return; // 已经生成过，不再重复生成

        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;
        ushort[] depthData = _MultiManager.GetDepthData();

        if (depthData == null || depthData.Length == 0)
        {
            Debug.LogError("GenerateStaticPointCloud: Failed to get depth data from MultiSourceManager. depthData is null or empty.");
            return;
        }
        Debug.Log($"GenerateStaticPointCloud (from MultiSourceManager): Depth data length: {depthData.Length}, first value: {depthData[0]}");

        // 注意：这里不再需要 _DownsampleSize，因为点云通常就是每个像素一个点
        // 但如果点太多，为了性能你可以继续使用 _DownsampleSize

        List<Vector3> pointList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>(); // 虽然静态点云可能不需要UV，但为了通用性保留

        CameraSpacePoint[] cameraSpacePoints = new CameraSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToCameraSpace(depthData, cameraSpacePoints);

        // 如果需要颜色纹理，MapDepthFrameToColorSpace 才有意义
        // 如果点云只是为了形状，可以省略以下两行和UVs的计算
        ColorSpacePoint[] colorSpacePoints = new ColorSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpacePoints);

        float colorWidth = _Sensor.ColorFrameSource.FrameDescription.Width;
        float colorHeight = _Sensor.ColorFrameSource.FrameDescription.Height;


        int validPointsCountCheck = 0;
        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize) // 可以调整 _DownsampleSize
        {
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize) // 可以调整 _DownsampleSize
            {
                int fullIndex = (y * frameDesc.Width) + x;

                if (!float.IsNegativeInfinity(cameraSpacePoints[fullIndex].X) &&
                    !float.IsNegativeInfinity(cameraSpacePoints[fullIndex].Y) &&
                    !float.IsNegativeInfinity(cameraSpacePoints[fullIndex].Z))
                {
                    Vector3 unityPoint = new Vector3(
                        cameraSpacePoints[fullIndex].X * 10,
                        cameraSpacePoints[fullIndex].Y * 10,
                        cameraSpacePoints[fullIndex].Z * 10
                    );

                    float kinectZ = cameraSpacePoints[fullIndex].Z;
                    if (kinectZ > 0.4f && kinectZ < 5.0f)
                    {
                        pointList.Add(unityPoint);
                        validPointsCountCheck++;

                        var colorSpacePoint = colorSpacePoints[fullIndex];
                        if (!float.IsNegativeInfinity(colorSpacePoint.X) && !float.IsNegativeInfinity(colorSpacePoint.Y) &&
                            colorSpacePoint.X >= 0 && colorSpacePoint.X < colorWidth &&
                            colorSpacePoint.Y >= 0 && colorSpacePoint.Y < colorHeight)
                        {
                            uvList.Add(new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight));
                        }
                        else
                        {
                            uvList.Add(Vector2.zero);
                        }
                    }
                }
            }
        }

        if (validPointsCountCheck == 0)
        {
            Debug.LogError("GenerateStaticPointCloud: No valid points found after mapping depth data to camera space and filtering. Check Kinect placement and ensure objects are in range.");
            _pointCloudGenerated = false;
            return;
        }

        CreatePointCloudMesh(pointList.ToArray(), uvList.ToArray());
        _pointCloudGenerated = true;
        Debug.Log("Static point cloud generated with " + pointList.Count + " points.");

        // 如果在生成后立即需要保存
        if (SavePointCloud)
        {
            SaveCurrentMeshAsAsset();
            SavePointCloud = false; // 保存一次后自动取消勾选
        }
    }


    void CreatePointCloudMesh(Vector3[] points, Vector2[] uvs)
    {
        _Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = points;
        _UV = uvs;

        _Indices = new int[_Vertices.Length];
        for (int i = 0; i < _Vertices.Length; i++)
        {
            _Indices[i] = i;
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.SetIndices(_Indices, MeshTopology.Points, 0); // MeshTopology.Points 渲染为点

        // 如果需要，也可以计算法线和切线，虽然点云通常不需要
        //_Mesh.RecalculateNormals(); 
        _Mesh.RecalculateBounds();
    }

    // 新增方法：保存当前的Mesh为Asset
    private void SaveCurrentMeshAsAsset()
    {
        if (_Mesh == null)
        {
            Debug.LogWarning("No mesh generated to save.");
            return;
        }

        // 确保在Assets文件夹下有一个目录来保存
        string folderPath = "Assets/SavedPointClouds";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "SavedPointClouds");
        }

        // 生成一个唯一的文件名
        string fileName = $"PointCloud_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
        string fullPath = $"{folderPath}/{fileName}";

        AssetDatabase.CreateAsset(_Mesh, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Point Cloud Mesh saved successfully to: {fullPath}");
    }


    void OnGUI()
    {
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        GUI.TextField(new Rect(Screen.width - 250, 10, 250, 20), "Static Point Cloud");
        string status = _pointCloudGenerated ? $"Static Point Cloud (Points: {_Vertices.Length})" : "Waiting for Depth Data...";
        GUI.TextField(new Rect(Screen.width - 250, 30, 250, 20), status);
        GUI.EndGroup();
    }

    void Update()
    {
        if (_Sensor == null || !_pointCloudGenerated)
        {
            return;
        }

        float yVal = Input.GetAxis("Horizontal");
        float xVal = -Input.GetAxis("Vertical");

        transform.Rotate(
            (xVal * Time.deltaTime * _Speed),
            (yVal * Time.deltaTime * _Speed),
            0,
            Space.Self);

        // 可以在这里添加一个按键来触发保存，或者使用 Inspector 上的 SavePointCloud 变量
        // if (Input.GetKeyDown(KeyCode.S)) 
        // {
        //     SaveCurrentMeshAsAsset();
        // }

        // 如果您在Inspector中勾选了SavePointCloud，这里触发保存
        if (SavePointCloud)
        {
            SaveCurrentMeshAsAsset();
            SavePointCloud = false; // 保存一次后自动取消勾选
        }
    }

    void OnApplicationQuit()
    {
        if (_Mapper != null)
        {
            _Mapper = null;
        }
        // Kinect传感器由MultiSourceManager管理关闭
    }
}
// --- END OF FILE DepthSourceView.cs ---