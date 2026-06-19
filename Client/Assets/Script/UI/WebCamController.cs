using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WebCamController : MonoBehaviour
{
    public Renderer targetRenderer;
    private WebCamTexture _webCamTexture;

    // UI Manager에서 호출할 함수
    // 웹캠 시작 함수
    public void StartCamera()
    {
        // 카메라 장치 유무 확인 
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("웹캠이 없습니다");
            return;
        }

        // 전면 카메라 우선 선택
        string frontCamName = "";
        foreach (var device in devices)
        {
            if (device.isFrontFacing)
            {
                frontCamName = device.name;
                break;
            }
        }

        // WebCamTexture 생성 
        // 전면 카메라 없으면 배열 0번째 카메라로 설정
        _webCamTexture = new WebCamTexture(frontCamName != "" ? frontCamName : devices[0].name);

        // 3d 오브젝트 머티리얼에 텍스처 할당
        if (targetRenderer != null)
        {
            // 웹캠 텍스처 연결
            targetRenderer.material.mainTexture = _webCamTexture;

            // 셰이더를 Unlit/Texture로 설정 (빛에 영향 x)
            targetRenderer.material.shader = Shader.Find("Unlit/Texture");
        }

        _webCamTexture.Play();
    }


    // 웹캠 중지 함수
    public void StopCamera()
    {
        if (_webCamTexture != null && _webCamTexture.isPlaying)
        {
            _webCamTexture.Stop();
        }
    }

    public Texture2D GetSnapshot()
    {
        if (_webCamTexture == null || !_webCamTexture.isPlaying || _webCamTexture.width < 100)
        {
            Debug.Log("[WebCam] 아직 카메라가 준비되지 않았습니다.");
            return null;
        }

        Texture2D snap = new Texture2D(_webCamTexture.width, _webCamTexture.height);
        snap.SetPixels(_webCamTexture.GetPixels());
        snap.Apply();
        return snap;
    }
}
