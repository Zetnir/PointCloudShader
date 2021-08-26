using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshRenderer : MonoBehaviour
{

    MeshFilter mf;
    [Header("Dependencies")]
    public Material mt;
    public ComputeShader shader;
    [Header("Point Cloud Parameters")]
    public int pointsNumber = 100;
    public float pointsDistance = 10;
    [Range(0.0f, 1.0f)]
    public float particleSize = 2;
    [Header("Render Options")]
    public bool doUpdate = true;
    public RenderMode mode = RenderMode.Triangle;

    ComputeBuffer ParticleBuffer;
    int[] ConsumeIds;

    ComputeBuffer vertBuffer;
    ComputeBuffer colBuffer;
    ComputeBuffer indexBuffer;

    RenderMode previousMode = RenderMode.Triangle;

    public enum RenderMode
    {
        Triangle,
        Quad,
        Piramyd,
        Cube,
    }

    struct ParticleData
    {
        public Vector3 position;
        public Vector4 color;
    }

    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mf.mesh = mesh;
        GetComponent<Renderer>().material = mt;
        
        ParticleBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 7, ComputeBufferType.Append);

        vertBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 3 * 8, ComputeBufferType.Append);
        colBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 4 * 8, ComputeBufferType.Append);
        indexBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 36, ComputeBufferType.Append);

        ResetComputeSim();
    }

    // Create a random series of points with a position and a color
    private void ResetComputeSim()
    {
        ParticleData[] tempArray = new ParticleData[pointsNumber];
        for (int i = 0; i < pointsNumber; ++i)
        {
            tempArray[i].position = new Vector3(Random.value * pointsDistance, Random.value * pointsDistance, Random.value * pointsDistance);
            tempArray[i].color = new Vector4(Random.value, Random.value, Random.value, 1.0f);
        }

        ParticleBuffer.SetData(tempArray);
        ParticleBuffer.SetCounterValue((uint)pointsNumber);

        ComputeStepFrame();

    }
    private void SetShaderValues()
    {
        shader.SetFloat("ParticleSize", particleSize);
        Vector3 camPos = Camera.main.transform.position;
        shader.SetFloats("CamPos", new float[] { camPos.x, camPos.y, camPos.z });
        shader.SetInt("RenderMode", (int)mode);
        shader.SetFloats("WipeColour", new float[] { 0, 0, 0, 0 });
    }

    // Create the mesh of each particle with the data stored in the buffer
    private void ComputeStepFrame()
    {
        SetShaderValues();

        // Build Mesh
        int kernelHandle = shader.FindKernel("CSGenerateMesh");

        shader.SetBuffer(kernelHandle, "ParticleBuffer", ParticleBuffer);
        shader.SetBuffer(kernelHandle, "ParticleMeshVert", vertBuffer);
        shader.SetBuffer(kernelHandle, "ParticelMeshCol", colBuffer);
        shader.SetBuffer(kernelHandle, "ParticleMeshIndecies", indexBuffer);
        shader.Dispatch(kernelHandle, pointsNumber / 10, 1, 1);

        // Not the best and you would want a platform dive
        // To check how to avoid copying this off GPU memory
        Vector3[] vData = new Vector3[pointsNumber * 8];
        vertBuffer.GetData(vData);
        mf.mesh.vertices = vData;

        Color[] cData = new Color[pointsNumber * 8];
        colBuffer.GetData(cData);
        mf.mesh.colors = cData;

        int[] iData = new int[pointsNumber * 36];
        indexBuffer.GetData(iData);
        mf.mesh.SetIndices(iData, MeshTopology.Triangles, 0);
        
    }

    private void OnDestroy()
    {
        ParticleBuffer.Release();
        vertBuffer.Release();
        colBuffer.Release();
        indexBuffer.Release();
    }

    private void ResetBuffers()
    {
        ParticleBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 7, ComputeBufferType.Append);
        vertBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 3 * 8, ComputeBufferType.Append);
        colBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 4 * 8, ComputeBufferType.Append);
        indexBuffer = new ComputeBuffer(pointsNumber, sizeof(float) * 36, ComputeBufferType.Append);
        ParticleBuffer.SetCounterValue(0);
        colBuffer.SetCounterValue(0);
        vertBuffer.SetCounterValue(0);
        indexBuffer.SetCounterValue(0);
        ResetComputeSim();
    }

    void Update()
    {
        if(previousMode != mode)
        {
            Debug.Log("change mode");
            doUpdate = false;
            ResetBuffers();
            doUpdate = true;
            previousMode = mode;
        }


        if(doUpdate)
            ComputeStepFrame();
    }
}
