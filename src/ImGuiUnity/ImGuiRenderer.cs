using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
using UnityEngine;
using UnityEngine.Rendering;

namespace fih.ImGuiUnity;

/// <summary>
/// Turns ImGui's draw lists into Unity draw calls: one Mesh with a submesh per ImGui
/// command, each with its own texture and clip rect.
/// </summary>
internal sealed class ImGuiRenderer : IDisposable
{
    /// <summary>Source layout, binary-identical to ImDrawVert.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ImVert
    {
        public Vector2 Pos;
        public Vector2 Uv;
        public uint Col;
    }

    /// <summary>
    /// Destination layout: Unity reorders vertex attributes into its canonical order
    /// (position, colour, UVs), and position is Float32x3 so mesh bounds can be computed.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Vert
    {
        public Vector3 Pos;
        public uint Col;
        public Vector2 Uv;
    }

    private static readonly VertexAttributeDescriptor[] Layout =
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
    };

    private const MeshUpdateFlags SilentUpdate =
        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
        MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int TextureSampleAddId = Shader.PropertyToID("_TextureSampleAdd");
    private static readonly int GuiZTestModeId = Shader.PropertyToID("unity_GUIZTestMode");

    private readonly Mesh _mesh;
    private readonly Material _material;
    private readonly CommandBuffer _commands = new CommandBuffer { name = "fih.imgui" };
    private readonly List<MaterialPropertyBlock> _blocks = new List<MaterialPropertyBlock>();
    private readonly List<SubMeshDescriptor> _submeshes = new List<SubMeshDescriptor>();
    private readonly Dictionary<IntPtr, Texture> _textures = new Dictionary<IntPtr, Texture>();

    private Vert[] _vertices = new Vert[8192];
    private ushort[] _indices = new ushort[16384];
    private Texture2D _fontAtlas;

    internal ImGuiRenderer()
    {
        // UI/Default is UGUI's shader: straight alpha blending with a vertex-colour
        // multiply, matching ImGui's output convention.
        var shader = Shader.Find("UI/Default");
        if (shader == null) throw new InvalidOperationException("UI/Default shader missing from this build");

        _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _material.SetVector(TextureSampleAddId, Vector4.zero); // RGBA atlas, no alpha-only fixup
        // Without this the shader keeps its default depth test and the scene's depth
        // buffer rejects the overlay.
        _material.SetInt(GuiZTestModeId, (int)CompareFunction.Always);

        _mesh = new Mesh { name = "fih.imgui", hideFlags = HideFlags.HideAndDontSave };
        _mesh.MarkDynamic();
    }

    /// <summary>Uploads ImGui's font atlas and hands ImGui back the id we key it under.</summary>
    internal void CreateFontAtlas(ImGuiIOPtr io)
    {
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out var bytesPerPixel);

        _fontAtlas = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
        {
            name = "fih.imgui.atlas",
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        _fontAtlas.LoadRawTextureData(pixels, width * height * bytesPerPixel);
        _fontAtlas.Apply(false, false);

        var id = (IntPtr)1;
        _textures[id] = _fontAtlas;
        io.Fonts.SetTexID(id);
        io.Fonts.ClearTexData();

        Plugin.Logger.LogInfo($"[imgui] font atlas uploaded {width}x{height}");
    }

    internal void Render(ScriptableRenderContext context, ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0 || drawData.TotalVtxCount == 0) return;

        BuildMesh(drawData);

        var width = drawData.DisplaySize.X;
        var height = drawData.DisplaySize.Y;

        _commands.Clear();
        // ImGui's origin is top-left with y down, hence top and bottom are swapped here.
        _commands.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(0f, width, height, 0f, -1f, 1f));

        var submesh = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var list = drawData.CmdLists[n];
            for (var c = 0; c < list.CmdBuffer.Size; c++, submesh++)
            {
                var cmd = list.CmdBuffer[c];
                var clip = cmd.ClipRect; // (min.x, min.y, max.x, max.y), y down

                _commands.EnableScissorRect(new Rect(clip.X, height - clip.W, clip.Z - clip.X, clip.W - clip.Y));

                var block = BlockAt(submesh);
                block.SetTexture(MainTexId, TextureFor(cmd.TextureId));
                _commands.DrawMesh(_mesh, Matrix4x4.identity, _material, submesh, 0, block);
            }
        }

        _commands.DisableScissorRect();
        context.ExecuteCommandBuffer(_commands);
        context.Submit();
    }

    private unsafe void BuildMesh(ImDrawDataPtr drawData)
    {
        var totalVertices = drawData.TotalVtxCount;
        var totalIndices = drawData.TotalIdxCount;

        if (_vertices.Length < totalVertices) Array.Resize(ref _vertices, Mathf.NextPowerOfTwo(totalVertices));
        if (_indices.Length < totalIndices) Array.Resize(ref _indices, Mathf.NextPowerOfTwo(totalIndices));

        _submeshes.Clear();
        var vertexOffset = 0;
        var indexOffset = 0;

        fixed (Vert* vertexDst = _vertices)
        fixed (ushort* indexDst = _indices)
        {
            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var list = drawData.CmdLists[n];
                var vertexCount = list.VtxBuffer.Size;
                var indexCount = list.IdxBuffer.Size;

                // Layouts differ, so this is a per-vertex shuffle rather than a memcpy.
                var source = (ImVert*)list.VtxBuffer.Data;
                for (var v = 0; v < vertexCount; v++)
                {
                    var vertex = source[v];
                    vertexDst[vertexOffset + v] = new Vert
                    {
                        Pos = new Vector3(vertex.Pos.x, vertex.Pos.y, 0f),
                        Col = vertex.Col,
                        Uv = vertex.Uv
                    };
                }

                Buffer.MemoryCopy((void*)list.IdxBuffer.Data, indexDst + indexOffset,
                    (_indices.Length - indexOffset) * sizeof(ushort), indexCount * sizeof(ushort));

                // Indices are local to each draw list, so baseVertex rebases them.
                for (var c = 0; c < list.CmdBuffer.Size; c++)
                {
                    var cmd = list.CmdBuffer[c];
                    _submeshes.Add(new SubMeshDescriptor
                    {
                        topology = MeshTopology.Triangles,
                        indexStart = indexOffset + (int)cmd.IdxOffset,
                        indexCount = (int)cmd.ElemCount,
                        baseVertex = vertexOffset + (int)cmd.VtxOffset,
                        bounds = default
                    });
                }

                vertexOffset += vertexCount;
                indexOffset += indexCount;
            }
        }

        _mesh.Clear(true);
        _mesh.SetVertexBufferParams(totalVertices, Layout);
        _mesh.SetVertexBufferData(_vertices, 0, 0, totalVertices, 0, SilentUpdate);
        _mesh.SetIndexBufferParams(totalIndices, IndexFormat.UInt16);
        _mesh.SetIndexBufferData(_indices, 0, 0, totalIndices, SilentUpdate);

        _mesh.subMeshCount = _submeshes.Count;
        for (var i = 0; i < _submeshes.Count; i++) _mesh.SetSubMesh(i, _submeshes[i], SilentUpdate);

        // Bounds are never recalculated, so give the mesh one that can't be culled.
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
    }

    private MaterialPropertyBlock BlockAt(int index)
    {
        while (_blocks.Count <= index) _blocks.Add(new MaterialPropertyBlock());
        return _blocks[index];
    }

    private Texture TextureFor(IntPtr id) =>
        _textures.TryGetValue(id, out var texture) ? texture : _fontAtlas;

    public void Dispose()
    {
        _commands.Dispose();
        if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
        if (_material != null) UnityEngine.Object.Destroy(_material);
        if (_fontAtlas != null) UnityEngine.Object.Destroy(_fontAtlas);
    }
}
