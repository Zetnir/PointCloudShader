using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOfLifeRenderer : MonoBehaviour
{
    public ComputeShader shader;
    public int TexResolution = 256;

    Renderer rend;
    RenderTexture[] myRt;
    int currentTex = 0;
    int numTex = 2;

    bool doUpdate = true;

    public float maxTimerUpdate = .5f;
    float lastTime = 0;
    // Start is called before the first frame update
    void Start()
    {
        myRt = new RenderTexture[]{
           new RenderTexture(TexResolution, TexResolution, 24),
           new RenderTexture(TexResolution, TexResolution, 24),
        };
        myRt[0].enableRandomWrite = true;
        myRt[0].Create();
        myRt[1].enableRandomWrite = true;
        myRt[1].Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        ResetComputeSim();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            doUpdate = !doUpdate;
            lastTime = 0f;
        }

        if(doUpdate && Time.time - lastTime > maxTimerUpdate)
        {
            ComputeStepFrame();
            lastTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetComputeSim();
        }
    }

    void ComputeStepFrame()
    {
        int prevTex = currentTex;
        currentTex = (currentTex + 1) % numTex;

        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(kernelHandle, "Prev", myRt[prevTex]);
        shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));
        shader.SetInt("TexSize", TexResolution - 1);
        shader.SetTexture(kernelHandle, "Result", myRt[currentTex]);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        rend.material.SetTexture("_MainTex", myRt[currentTex]);
        Debug.Log("Compute Step");
    }

    void ResetComputeSim()
    {
        int kernelHandle = shader.FindKernel("CSRandom");
        shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));
        shader.SetTexture(kernelHandle, "Result", myRt[currentTex]);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        rend.material.SetTexture("_MainTex", myRt[currentTex]);
        lastTime = Time.time;
    }

}
