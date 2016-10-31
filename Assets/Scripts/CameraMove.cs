using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour {
    //旋转变量;
    private float m_deltX = 0f;
    private float m_deltY = 0f;
    //缩放变量;
    private float m_distance = 10f;
    private float m_mSpeed = 5f;
    //移动变量;
    private Vector3 m_mouseMovePos = Vector3.zero;

    void Start() {
        GetComponent<Camera>().transform.localPosition = new Vector3(0, m_distance, 0);
    }

    void Update() {
        //鼠标右键点下控制相机旋转;
        if (Input.GetMouseButton(1)) {
            m_deltX += Input.GetAxis("Mouse X") * m_mSpeed;
            m_deltY -= Input.GetAxis("Mouse Y") * m_mSpeed;
            m_deltX = ClampAngle(m_deltX, -360, 360);
            m_deltY = ClampAngle(m_deltY, -70, 70);
            GetComponent<Camera>().transform.rotation = Quaternion.Euler(m_deltY, m_deltX, 0);
        }

        //鼠标中键点下场景缩放;
        if (Input.GetAxis("Mouse ScrollWheel") != 0) {
            //自由缩放方式;
            m_distance = Input.GetAxis("Mouse ScrollWheel") * 10f;
            GetComponent<Camera>().transform.localPosition = GetComponent<Camera>().transform.position + GetComponent<Camera>().transform.forward * m_distance;
        }

        if (Input.GetKey(KeyCode.W)) {
            gameObject.transform.Translate(new Vector3(0, 0, 10 * Time.deltaTime));
        }
        if (Input.GetKey(KeyCode.S)) {
            gameObject.transform.Translate(new Vector3(0, 0, -10 * Time.deltaTime));
        }
        if (Input.GetKey(KeyCode.A)) {
            gameObject.transform.Translate(new Vector3(-10 * Time.deltaTime, 0, 0 * Time.deltaTime));
        }
        if (Input.GetKey(KeyCode.D)) {
            gameObject.transform.Translate(new Vector3(10 * Time.deltaTime, 0, 0));
        }

        //相机复位远点;
        if (Input.GetKey(KeyCode.Space)) {
            m_distance = 10.0f;
            GetComponent<Camera>().transform.localPosition = new Vector3(0, m_distance, 0);
        }
    }

    //规划角度;
    float ClampAngle(float angle, float minAngle, float maxAgnle) {
        if (angle <= -360)
            angle += 360;
        if (angle >= 360)
            angle -= 360;

        return Mathf.Clamp(angle, minAngle, maxAgnle);
    }
}