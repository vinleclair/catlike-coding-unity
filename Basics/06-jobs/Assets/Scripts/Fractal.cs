using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct UpdateFractalLevelJob : IJobFor
    {
        public float SpinAngleDelta;
        public float Scale;

        [ReadOnly] public NativeArray<FractalPart> Parents;
        public NativeArray<FractalPart> Parts;

        [WriteOnly] public NativeArray<float3x4> Matrices;

        public void Execute(int i)
        {
            var parent = Parents[i / 5];
            var part = Parts[i];
            part.SpinAngle += SpinAngleDelta;
            part.WorldRotation =
                mul(parent.WorldRotation,
                    mul(part.Rotation, quaternion.RotateY(part.SpinAngle)));
            part.WorldPosition =
                parent.WorldPosition +
                mul(parent.WorldRotation, (1.5f * Scale * part.Direction));
            Parts[i] = part;

            var r = float3x3(part.WorldRotation) * Scale;
            Matrices[i] = float3x4(r.c0, r.c1, r.c2, part.WorldPosition);
        }
    }

    private struct FractalPart
    {
        public float3 Direction, WorldPosition;
        public quaternion Rotation, WorldRotation;
        public float SpinAngle;
    }

    [SerializeField, Range(1, 8)] private int depth = 4;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    private static readonly float3[] Directions =
    {
        up(),
        right(),
        left(),
        forward(),
        back()
    };

    private static readonly quaternion[] Rotations =
    {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI),
        quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI),
        quaternion.RotateX(-0.5f * PI)
    };

    private static readonly int MatricesId = Shader.PropertyToID("_Matrices");

    private static MaterialPropertyBlock _propertyBlock;

    private NativeArray<FractalPart>[] parts;

    private NativeArray<float3x4>[] matrices;

    private ComputeBuffer[] matricesBuffers;


    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        const int stride = 12 * 4;
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        parts[0][0] = CreatePart(0);
        for (var li = 1; li < parts.Length; li++)
        {
            var levelParts = parts[li];
            for (var fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (var ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        _propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        for (var i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }

        parts = null;
        matrices = null;
        matricesBuffers = null;
    }

    private void OnValidate()
    {
        if (parts == null || !enabled) return;
        OnDisable();
        OnEnable();
    }

    private static FractalPart CreatePart(int childIndex)
    {
        return new FractalPart
        {
            Direction = Directions[childIndex],
            Rotation = Rotations[childIndex]
        };
    }

    private void Update()
    {
        var spinAngleDelta = 0.125f * PI * Time.deltaTime;
        var rootPart = parts[0][0];
        rootPart.SpinAngle = spinAngleDelta;
        rootPart.WorldRotation =
            mul(transform.rotation, mul(rootPart.Rotation, quaternion.RotateY(rootPart.SpinAngle)));
        var gameObjectTransform = transform;
        rootPart.WorldPosition = gameObjectTransform.position;
        parts[0][0] = rootPart;
        var objectScale = gameObjectTransform.lossyScale.x;
        var r = float3x3(rootPart.WorldRotation) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.WorldPosition);

        var scale = objectScale;
        JobHandle jobHandle = default;
        for (var li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob
            {
                SpinAngleDelta = spinAngleDelta,
                Scale = scale,
                Parents = parts[li - 1],
                Parts = parts[li],
                Matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
        }

        jobHandle.Complete();

        var bounds = new Bounds(rootPart.WorldPosition, 3f * objectScale * Vector3.one);
        for (var i = 0; i < matricesBuffers.Length; i++)
        {
            var buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            _propertyBlock.SetBuffer(MatricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, buffer.count, _propertyBlock);
        }
    }
}