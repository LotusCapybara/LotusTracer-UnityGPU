using CapyTracerCore.Tracer;
using Unity.Mathematics;
using UnityEngine;

public class TracerCamera : MonoBehaviour
{
    private Camera _camera;

    private RenderCamera _renderCamera;
    
    public bool isMoving { get; private set; }
    
    public void Initialize(RenderCamera renderCamera)
    {
        _camera = GetComponent<Camera>();
        _camera.enabled = false;
        _renderCamera = renderCamera;
        _camera.fieldOfView = renderCamera.fov;
        _camera.orthographicSize = renderCamera.horizontalSize;
        transform.position = renderCamera.position;
        transform.LookAt(transform.position + (Vector3) renderCamera.forward);
    }

    private void Update()
    {
        isMoving = false;
        
        if(_renderCamera == null)
            return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if ( Input.GetMouseButton(1))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            float speedY = Input.GetAxis("Mouse Y");
            transform.RotateAround(transform.position, transform.right, -speedY * 50f * Time.deltaTime);
            
            float speedX = Input.GetAxis("Mouse X");
            transform.RotateAround(transform.position, Vector3.up, speedX * 50f * Time.deltaTime);
            
            
            _renderCamera.fov = _camera.fieldOfView;
            
            _renderCamera.horizontalSize = _camera.orthographicSize;

            Vector3 movement = new Vector3();

            if (Input.GetKey(KeyCode.W))
                movement.z = 1;
            if (Input.GetKey(KeyCode.S))
                movement.z = -1;
            if (Input.GetKey(KeyCode.D))
                movement.x = 1;
            if (Input.GetKey(KeyCode.A))
                movement.x = -1;
            if (Input.GetKey(KeyCode.E))
                movement.y = 0.5f;
            if (Input.GetKey(KeyCode.Q))
                movement.y = -0.5f;
            
            movement.Normalize();

            movement = transform.TransformDirection(movement);

            transform.position += movement * (Time.deltaTime * 10f);
            
            _renderCamera.forward = transform.forward;
            _renderCamera.position = transform.position;
            _renderCamera.right = transform.right;
            _renderCamera.up = transform.up;

            isMoving = true;    
        }
    }
}
