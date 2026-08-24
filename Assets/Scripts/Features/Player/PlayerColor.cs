using Unity.Netcode;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    [SerializeField] private Material[] costumeMaterials = new Material[10];
    public NetworkVariable<int> NetColorIndex = new NetworkVariable<int>
    (
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private SkinnedMeshRenderer[] _meshRenderers;

    private void Awake()
    {
        _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        ApplyMaterial(NetColorIndex.Value);

        NetColorIndex.OnValueChanged += OnColorChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetColorIndex.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(int previousValue, int newValue)
    {
        ApplyMaterial(newValue);
    }

   
    public void ApplyMaterial(int colorIndex)
    {
        if (costumeMaterials == null || costumeMaterials.Length == 0) return;
        if (_meshRenderers == null || _meshRenderers.Length == 0)
        {
            _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        colorIndex = Mathf.Clamp(colorIndex, 0, costumeMaterials.Length - 1);
        Material targetMaterial = costumeMaterials[colorIndex];

        if (targetMaterial == null) return;

        foreach (var meshRenderer in _meshRenderers)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material = targetMaterial;
            }
        }
    }

 
    public void SetColorServer(int colorIndex)
    {
        if (IsServer)
        {
            NetColorIndex.Value = colorIndex;
        }
    }
}