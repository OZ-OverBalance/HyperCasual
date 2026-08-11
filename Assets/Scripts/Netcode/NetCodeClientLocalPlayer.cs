using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class DNNetCodeClientLocalPlayer : NetworkBehaviour
{
    [Header("플레이어 설정")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] public float _mouseSensitivity = 200f;

    [Header("점프 및 물리")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private bool _isGrounded;

    [Header("컴포넌트")]
    [SerializeField] public Camera Camera_Player;
    [SerializeField] public AudioListener AudioListner_Player;

    [SerializeField] private Rigidbody Rigidbody_Player;
    [SerializeField] private GroundDetector GroundDetector;


    // 카메라 상하 회전값 저장용
    private float _rotationX = 0f;

    private bool _isMouseLock;

    void Start()
    {
        ToggleMouseLock(true);
    }

    private void ToggleMouseLock(bool isLock)
    {
        // 게임 시작 시 마우스 커서를 화면 중앙에 고정하고 숨김
        Cursor.lockState = isLock ? CursorLockMode.Locked : CursorLockMode.None;
        _isMouseLock = isLock;
    }

    #region NetCode Sample

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 네트워크 스폰이 완료된 시점에 로컬 플레이어 여부 확인
        CheckIsLocalPlayer();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

    }

    #endregion

    private void OnEnable()
    {
        GroundDetector.GroundTriggeredEvent += OnGroundTriggered;
    }

    private void OnDisable()
    {
        GroundDetector.GroundTriggeredEvent -= OnGroundTriggered;
    }

    private void CheckIsLocalPlayer()
    {
        // [NetCode] 내가 아니라면, 카메라 같이 조종하는 사람 외에 필요 없는 요소들은 비활성화한다
        // 데디서버 플레이어 개발시 가장 헷갈리는 부분일 수 있다 (이 플레이어 컴포넌트가 나 뿐만 아니라 타 플레이어 기준으로도 조작될 수 있기 때문에)
        bool isLocalPlayer = this.IsOwner;
        Camera_Player.gameObject.SetActive(isLocalPlayer);
        Camera_Player.enabled = isLocalPlayer;
        AudioListner_Player.enabled = isLocalPlayer;
    }


    void Update()
    {
        // [NetCode] : 네트워크 비헤이비어에 있는 IsOwner
        if (this.IsOwner == false)
        {
            return; // 내 캐릭터가 아니면 입력 처리 제외
        }


        RotateCheckOnUpdate();
        MoveOnUpdate();

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            StartJump();
        }

        if (Input.GetKeyDown(KeyCode.LeftAlt) && _isGrounded)
        {
            ToggleMouseLock(false);
        }

        if (Input.GetKeyUp(KeyCode.LeftAlt) && _isGrounded)
        {
            ToggleMouseLock(true);
        }

 
    }

    void RotateCheckOnUpdate()
    {
        if (_isMouseLock == false)
        {
            // 마우스 락 상태가 아니면 화면 회전 제외
            return;
        }

        // 1. 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.deltaTime;

        // 2. 상하 회전 (카메라만 까딱까딱)
        _rotationX -= mouseY;
        // 너무 뒤로 넘어가지 않게 Clamp를 써서 제한
        _rotationX = Mathf.Clamp(_rotationX, -90f, 90f);
        Camera_Player.transform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);

        // 3. 좌우 회전 (플레이어 몸통 전체를 회전)
        this.transform.Rotate(Vector3.up * mouseX);
    }

    void MoveOnUpdate()
    {
        // 1. 키보드 입력 받기 (W, S, A, D)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");



        // 2. "내가 바라보는 방향" 기준으로 이동 방향 계산
        // transform.right는 나의 오른쪽, transform.forward는 나의 앞쪽
        Vector3 move = (this.transform.right * x) + (this.transform.forward * z);

        // 3. 실제 이동 처리
        this.transform.position += ((move * _moveSpeed) * Time.deltaTime);
    }

    void StartJump()
    {
        // 위쪽 방향으로 즉각적인 힘을 가함
        Rigidbody_Player.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        _isGrounded = false;
    }


    private void OnGroundTriggered(bool isGrounded)
    {
        _isGrounded = isGrounded;
    }

}
