using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class CSMRenderer
{
    private readonly Dictionary<Type, CSMInstance> csmInstances = new();

    public T GetInstance<T>() where T : CSMInstance
    {
        if (!csmInstances.TryGetValue(typeof(T), out CSMInstance? rinst))
        {
            rinst = Activator.CreateInstance<T>();
            csmInstances.Add(typeof(T), rinst);
            rinst.Load();
        }

        T? result = rinst as T;

        if (result == null) throw new Exception("Instance not found exception!");

        return result;
    }

    private readonly Matrix4[] lightMatrices = new Matrix4[3];
    public Texture2D[] depthMap = new Texture2D[3];
    private readonly FrameBuffer[] depthMapFBO = new FrameBuffer[3];

    public const int ShadowMapSize = 4096;

    private ShadowShader shadowShader = null!;
    private PhongShader phongShader = null!;

    private readonly float[] shadowCascadeLevels = { 20, 60 };

    private static void GetFrustomPoints(Span<Vector4> corners, Matrix4 proj, Matrix4 view)
    {
        bool result = Matrix4.Invert(proj * view, out Matrix4 inv);
        if (!result) throw new Exception("matrix misbehaved.");

        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector4 pt;
            pt.X = 2.0f * x - 1.0f;
            pt.Y = 2.0f * y - 1.0f;
            pt.Z = 2.0f * z - 1.0f;
            pt.W = 1.0f;

            pt = inv * pt;

            corners[4 * x + 2 * y + z] = pt * (1.0f / pt.W);
        }
    }

    private Vector3 lightDir;

    private Matrix4 GetLightSpaceMatrix(float nearPlane, float farPlane)
    {
        float width = RenderWindow.Instance.Width;
        float height = RenderWindow.Instance.Height;

        Camera camera = RenderWindow.Instance.Camera;

        Span<Vector4> corners = stackalloc Vector4[8];

        float cameraZoom = (float)(Math.PI / 4.0d); // TODO
        Matrix4 proj = MatrixHelper.CreatePerspectiveFieldOfView(cameraZoom, width / height, nearPlane, farPlane);

        GetFrustomPoints(corners, proj, camera.ViewMatrix);

        Vector3 center = Vector3.Zero;

        for (int i = 0; i < 8; i++)
        {
            center += corners[i].XYZ;
        }

        center *= 1.0f / 8.0f;

        Vector3 rotv = new Vector3(1, 2, 1);

        lightDir = Vector3.Normalize(rotv);
        Matrix4 lightView = MatrixHelper.CreateLookAt(center + lightDir, center, Vector3.UnitY);

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < 8; i++)
        {
            Vector4 trf = lightView * corners[i];
            if (trf.X < min.X) min.X = trf.X;
            if (trf.Y < min.Y) min.Y = trf.Y;
            if (trf.Z < min.Z) min.Z = trf.Z;
            if (trf.X > max.X) max.X = trf.X;
            if (trf.Y > max.Y) max.Y = trf.Y;
            if (trf.Z > max.Z) max.Z = trf.Z;
        }

        // Tune this parameter according to the scene
        float zMult = 3.0f;
        if (min.Z < 0) min.Z *= zMult;
        else min.Z /= zMult;

        if (max.Z < 0) max.Z /= zMult;
        else max.Z *= zMult;

        Matrix4 lightProjection = MatrixHelper.CreateOrthographicOffCenter(min.X, max.X, min.Y, max.Y, min.Z, max.Z);

        return lightProjection * lightView;
    }

    private void GetLightSpaceMatrices(Matrix4[] matrices)
    {
        Camera camera = RenderWindow.Instance.Camera;

        for (int i = 0;; i++)
        {
            if (i == 0) matrices[i] = GetLightSpaceMatrix(camera.NearPlane, shadowCascadeLevels[i]);
            else if (i < shadowCascadeLevels.Length)
                matrices[i] = GetLightSpaceMatrix(shadowCascadeLevels[i - 1], shadowCascadeLevels[i]);
            else
            {
                matrices[i] = GetLightSpaceMatrix(shadowCascadeLevels[i - 1], camera.FarPlane);
                break;
            }
        }
    }

    private void CreateFramebuffer(out Texture2D texture, out FrameBuffer frameBuffer)
    {
        texture = new Texture2D();

        frameBuffer = new FrameBuffer();

        frameBuffer.AttachDepthTexture(texture);

        texture.Specify(Texture.Format.Depth, ShadowMapSize, ShadowMapSize, Texture.Type.Float);
        texture.SetMinMagFilter(Texture.Filter.Nearest, Texture.Filter.Nearest);
        texture.SetWrap(Texture.Wrap.ClampToBorder);
        texture.SetBorderColor(new Vector4(1, 1, 1, 1));

        FrameBuffer.Default.Bind();
    }

    public void Load()
    {
        for (int i = 0; i < 3; i++) CreateFramebuffer(out depthMap[i], out depthMapFBO[i]);

        shadowShader = new ShadowShader();
        phongShader = new PhongShader();

        shadowShader.Use();
        shadowShader.Model.Set(Matrix4.Identity);
        phongShader.Use();
        phongShader.Model.Set(Matrix4.Identity);
    }

    public void Draw()
    {
        Camera camera = RenderWindow.Instance.Camera;

        GLDevice.SetViewport(0, 0, ShadowMapSize, ShadowMapSize);

        foreach (var drawable in csmInstances.Values)
        {
            if (drawable.Count == 0) continue;
            drawable.UpdateWorldMatrices();
        }

        shadowShader.Use();

        //GLDevice.Disable(Capability.CullFace);

        GetLightSpaceMatrices(lightMatrices);

        for (int i = 0; i < 3; i++)
        {
            depthMapFBO[i].Bind();
            GLDevice.Clear(ClearFlags.DepthBuffer);

            shadowShader.ProjectionView.Set(lightMatrices[i]);

            GLDevice.SetCullFaceMode(CullMode.Front);

            foreach (var drawable in csmInstances.Values)
            {
                if (drawable.Count == 0) continue;
                drawable.ShadowPass(shadowShader);
            }

            GLDevice.SetCullFaceMode(CullMode.Back);
        }

        FrameBuffer.Default.Bind();

        for (uint i = 0; i < 3; i++)
        {
            depthMap[i].Bind(i);
        }

        GLDevice.SetViewport(0, 0, RenderWindow.Instance.Width, RenderWindow.Instance.Height);

        phongShader.Use();
        phongShader.MaterialProperties.SetDefaultMaterial();
        phongShader.Projection.Set(camera.ProjectionMatrix);
        phongShader.View.Set(camera.ViewMatrix);
        phongShader.ViewPosition.Set(camera.Position);
        phongShader.Lights.Set(lightMatrices, false);
        phongShader.SunDir.Set(lightDir);

        //GLDevice.Enable(Capability.CullFace);

        foreach (var drawable in csmInstances.Values)
        {
            if (drawable.Count == 0) continue;

            drawable.LightPass(phongShader);

            int mld2 = drawable.WorldMatrices.Length / 2;
            if (drawable.Count < mld2)
            {
                Array.Resize(ref drawable.WorldMatrices, mld2);
            }

            drawable.Count = 0;
        }
    }
}