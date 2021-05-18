using UnityEngine;

public class MaterialSelector : MonoBehaviour
{
    [SerializeField] private Material[] materials = default;
    [SerializeField] private MeshRenderer meshRenderer = default;

    public void Select(int index)
    {
        if (meshRenderer && materials != null && index >= 0 && index < materials.Length)
        {
            meshRenderer.material = materials[index];
        }
    }
}