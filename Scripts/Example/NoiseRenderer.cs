using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseRenderer : MonoBehaviour
{
    public ComputeShader shader;
    public int TexResolution = 256;
    public float maxTimerUpdate = .5f;
    float lastTime = 0;

    Renderer rend;
    RenderTexture myRt;
    // Start is called before the first frame update
    void Start()
    {
        myRt = new RenderTexture(TexResolution, TexResolution, 24);
        myRt.enableRandomWrite = true;
        myRt.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        UpdateTextureFromCompute();
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time - lastTime > maxTimerUpdate)
        {
            UpdateTextureFromCompute();
            lastTime = Time.time;
        }
    }

    private void UpdateTextureFromCompute()
    {
        int kernelHandle = shader.FindKernel("CSMain");
        shader.SetInt("RandOffset", (int)Time.timeSinceLevelLoad * 100);

        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        rend.material.SetTexture("_MainTex", myRt);
    }
}
