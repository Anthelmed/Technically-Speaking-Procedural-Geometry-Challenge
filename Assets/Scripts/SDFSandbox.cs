using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SDFSandbox : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material material;
    [SerializeField] private int textureSize = 64;
    [SerializeField] private Transform Viewer;
    [SerializeField] private ParticleSystem particleSystem;

    [SerializeField] private SDFObject[] sdfObjects;

    private RenderTexture _renderTexture;
    private ComputeBuffer _shapesBuffer;
    private static readonly int SDFTexture = Shader.PropertyToID("_MainTex");
    
    private void OnTransformChildrenChanged()
    {
        sdfObjects = GetComponentsInChildren<SDFObject>();
    }

    private List<Shape> ShapeList()
    {
        var shapes = new List<Shape>();

        foreach (var sdfObject in sdfObjects)
        {
            var shape = new Shape(sdfObject.color, sdfObject.useColor, sdfObject.transform.position, sdfObject.transform.rotation,
                sdfObject.transform.localScale, sdfObject.blend, sdfObject.blendColor, (int)sdfObject.geometryType, (int)sdfObject.operationType);
            
            shapes.Add(shape);
        }

        var boundaries = shapes.Last();

        shapes.Remove(boundaries);
        
        var particles = new ParticleSystem.Particle[particleSystem.particleCount];

        particleSystem.GetParticles(particles);

        foreach (var particle in particles)
        {
            var shape = new Shape(Color.white, true, particle.position, Quaternion.identity, 
                particle.GetCurrentSize3D(particleSystem) * 0.01f, 0.01f, 0.001f, (int)SDFObject.GeometryType.Sphere, (int)SDFObject.OperationType.Union);
            
            shapes.Add(shape);
        }

        shapes.Add(boundaries);

        return shapes;
    }

    private void OnValidate()
    {
        _renderTexture = null;
    }

    private RenderTexture Texture()
    {
        if (_renderTexture == null)
        {
            _renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                dimension = TextureDimension.Tex3D, 
                volumeDepth = textureSize, 
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        
            _renderTexture.Create();
        }

        return _renderTexture;
    }

    private void Update()
    {
        var shapes = ShapeList();
        var texture = Texture();

        unsafe
        {
            _shapesBuffer?.Release();
            
            _shapesBuffer = new ComputeBuffer(shapes.Count, sizeof(Shape));
            _shapesBuffer.SetData(shapes);
            
            computeShader.SetTexture(0, "Texture", texture);
            computeShader.SetBuffer(0, "ShapesBuffer", _shapesBuffer);
            computeShader.SetInt("TextureSize", textureSize);
            computeShader.SetVector("ViewerScale", Viewer.localScale);
            computeShader.SetInt("ShapesCount", shapes.Count);

            var threadGroupSize = textureSize / 8;
            
            computeShader.Dispatch(0, threadGroupSize, threadGroupSize, threadGroupSize);
        }
        
                    
        material.SetTexture(SDFTexture, _renderTexture);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        
        Gizmos.DrawWireCube(Viewer.position, Viewer.localScale);
    }
    
    private struct Shape
    {
        Vector3 color;
        int useColor;
        Vector3 position;
        Vector3 rotation;
        Vector3 scale;
        float blend;
        float blendColor;
        int geometry;
        int operation;
        
        public Shape(Color color, bool useColor, Vector3 position, Quaternion rotation, Vector3 scale, float blend, float blendColor, int geometry, int operation)
        {
            this.color = new Vector3(color.r, color.g, color.b);
            this.useColor = useColor ? 1 : 0;
            this.position = position;
            this.rotation = rotation.eulerAngles * Mathf.Deg2Rad;
            this.scale = scale;
            this.blend = blend;
            this.blendColor = blendColor;
            this.geometry = geometry;
            this.operation = operation;
        }
    };
}
