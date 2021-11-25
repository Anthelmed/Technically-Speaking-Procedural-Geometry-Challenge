using System;
using UnityEditor;
using UnityEngine;

public class SDFObject : MonoBehaviour
{
    public enum GeometryType
    {
        Sphere,
        Box,
        Cylinder
    }
    
    public enum OperationType
    {
        Union,
        Subtraction,
        Intersection
    }

    [ColorUsage(true, true)] [SerializeField] public Color color = Color.white;
    [SerializeField] public bool useColor = false;
    [SerializeField] public GeometryType geometryType;
    [SerializeField] public OperationType operationType;
    [SerializeField] public float blend = 0.01f;
    [SerializeField] public float blendColor = 0.01f;
}