using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct UpdateFractalLevelJob : IJobFor
    {
        public float Scale;
        public float DeltaTime;

        [ReadOnly] public NativeArray<FractalPart> Parents;
        public NativeArray<FractalPart> Parts;

        [WriteOnly] public NativeArray<float3x4> Matrices;

        public void Execute(int i)
        {
            var parent = Parents[i / 5];
            var part = Parts[i];
            part.SpinAngle += part.SpinVelocity * DeltaTime;

            var upAxis = mul(mul(parent.WorldRotation, part.Rotation), up());
            var sagAxis = cross(up(), upAxis);

            var sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude;
                var sagRotation = quaternion.AxisAngle(sagAxis, part.MaxSagAngle * sagMagnitude);
                baseRotation = mul(sagRotation, parent.WorldRotation);
            }
            else
            {
                baseRotation = parent.WorldRotation;
            }

            part.WorldRotation =
                mul(baseRotation,
                    mul(part.Rotation, quaternion.RotateY(part.SpinAngle)));
            part.WorldPosition =
                parent.WorldPosition +
                mul(part.WorldRotation, float3(0f, 1.5f * Scale, 0f));
            Parts[i] = part;

            var r = float3x3(part.WorldRotation) * Scale;
            Matrices[i] = float3x4(r.c0, r.c1, r.c2, part.WorldPosition);
        }
    }

    private struct FractalPart
    {
        public float3 WorldPosition;
        public quaternion Rotation, WorldRotation;
        public float MaxSagAngle, SpinAngle, SpinVelocity;
    }

    [SerializeField, Range(3, 8)] private int depth = 4;
    [SerializeField] private Mesh mesh, leafMesh;
    [SerializeField] private Material material;
    [SerializeField] private Gradient gradientA, gradientB;
    [SerializeField] private Color leafColorA, leafColorB;
    [SerializeField, Range(0f, 90f)] private float maxSagAngleA = 15f, maxSagAngleB = 25f;
    [SerializeField, Range(0f, 90f)] private float spinSpeedA = 20f, spinSpeedB = 25f;
    [SerializeField, Range(0f, 1f)] private float reverseSpinChance = 0.25f;

    private static readonly quaternion[] Rotations =
    {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI),
        quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI),
        quaternion.RotateX(-0.5f * PI)
    };

    private static readonly int
        ColorAId = Shader.PropertyToID("_ColorA"),
        ColorBId = Shader.PropertyToID("_ColorB"),
        MatricesId = Shader.PropertyToID("_Matrices"),
        SequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

    private static MaterialPropertyBlock _propertyBlock;

    private NativeArray<FractalPart>[] parts;

    private NativeArray<float3x4>[] matrices;

    private ComputeBuffer[] matricesBuffers;

    private Vector4[] sequenceNumbers;

    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        sequenceNumbers = new Vector4[depth];
        const int stride = 12 * 4;
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
            sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
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
        sequenceNumbers = null;
    }

    private void OnValidate()
    {
        if (parts == null || !enabled) return;
        OnDisable();
        OnEnable();
    }

    private FractalPart CreatePart(int childIndex)
    {
        return new FractalPart
        {
            MaxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
            Rotation = Rotations[childIndex],
            SpinVelocity = (Random.value < reverseSpinChance ? -1f : 1f) * radians(Random.Range(spinSpeedA, spinSpeedB))
        };
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        var rootPart = parts[0][0];
        rootPart.SpinAngle = rootPart.SpinVelocity * deltaTime;
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
                DeltaTime = deltaTime,
                Scale = scale,
                Parents = parts[li - 1],
                Parts = parts[li],
                Matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
        }

        jobHandle.Complete();

        var bounds = new Bounds(rootPart.WorldPosition, 3f * objectScale * Vector3.one);
        var leafIndex = matricesBuffers.Length - 1;
        for (var i = 0; i < matricesBuffers.Length; i++)
        {
            var buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            _propertyBlock.SetBuffer(MatricesId, buffer);
            Color colorA, colorB;
            Mesh instanceMesh;
            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                var gradientInterpolator = i / (matricesBuffers.Length - 2f);
                colorA = gradientA.Evaluate(gradientInterpolator);
                colorB = gradientB.Evaluate(gradientInterpolator);
                instanceMesh = mesh;
            }

            _propertyBlock.SetColor(ColorAId, colorA);
            _propertyBlock.SetColor(ColorBId, colorB);
            _propertyBlock.SetVector(SequenceNumbersId, sequenceNumbers[i]);
            Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, buffer.count, _propertyBlock);
        }
    }
}