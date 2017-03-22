using System;
using AcTools.Kn5File;
using AcTools.Render.Base;
using AcTools.Render.Base.Cameras;
using AcTools.Render.Base.Materials;
using AcTools.Render.Base.Objects;
using AcTools.Render.Base.Structs;
using AcTools.Render.Base.Utils;
using AcTools.Render.Kn5Specific.Objects;
using AcTools.Render.Kn5Specific.Textures;
using AcTools.Render.Shaders;
using AcTools.Utils.Helpers;
using JetBrains.Annotations;
using SlimDX;
using SlimDX.Direct3D11;

namespace AcTools.Render.Kn5SpecificSpecial {
    public class DepthMaterialsFactory : IMaterialsFactory {
        public IRenderableMaterial CreateMaterial(object key) {
            if (BasicMaterials.DepthOnlyKey.Equals(key)) {
                /* Model is loaded directly without using Kn5RenderableFile as a wrapper, so all materials
                 * keys won�t be converted to Kn5MaterialDescription. We don�t need any information about
                 * materials anyway. */
                return new Kn5MaterialDepth();
            }

            return new InvisibleMaterial();
        }
    }

    public class Kn5MaterialDepth : IRenderableMaterial {
        private EffectSpecialShadow _effect;

        public void Initialize(IDeviceContextHolder contextHolder) {
            _effect = contextHolder.GetEffect<EffectSpecialShadow>();
        }

        public bool Prepare(IDeviceContextHolder contextHolder, SpecialRenderMode mode) {
            contextHolder.DeviceContext.InputAssembler.InputLayout = _effect.LayoutP;
            return true;
        }

        public void SetMatrices(Matrix objectTransform, ICamera camera) {
            _effect.FxWorldViewProj.SetMatrix(objectTransform * camera.ViewProj);
        }

        public void Draw(IDeviceContextHolder contextHolder, int indices, SpecialRenderMode mode) {
            _effect.TechSimplest.DrawAllPasses(contextHolder.DeviceContext, indices);
        }

        public void PrepareAo(IDeviceContextHolder contextHolder, ShaderResourceView txNormal, float uvRepeat) {
            contextHolder.DeviceContext.InputAssembler.InputLayout = _effect.LayoutPNTG;
            _effect.FxNormalMap.SetResource(txNormal);
            _effect.FxNormalUvMult.Set(uvRepeat);
        }

        public void SetMatricesAo(Matrix objectTransform) {
            _effect.FxWorld.SetMatrix(objectTransform);
            _effect.FxWorldInvTranspose.SetMatrix(Matrix.Invert(Matrix.Transpose(objectTransform)));
        }

        public void DrawAo(IDeviceContextHolder contextHolder, int indices) {
            _effect.TechAo.DrawAllPasses(contextHolder.DeviceContext, indices);
        }

        public bool IsBlending => false;

        public void Dispose() { }
    }

    public interface INormalTexturesProvider : IDisposable {
        [CanBeNull]
        Tuple<IRenderableTexture, float> GetTexture(IDeviceContextHolder contextHolder, uint materialId);
    }

    public sealed class Kn5RenderableDepthOnlyObject : TrianglesRenderableObject<InputLayouts.VerticeP>, IKn5RenderableObject {
        public Kn5Node OriginalNode { get; }

        public Matrix ModelMatrixInverted { get; set; }

        public void SetMirrorMode(IDeviceContextHolder holder, bool enabled) { }

        public void SetDebugMode(IDeviceContextHolder holder, bool enabled) { }

        public void SetEmissive(Vector3? color) { }

        int IKn5RenderableObject.TrianglesCount => GetTrianglesCount();

        public void SetTransparent(bool? isTransparent) { }

        private TrianglesRenderableObject<InputLayouts.VerticePNTG> _pntgObject;

        private static ushort[] Convert(ushort[] indices) {
            return indices.ToIndicesFixX();
        }

        public Kn5RenderableDepthOnlyObject(Kn5Node node, bool forceVisible = false)
                : base(node.Name, InputLayouts.VerticeP.Convert(node.Vertices), Convert(node.Indices)) {
            OriginalNode = node;
            if (IsEnabled && (!node.Active || !forceVisible && (!node.IsVisible || !node.IsRenderable))) {
                IsEnabled = false;
            }
        }

        private Kn5MaterialDepth _material;

        protected override void Initialize(IDeviceContextHolder contextHolder) {
            base.Initialize(contextHolder);

            _material = (Kn5MaterialDepth)contextHolder.Get<SharedMaterials>().GetMaterial(BasicMaterials.DepthOnlyKey);
            _material.Initialize(contextHolder);
        }

        private Tuple<IRenderableTexture, float> _txNormal;
        private ShaderResourceView _txNormalView;

        protected override void DrawOverride(IDeviceContextHolder contextHolder, ICamera camera, SpecialRenderMode mode) {
            if (mode == SpecialRenderMode.Shadow) {
                if (_pntgObject == null) {
                    _pntgObject = new TrianglesRenderableObject<InputLayouts.VerticePNTG>("",
                            InputLayouts.VerticePNTG.Convert(OriginalNode.Vertices), Indices);
                    _pntgObject.Draw(contextHolder, camera, SpecialRenderMode.InitializeOnly);
                    
                    _txNormal = contextHolder.Get<INormalTexturesProvider>().GetTexture(contextHolder, OriginalNode.MaterialId);
                    _txNormalView = _txNormal?.Item1.Resource ?? contextHolder.GetFlatNmTexture();
                }

                _material.PrepareAo(contextHolder, _txNormalView, _txNormal?.Item2 ?? 1f);
                _pntgObject.SetBuffers(contextHolder);
                _material.SetMatricesAo(ParentMatrix);
                _material.DrawAo(contextHolder, Indices.Length);
            } else {
                if (mode != SpecialRenderMode.Simple) return;
                if (!_material.Prepare(contextHolder, mode)) return;

                base.DrawOverride(contextHolder, camera, mode);

                _material.SetMatrices(ParentMatrix, camera);
                _material.Draw(contextHolder, Indices.Length, mode);
            }
        }

        public override BaseRenderableObject Clone() {
            throw new NotSupportedException();
        }

        public override void Dispose() {
            DisposeHelper.Dispose(ref _pntgObject);
            DisposeHelper.Dispose(ref _material);
            base.Dispose();
        }

        public static IRenderableObject Convert(Kn5Node node) {
            switch (node.NodeClass) {
                case Kn5NodeClass.Base:
                    return new Kn5RenderableList(node, Convert);

                case Kn5NodeClass.Mesh:
                case Kn5NodeClass.SkinnedMesh:
                    return new Kn5RenderableDepthOnlyObject(node);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}