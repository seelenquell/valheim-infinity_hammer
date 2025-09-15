using System;
using System.Collections.Generic;
using System.Linq;
using Argo.Blueprint;
using UnityEngine;

namespace InfinityHammer;


[Serializable]
public class BlueprintObject(
    string name,
    Vector3 pos,
    Quaternion rot,
    Vector3 scale,
    string info,
    string data,
    float chance) 
{
    public string prefab = name;
    public Vector3 pos = pos;
    public Quaternion rot = rot.normalized;
    public string data = data;
    public Vector3 scale = scale;
    public float chance = chance;
    public string extraInfo = info;

    public virtual string Prefab { get => prefab; set => prefab = value; }
    public virtual Vector3 Pos { get => pos; set => pos = value; }
    public virtual Quaternion Rot { get => rot; set => rot = value; }
    public virtual string Data { get => data; set => data = value; }
    public virtual Vector3 Scale { get => scale; set => scale = value; }
    public virtual float Chance { get => chance; set => chance = value; }
    public virtual string ExtraInfo { get => extraInfo; set => extraInfo = value; }
}


public class Blueprint 
{

  public string Name = "";
  public string Description = "";
  public string Creator = "";
  public string Category = "";
  public Vector3 Coordinates = Vector3.zero;
  public Vector3 Rotation = Vector3.zero;
  public string CenterPiece = Configuration.BlueprintCenterPiece;
  public List<BlueprintObject> Objects = [];
  public List<Vector3> SnapPoints = [];
  public float Radius = 0f;
 

    public Vector3 Center(string centerPiece)
    {
        if (centerPiece != "")
            CenterPiece = centerPiece;
        Bounds bounds = new();
        var y = float.MaxValue;
        Quaternion rot = Quaternion.identity;
        foreach (var obj in Objects)
        {
            y = Mathf.Min(y, obj.Pos.y);
            bounds.Encapsulate(obj.Pos);
        }

        Vector3 center = new(bounds.center.x, y, bounds.center.z);
        foreach (var obj in Objects)
        {
            if (obj.Prefab == CenterPiece)
            {
                center = obj.Pos;
                rot = Quaternion.Inverse(obj.Rot);
                break;
            }
        }

        Radius = Utils.LengthXZ(bounds.extents);
        foreach (var obj in Objects)
            obj.Pos -= center;
        SnapPoints = SnapPoints.Select(p => p - center).ToList();
        if (rot != Quaternion.identity)
        {
            foreach (var obj in Objects)
            {
                obj.Pos = rot * obj.Pos;
                obj.Rot = rot * obj.Rot;
            }

            SnapPoints = SnapPoints.Select(p => rot * p).ToList();
        }

        return center;
    }
}