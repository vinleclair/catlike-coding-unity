using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    private enum TransitionMode
    {
        Cycle,
        Random
    }

    private const int MAXResolution = 1000;

    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material material;
    [SerializeField] private Mesh mesh;
    [SerializeField, Range(10, MAXResolution)] private int resolution = 10;
    [SerializeField] private FunctionLibrary.FunctionName function;
    [SerializeField] private TransitionMode transitionMode = TransitionMode.Cycle;
    [SerializeField, Min(0f)] private float functionDuration = 1f, transitionDuration = 1f;

    private float duration;
    private bool transitioning;
    private FunctionLibrary.FunctionName transitionFunction;
    private ComputeBuffer positionsBuffer;
    private static readonly int PositionsId = Shader.PropertyToID("_Positions"), 
        ResolutionId = Shader.PropertyToID("_Resolution"),
        StepId = Shader.PropertyToID("_Step"),
        TimeId = Shader.PropertyToID("_Time"),
        TransitionProgressId = Shader.PropertyToID("_TransitionProgress");

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(MAXResolution * MAXResolution, 3 * 4);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    private void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            } 
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }
        UpdateFunctionOnGPU();
    }

    private void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle
            ? FunctionLibrary.GetNextFunctionName(function)
            : FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

    private void UpdateFunctionOnGPU () {
        var step = 2f / resolution;
        
        computeShader.SetInt(ResolutionId, resolution);
        computeShader.SetFloat(StepId, step);
        computeShader.SetFloat(TimeId, Time.time);
        
        if (transitioning) {
            computeShader.SetFloat(
                TransitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration)
            );
        }
        
        var kernelIndex = (int)function + (int)(transitioning ? transitionFunction : function) * FunctionLibrary.FunctionCount;
        computeShader.SetBuffer(kernelIndex, PositionsId, positionsBuffer);
        
        var groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
        
        material.SetFloat(StepId, step);
        material.SetBuffer(PositionsId, positionsBuffer);
        
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }
}