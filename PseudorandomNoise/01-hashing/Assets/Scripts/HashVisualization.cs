using System;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;
using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct HashJob : IJobFor
    {
        [WriteOnly] public NativeArray<uint> Hashes;
        
        public int Resolution;

        public float InvResolution;

        public SmallXXHash Hash;

        public void Execute(int i)
        {
            var v = (int)floor(InvResolution * i + 0.00001f);
            var u = i - Resolution * v - Resolution / 2;
            v -= Resolution / 2;

            Hashes[i] = Hash.Eat(u).Eat(v);
        }
    }

    private static readonly int
        HashesId = Shader.PropertyToID("_Hashes"),
        ConfigId = Shader.PropertyToID("_Config");

    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private Material material;
    [SerializeField, Range(1, 512)] private int resolution;
    [SerializeField] private int seed = 0;
    [SerializeField, Range(-2f, 2f)] private float verticalOffset = 1f;

    private NativeArray<uint> _hashes;

    private ComputeBuffer _hashesBuffer;

    private MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        var length = resolution * resolution;
        _hashes = new NativeArray<uint>(length, Allocator.Persistent);
        _hashesBuffer = new ComputeBuffer(length, 4);

        new HashJob
        {
            Hashes = _hashes,
            Resolution = resolution,
            InvResolution = 1f / resolution,
            Hash = SmallXXHash.Seed(seed)
        }.ScheduleParallel(_hashes.Length, resolution, default).Complete();
        
        _hashesBuffer.SetData(_hashes);

        _propertyBlock ??= new MaterialPropertyBlock();
        _propertyBlock.SetBuffer(HashesId, _hashesBuffer);
        _propertyBlock.SetVector(ConfigId, new Vector4(resolution, 1f / resolution, verticalOffset / resolution));
    }

    private void OnDisable()
    {
        _hashes.Dispose();
        _hashesBuffer.Release();
        _hashesBuffer = null;
    }

    private void OnValidate ()
    {
        if (_hashesBuffer == null || !enabled) return;
        OnDisable();
        OnEnable();
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one),
            _hashes.Length, _propertyBlock
        );
    }
}
