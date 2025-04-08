using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using Joint = Windows.Kinect.Joint;
using Windows.Kinect;
using System.Security;

public class BodySourceView : MonoBehaviour  
{
    public BodySourceManager mBodySourceManager;
    public GameObject mJointObject;
    //public Material BoneMaterial;
    //public GameObject BodySourceManager;

    private Dictionary<ulong, GameObject> mBodies = new Dictionary<ulong, GameObject>();
    //private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    //private BodySourceManager _BodyManager;

    private List<JointType> _joints = new List<JointType>
    {
        JointType.HandLeft,
        JointType.HandRight,
        JointType.Head,
    };

    //private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    //{
    //    { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
    //    { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
    //    { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
    //    { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
    //    { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
    //    { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
    //    { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
    //    { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
    //    { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
    //    { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
    //    { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
    //    { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
    //    { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
    //    { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
    //    { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
    //    { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
    //    { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
    //    { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
    //    { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
    //    { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
    //    { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
    //    { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
    //    { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
    //    { Kinect.JointType.Neck, Kinect.JointType.Head },
    //}; 
    
    void Update () 
    {
        #region Get Kinect data
        Body[] data = mBodySourceManager.GetData();
        if (data == null)
            return;

        List<ulong> trackedIds = new List<ulong>();
        foreach (var body in data)
        {
            if (data == null)
                continue;

            if (body.IsTracked)
                trackedIds.Add(body.TrackingId);
        }
        #endregion
        //if (BodySourceManager == null)
        //{
        //    return;
        //}

        //_BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        //if (_BodyManager == null)
        //{
        //    return;
        //}

        //Kinect.Body[] data = _BodyManager.GetData();
        //if (data == null)
        //{
        //    return;
        //}

        //List<ulong> trackedIds = new List<ulong>();
        //foreach (var body in data)
        //{
        //    if (body == null)
        //    {
        //        continue;
        //    }

        //    if (body.IsTracked)
        //    {
        //        trackedIds.Add(body.TrackingId);
        //    }
        //}

        List<ulong> knownIds = new List<ulong>(mBodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(mBodies[trackingId]);
                mBodies.Remove(trackingId);
            }
        }

        foreach (var body in data)

        {//if no body, skip
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                //if body isn't tracked, create body
                if (!mBodies.ContainsKey(body.TrackingId))
                {
                    mBodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }

                //update positions
                UpdateBodyObject(body, mBodies[body.TrackingId]);
            }
        }
    }
    
    private GameObject CreateBodyObject(ulong id)
    {
        //create body parent
        GameObject body = new GameObject("Body:" + id);
        
        //create joints
        foreach (JointType joint in _joints)
        {
            //create object
            GameObject newJoint = Instantiate(mJointObject);
            newJoint.name = joint.ToString();

            //parent to body
            newJoint.transform.parent = body.transform;
            //GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            //LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            //lr.SetVertexCount(2);
            //lr.material = BoneMaterial;
            //lr.SetWidth(0.05f, 0.05f);
            
            //jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            //jointObj.name = jt.ToString();
            //jointObj.transform.parent = body.transform;
        }
        
        return body;
    }


    private void UpdateBodyObject(Body body, GameObject bodyObject)
    //private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        //update jpints
        foreach (JointType _joint in _joints)
        {
            Joint sourceJoint = body.Joints[_joint];
            Vector3 targetPosition = GetVector3FromJoint(sourceJoint);
//            targetPosition.z = 0;

            //get joint,set nwe position
            Transform jointObject = bodyObject.transform.Find(_joint.ToString());
            jointObject.position = targetPosition;
        }
    //    for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
    //    {
    //        Kinect.Joint sourceJoint = body.Joints[jt];
    //        Kinect.Joint? targetJoint = null;
            
    //        if(_BoneMap.ContainsKey(jt))
    //        {
    //            targetJoint = body.Joints[_BoneMap[jt]];
    //        }
            
    //        Transform jointObj = bodyObject.transform.Find(jt.ToString());
    //        jointObj.localPosition = GetVector3FromJoint(sourceJoint);
            
    //        LineRenderer lr = jointObj.GetComponent<LineRenderer>();
    //        if(targetJoint.HasValue)
    //        {
    //            lr.SetPosition(0, jointObj.localPosition);
    //            lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
    //            lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
    //        }
    //        else
    //        {
    //            lr.enabled = false;
    //        }
    //    }
    }

//private static Color GetColorForState(Kinect.TrackingState state)
//{
//    switch (state)
//    {
//    case Kinect.TrackingState.Tracked:
//        return Color.green;

//    case Kinect.TrackingState.Inferred:
//        return Color.red;

//    default:
//        return Color.black;
//    }
//}

private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }
}
