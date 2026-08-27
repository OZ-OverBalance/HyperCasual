using UnityEngine;

public class VisualConveyor : MonoBehaviour
{
    [SerializeField] private ObstacleConveyor Conveyor_Logic;
    [SerializeField] private Renderer Renderer_Belt;
    [SerializeField] private int MaterialSlotIndex = 1; 
    [SerializeField] private float ScrollSpeed = 3f;
    [SerializeField] private string TextureProperty = "_BaseMap_ST"; 

    private MaterialPropertyBlock _propertyBlock;
    private float _offset;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (Renderer_Belt == null || Conveyor_Logic == null) return;

        float direction = Conveyor_Logic.Direction == ConveyorDirection.Clockwise ? 1f : -1f;
        _offset += ScrollSpeed * direction * Time.deltaTime;

        Renderer_Belt.GetPropertyBlock(_propertyBlock, MaterialSlotIndex);
        _propertyBlock.SetVector(TextureProperty, new Vector4(1f, 1f, 0f, _offset));
        Renderer_Belt.SetPropertyBlock(_propertyBlock, MaterialSlotIndex);
    }
}