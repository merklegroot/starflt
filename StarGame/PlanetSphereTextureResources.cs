using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Loads optional planet surface textures (equirectangular, CC BY 4.0 — see ASSETS.md).
/// Draws with <see cref="Raylib.DrawMesh"/> so the default 3D shader samples the albedo map
/// (Rlgl immediate mode inside <c>BeginMode3D</c> does not apply textures reliably).
/// Raylib enables backface culling (CCW front faces); <see cref="Raylib.GenMeshSphere"/> winding can
/// leave the camera-facing hemisphere culled, which reads as a solid black disk—disable culling for this draw only.
/// JPEG maps load as RGB; with blending enabled, RGB textures can sample with alpha 0 and contribute nothing (black).
/// We convert to RGBA8 before upload; rectangular 2:1 equirectangular dimensions are fine on the GPU.
/// <see cref="Raylib.GenMeshSphere"/> UVs are parametric, not equirectangular—without remapping the map looks like noise.
/// </summary>
internal static class PlanetSphereTextureResources
{
    private static Texture2D _jupiter;
    private static bool _jupiterLoaded;
    private static bool _jupiterFailed;

    private static Mesh _jupiterSphereMesh;
    private static bool _jupiterMeshReady;
    private static Material _jupiterMaterial;
    private static bool _jupiterMaterialReady;

    private static readonly string[] JupiterRelativePaths =
    {
        "Textures/solarsystemscope/jupiter.png",
        "Textures/solarsystemscope/jupiter.jpg"
    };

    /// <summary>Jupiter map from Solar System Scope; used when <see cref="Planet.Name"/> is Jupiter.</summary>
    public static bool TryGetJupiter(out Texture2D texture)
    {
        texture = default;

        if (_jupiterFailed)
        {
            return false;
        }

        if (!_jupiterLoaded)
        {
            for (int i = 0; i < JupiterRelativePaths.Length; i++)
            {
                string path = Path.Combine(AppContext.BaseDirectory, JupiterRelativePaths[i]);
                if (!File.Exists(path))
                {
                    continue;
                }

                // Keep a guaranteed RGBA upload path for consistent blending behavior.
                Image img = Raylib.LoadImage(path);
                if (img.Width <= 0 || img.Height <= 0)
                {
                    Raylib.UnloadImage(img);
                    continue;
                }

                Raylib.ImageFormat(ref img, PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8);
                _jupiter = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);

                if (_jupiter.Id != 0)
                {
                    Raylib.SetTextureFilter(_jupiter, TextureFilter.TEXTURE_FILTER_BILINEAR);
                    Raylib.SetTextureWrap(_jupiter, TextureWrap.TEXTURE_WRAP_REPEAT);
                    _jupiterLoaded = true;
                    break;
                }
            }

            if (!_jupiterLoaded)
            {
                _jupiterFailed = true;
                return false;
            }
        }

        texture = _jupiter;
        return true;
    }

    /// <summary>
    /// Unit-radius sphere mesh with equirectangular-friendly UVs (matches Solar System Scope maps).
    /// </summary>
    public static void DrawJupiterTextured(Texture2D texture, float sphereRadius, float rotationY)
    {
        EnsureJupiterSphereMesh();
        EnsureJupiterMaterial(texture);

        Matrix4x4 transform = Matrix4x4.CreateRotationY(rotationY) * Matrix4x4.CreateScale(sphereRadius);
        Rlgl.DisableBackfaceCulling();
        Raylib.DrawMesh(_jupiterSphereMesh, _jupiterMaterial, transform);
        Rlgl.EnableBackfaceCulling();
    }

    private static void EnsureJupiterSphereMesh()
    {
        if (_jupiterMeshReady)
        {
            return;
        }

        _jupiterSphereMesh = Raylib.GenMeshSphere(1f, 128, 128);
        ApplyEquirectangularTexCoords(ref _jupiterSphereMesh);
        _jupiterMeshReady = true;
    }

    /// <summary>
    /// Solar System Scope maps are equirectangular (longitude × latitude). par_shapes sphere UVs are not;
    /// remap texcoords from each vertex direction so the photo reads correctly on the mesh.
    /// </summary>
    private static unsafe void ApplyEquirectangularTexCoords(ref Mesh mesh)
    {
        Span<float> verts = mesh.VerticesAs<float>();
        Span<float> tc = mesh.TexCoordsAs<float>();
        int n = mesh.VertexCount;
        const float pi = MathF.PI;
        const float twoPi = 2f * MathF.PI;

        for (int i = 0; i < n; i++)
        {
            float x = verts[i * 3 + 0];
            float y = verts[i * 3 + 1];
            float z = verts[i * 3 + 2];
            float invLen = 1f / MathF.Sqrt(x * x + y * y + z * z);
            x *= invLen;
            y *= invLen;
            z *= invLen;

            float u = MathF.Atan2(z, x) / twoPi + 0.5f;
            float v = MathF.Asin(Math.Clamp(y, -1f, 1f)) / pi + 0.5f;
            tc[i * 2 + 0] = u;
            tc[i * 2 + 1] = v;
        }

        // Buffer index 1 = texcoords (see raylib UploadMesh / rlgl default mesh layout).
        fixed (float* texPtr = tc)
        {
            Raylib.UpdateMeshBuffer(mesh, 1, texPtr, tc.Length * sizeof(float), 0);
        }
    }

    private static void EnsureJupiterMaterial(Texture2D texture)
    {
        if (_jupiterMaterialReady)
        {
            return;
        }

        _jupiterMaterial = Raylib.LoadMaterialDefault();
        Raylib.SetMaterialTexture(ref _jupiterMaterial, MaterialMapIndex.MATERIAL_MAP_ALBEDO, texture);
        _jupiterMaterialReady = true;
    }
}
